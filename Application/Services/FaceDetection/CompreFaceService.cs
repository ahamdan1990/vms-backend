using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VisitorManagementSystem.Api.Application.Services.FaceDetection;

/// <summary>
/// Production-ready implementation of face detection and recognition service using CompreFace
/// </summary>
public class CompreFaceService : IFaceDetectionService
{
    private readonly HttpClient _httpClient;
    private readonly CompreFaceSettings _settings;
    private readonly ILogger<CompreFaceService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public CompreFaceService(
        HttpClient httpClient,
        IOptions<CompreFaceSettings> settings,
        ILogger<CompreFaceService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        // Configure JSON serialization options
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // Configure HttpClient base address
        if (!string.IsNullOrEmpty(_settings.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/'));
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }
    }

    /// <summary>
    /// Executes an operation with exponential backoff retry logic
    /// </summary>
    private async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        int maxRetries = 3,
        CancellationToken cancellationToken = default)
    {
        var retryCount = 0;
        var delay = TimeSpan.FromMilliseconds(500);

        while (true)
        {
            try
            {
                return await operation();
            }
            catch (HttpRequestException ex) when (retryCount < maxRetries)
            {
                retryCount++;
                _logger.LogWarning(ex,
                    "Attempt {Attempt}/{MaxAttempts} failed for {Operation}. Retrying in {Delay}ms...",
                    retryCount, maxRetries, operationName, delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2); // Exponential backoff
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Operation {Operation} failed after {Attempts} attempts",
                    operationName, retryCount);
                throw;
            }
        }
    }

    public async Task<List<DetectedFace>> DetectFacesAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_settings.Enabled)
            {
                _logger.LogDebug("CompreFace is disabled, returning empty face list");
                return new List<DetectedFace>();
            }

            if (string.IsNullOrEmpty(_settings.BaseUrl) || string.IsNullOrEmpty(_settings.DetectionApiKey))
            {
                _logger.LogWarning("CompreFace Detection Service is not configured properly");
                return new List<DetectedFace>();
            }

            // Reset stream position
            if (imageStream.CanSeek)
            {
                imageStream.Position = 0;
            }

            // Copy stream to byte array to allow retries
            byte[] imageBytes;
            using (var memoryStream = new MemoryStream())
            {
                await imageStream.CopyToAsync(memoryStream, cancellationToken);
                imageBytes = memoryStream.ToArray();
            }

            // Execute with retry logic for transient errors
            return await ExecuteWithRetryAsync(async () =>
            {
                // Create multipart form data
                using var content = new MultipartFormDataContent();
                using var streamContent = new ByteArrayContent(imageBytes);
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                content.Add(streamContent, "file", "image.jpg");

                // Call CompreFace detection API
                var endpoint = $"/api/v1/detection/detect?limit={_settings.MaxFacesDetect}&det_prob_threshold={_settings.MinimumConfidence}&face_plugins=age,gender";

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Add("x-api-key", _settings.DetectionApiKey);
                request.Content = content;

                var response = await _httpClient.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("CompreFace detection failed: {StatusCode} - {Error}",
                        response.StatusCode, errorContent);

                    // Throw to trigger retry for 5xx errors
                    if ((int)response.StatusCode >= 500)
                    {
                        throw new HttpRequestException($"CompreFace server error: {response.StatusCode} - {errorContent}");
                    }

                    return new List<DetectedFace>();
                }

                var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<CompreFaceDetectionResponse>(jsonResponse, _jsonOptions);

                if (result?.Result == null || result.Result.Count == 0)
                {
                    _logger.LogDebug("No faces detected in image");
                    return new List<DetectedFace>();
                }

                // Filter by minimum confidence and convert to DetectedFace objects
                var faces = result.Result
                    .Where(face => face.Box?.Probability >= _settings.MinimumConfidence)
                    .Select(face => new DetectedFace
                    {
                        X = face.Box?.XMin ?? 0,
                        Y = face.Box?.YMin ?? 0,
                        Width = (face.Box?.XMax ?? 0) - (face.Box?.XMin ?? 0),
                        Height = (face.Box?.YMax ?? 0) - (face.Box?.YMin ?? 0),
                        Confidence = face.Box?.Probability ?? 0,
                        Age = face.Age?.Low,
                        Gender = face.Gender?.Value
                    })
                    .OrderByDescending(f => f.Confidence)
                    .ToList();

                _logger.LogInformation("Detected {Count} face(s) in image with confidence >= {MinConfidence}",
                    faces.Count, _settings.MinimumConfidence);

                return faces;
            }, "DetectFaces", maxRetries: 2, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("CompreFace detection request timed out");
            return new List<DetectedFace>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting faces with CompreFace");
            return new List<DetectedFace>();
        }
    }

    public async Task<byte[]?> DetectAndCropFaceAsync(Stream imageStream, int marginPercent = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_settings.Enabled)
            {
                _logger.LogDebug("CompreFace is disabled, skipping face cropping");
                return null;
            }

            // Reset stream position
            if (imageStream.CanSeek)
            {
                imageStream.Position = 0;
            }

            // Copy stream to byte array to avoid stream disposal issues
            byte[] imageBytes;
            using (var tempStream = new MemoryStream())
            {
                await imageStream.CopyToAsync(tempStream, cancellationToken);
                imageBytes = tempStream.ToArray();
            }

            // Detect faces using a new stream instance
            List<DetectedFace> faces;
            using (var detectionStream = new MemoryStream(imageBytes))
            {
                faces = await DetectFacesAsync(detectionStream, cancellationToken);
            }

            if (faces == null || faces.Count == 0)
            {
                _logger.LogDebug("No faces detected, returning null");
                return null;
            }

            // Use the first detected face (highest confidence)
            var face = faces.First();

            _logger.LogInformation("Cropping face at ({X},{Y}) with size {Width}x{Height}, confidence: {Confidence:P2}",
                face.X, face.Y, face.Width, face.Height, face.Confidence);

            // Load image from byte array for cropping
            using var image = await Image.LoadAsync(new MemoryStream(imageBytes), cancellationToken);

            // Calculate margin
            var marginX = (int)(face.Width * marginPercent / 100.0);
            var marginY = (int)(face.Height * marginPercent / 100.0);

            // Calculate crop rectangle with margin
            var x = Math.Max(0, face.X - marginX);
            var y = Math.Max(0, face.Y - marginY);
            var width = Math.Min(image.Width - x, face.Width + (2 * marginX));
            var height = Math.Min(image.Height - y, face.Height + (2 * marginY));

            // Ensure we have valid dimensions
            if (width <= 0 || height <= 0)
            {
                _logger.LogWarning("Invalid crop dimensions calculated, returning null");
                return null;
            }

            // Crop the image
            image.Mutate(ctx => ctx.Crop(new Rectangle(x, y, width, height)));

            // Save to byte array as JPEG
            using var outputStream = new MemoryStream();
            await image.SaveAsJpegAsync(outputStream, cancellationToken);

            _logger.LogInformation("Face cropped successfully, output size: {Size} KB", outputStream.Length / 1024);
            return outputStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cropping face from image");
            return null;
        }
    }

    public async Task<FaceRecognitionResult> AddFaceToCollectionAsync(
        byte[] imageBytes,
        string subjectId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_settings.Enabled || string.IsNullOrEmpty(_settings.RecognitionApiKey))
            {
                return new FaceRecognitionResult
                {
                    Success = false,
                    ErrorMessage = "CompreFace Recognition Service is not configured"
                };
            }

            // Execute with retry logic
            return await ExecuteWithRetryAsync(async () =>
            {
                // Create multipart form data
                using var content = new MultipartFormDataContent();
                using var imageContent = new ByteArrayContent(imageBytes);
                imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                content.Add(imageContent, "file", $"{subjectId}.jpg");

                // Add face to recognition service's face collection
                var endpoint = $"/api/v1/recognition/faces?subject={Uri.EscapeDataString(subjectId)}&det_prob_threshold={_settings.MinimumConfidence}";

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Add("x-api-key", _settings.RecognitionApiKey);
                request.Content = content;

                var response = await _httpClient.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Failed to add face to CompreFace collection for subject {Subject}: {StatusCode} - {Error}",
                        subjectId, response.StatusCode, errorContent);

                    // Throw to trigger retry for 5xx errors
                    if ((int)response.StatusCode >= 500)
                    {
                        throw new HttpRequestException($"CompreFace server error: {response.StatusCode}");
                    }

                    return new FaceRecognitionResult
                    {
                        Success = false,
                        ErrorMessage = $"Failed to add face to collection: {response.StatusCode}"
                    };
                }

                var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<CompreFaceAddFaceResponse>(jsonResponse, _jsonOptions);

                if (result == null || string.IsNullOrEmpty(result.ImageId))
                {
                    _logger.LogWarning("Invalid response when adding face to collection for subject {Subject}", subjectId);
                    return new FaceRecognitionResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid response from CompreFace"
                    };
                }

                _logger.LogInformation("Successfully added face to collection for subject {Subject}, image_id: {ImageId}",
                    subjectId, result.ImageId);

                return new FaceRecognitionResult
                {
                    Success = true,
                    SubjectId = subjectId,
                    ImageId = result.ImageId
                };
            }, $"AddFaceToCollection for {subjectId}", maxRetries: 3, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("CompreFace add face request timed out for subject {Subject}", subjectId);
            return new FaceRecognitionResult
            {
                Success = false,
                ErrorMessage = "Request timed out"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding face to collection for subject {Subject}", subjectId);
            return new FaceRecognitionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<bool> RemoveFaceFromCollectionAsync(string subjectId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_settings.Enabled || string.IsNullOrEmpty(_settings.RecognitionApiKey))
            {
                return false;
            }

            // Delete all face examples for this subject
            var endpoint = $"/api/v1/recognition/faces?subject={Uri.EscapeDataString(subjectId)}";

            using var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
            request.Headers.Add("x-api-key", _settings.RecognitionApiKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to remove faces from collection for subject {Subject}: {StatusCode} - {Error}",
                    subjectId, response.StatusCode, errorContent);
                return false;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<CompreFaceDeleteResponse>(jsonResponse, _jsonOptions);

            _logger.LogInformation("Removed {Count} face(s) from collection for subject {Subject}",
                result?.Deleted ?? 0, subjectId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing faces from collection for subject {Subject}", subjectId);
            return false;
        }
    }

    public async Task<List<RecognizedFace>> RecognizeFacesAsync(Stream imageStream, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_settings.Enabled || string.IsNullOrEmpty(_settings.RecognitionApiKey))
            {
                return new List<RecognizedFace>();
            }

            // Reset stream position
            if (imageStream.CanSeek)
            {
                imageStream.Position = 0;
            }

            // Create multipart form data
            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(imageStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            content.Add(streamContent, "file", "image.jpg");

            // Call CompreFace recognition API
            var endpoint = $"/api/v1/recognition/recognize?limit={_settings.MaxFacesDetect}&prediction_count=1&det_prob_threshold={_settings.MinimumConfidence}";

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("x-api-key", _settings.RecognitionApiKey);
            request.Content = content;

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("CompreFace recognition failed: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);
                return new List<RecognizedFace>();
            }

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<CompreFaceRecognitionResponse>(jsonResponse, _jsonOptions);

            if (result?.Result == null || result.Result.Count == 0)
            {
                _logger.LogDebug("No faces recognized in image");
                return new List<RecognizedFace>();
            }

            // Convert to RecognizedFace objects
            var recognizedFaces = result.Result
                .Where(face => face.Subjects != null && face.Subjects.Count > 0)
                .Select(face => new RecognizedFace
                {
                    SubjectId = face.Subjects![0].Subject ?? string.Empty,
                    Similarity = face.Subjects[0].Similarity,
                    Confidence = face.Box?.Probability ?? 0,
                    BoundingBox = new DetectedFace
                    {
                        X = face.Box?.XMin ?? 0,
                        Y = face.Box?.YMin ?? 0,
                        Width = (face.Box?.XMax ?? 0) - (face.Box?.XMin ?? 0),
                        Height = (face.Box?.YMax ?? 0) - (face.Box?.YMin ?? 0),
                        Confidence = face.Box?.Probability ?? 0
                    }
                })
                .Where(f => f.Similarity >= _settings.MinimumSimilarity)
                .ToList();

            _logger.LogInformation("Recognized {Count} face(s) in image", recognizedFaces.Count);
            return recognizedFaces;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recognizing faces with CompreFace");
            return new List<RecognizedFace>();
        }
    }

    public async Task<bool> VerifyFaceAsync(byte[] image1, byte[] image2, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_settings.Enabled || string.IsNullOrEmpty(_settings.VerificationApiKey))
            {
                return false;
            }

            // Create multipart form data with two images
            using var content = new MultipartFormDataContent();

            using var image1Content = new ByteArrayContent(image1);
            image1Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            content.Add(image1Content, "source_image", "image1.jpg");

            using var image2Content = new ByteArrayContent(image2);
            image2Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            content.Add(image2Content, "target_image", "image2.jpg");

            // Call CompreFace verification API
            var endpoint = $"/api/v1/verification/verify?det_prob_threshold={_settings.MinimumConfidence}";

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("x-api-key", _settings.VerificationApiKey);
            request.Content = content;

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("CompreFace verification failed: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);
                return false;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<CompreFaceVerificationResponse>(jsonResponse, _jsonOptions);

            var verified = result?.Result != null &&
                          result.Result.Count > 0 &&
                          result.Result[0].Similarity >= _settings.MinimumSimilarity;

            _logger.LogInformation("Face verification result: {Verified}, similarity: {Similarity:P2}",
                verified, result?.Result?[0].Similarity ?? 0);

            return verified;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying faces with CompreFace");
            return false;
        }
    }

    public async Task<bool> IsServiceAvailableAsync()
    {
        try
        {
            if (!_settings.Enabled)
            {
                return false;
            }

            if (string.IsNullOrEmpty(_settings.BaseUrl))
            {
                return false;
            }

            // Try to check if at least one service is available
            var hasAnyKey = !string.IsNullOrEmpty(_settings.DetectionApiKey) ||
                           !string.IsNullOrEmpty(_settings.RecognitionApiKey) ||
                           !string.IsNullOrEmpty(_settings.VerificationApiKey);

            if (!hasAnyKey)
            {
                return false;
            }

            // Try a simple HEAD or GET request to check if server is reachable
            using var request = new HttpRequestMessage(HttpMethod.Get, "/");
            var response = await _httpClient.SendAsync(request, CancellationToken.None);

            return response.IsSuccessStatusCode ||
                   response.StatusCode == System.Net.HttpStatusCode.NotFound ||
                   response.StatusCode == System.Net.HttpStatusCode.Unauthorized;
        }
        catch
        {
            return false;
        }
    }
}

#region Response Models

internal class CompreFaceDetectionResponse
{
    [JsonPropertyName("result")]
    public List<CompreFaceDetectionResult>? Result { get; set; }
}

internal class CompreFaceDetectionResult
{
    [JsonPropertyName("box")]
    public CompreFaceBoundingBox? Box { get; set; }

    [JsonPropertyName("age")]
    public CompreFaceAge? Age { get; set; }

    [JsonPropertyName("gender")]
    public CompreFaceGender? Gender { get; set; }
}

internal class CompreFaceBoundingBox
{
    [JsonPropertyName("probability")]
    public double Probability { get; set; }

    [JsonPropertyName("x_min")]
    public int XMin { get; set; }

    [JsonPropertyName("y_min")]
    public int YMin { get; set; }

    [JsonPropertyName("x_max")]
    public int XMax { get; set; }

    [JsonPropertyName("y_max")]
    public int YMax { get; set; }
}

internal class CompreFaceAge
{
    [JsonPropertyName("probability")]
    public double Probability { get; set; }

    [JsonPropertyName("high")]
    public int High { get; set; }

    [JsonPropertyName("low")]
    public int Low { get; set; }
}

internal class CompreFaceGender
{
    [JsonPropertyName("probability")]
    public double Probability { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

internal class CompreFaceAddFaceResponse
{
    [JsonPropertyName("image_id")]
    public string? ImageId { get; set; }

    [JsonPropertyName("subject")]
    public string? Subject { get; set; }
}

internal class CompreFaceDeleteResponse
{
    [JsonPropertyName("deleted")]
    public int Deleted { get; set; }
}

internal class CompreFaceRecognitionResponse
{
    [JsonPropertyName("result")]
    public List<CompreFaceRecognitionResult>? Result { get; set; }
}

internal class CompreFaceRecognitionResult
{
    [JsonPropertyName("box")]
    public CompreFaceBoundingBox? Box { get; set; }

    [JsonPropertyName("subjects")]
    public List<CompreFaceSubject>? Subjects { get; set; }
}

internal class CompreFaceSubject
{
    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("similarity")]
    public double Similarity { get; set; }
}

internal class CompreFaceVerificationResponse
{
    [JsonPropertyName("result")]
    public List<CompreFaceVerificationResult>? Result { get; set; }
}

internal class CompreFaceVerificationResult
{
    [JsonPropertyName("similarity")]
    public double Similarity { get; set; }
}

#endregion
