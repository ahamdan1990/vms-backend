using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Application.Commands.VisitorDocuments;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Application.Queries.VisitorDocuments;
using VisitorManagementSystem.Api.Application.Queries.Visitors;
using VisitorManagementSystem.Api.Application.Services.FileUploadService;
using VisitorManagementSystem.Api.Domain.Constants;

namespace VisitorManagementSystem.Api.Controllers;

/// <summary>
/// Controller for visitor document management operations
/// </summary>
[ApiController]
[Route("api/visitors/{visitorId:int}/documents")]
[Authorize]
public class VisitorDocumentsController : BaseController
{
    private readonly IMediator _mediator;
    private readonly ILogger<VisitorDocumentsController> _logger;
    private readonly IWebHostEnvironment _environment;

    public VisitorDocumentsController(
        IMediator mediator, 
        ILogger<VisitorDocumentsController> logger,
        IWebHostEnvironment environment)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <summary>
    /// Gets visitor documents for a visitor
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="documentType">Document type filter</param>
    /// <param name="includeDeleted">Include deleted documents</param>
    /// <returns>List of visitor documents</returns>
    [HttpGet]
    [Authorize(Policy = Permissions.VisitorDocument.Read)]
    public async Task<IActionResult> GetVisitorDocuments(
        int visitorId, 
        [FromQuery] string? documentType = null,
        [FromQuery] bool includeDeleted = false)
    {
        var query = new GetVisitorDocumentsByVisitorIdQuery 
        { 
            VisitorId = visitorId,
            DocumentType = documentType,
            IncludeDeleted = includeDeleted
        };

        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Gets a visitor document by ID
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="id">Document ID</param>
    /// <param name="includeDeleted">Include deleted document</param>
    /// <returns>Visitor document details</returns>
    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.VisitorDocument.Read)]
    public async Task<IActionResult> GetVisitorDocument(int visitorId, int id, [FromQuery] bool includeDeleted = false)
    {
        var query = new GetVisitorDocumentByIdQuery 
        { 
            Id = id,
            IncludeDeleted = includeDeleted 
        };

        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFoundResponse("Visitor document", id);
        }

        return SuccessResponse(result);
    }

    /// <summary>
    /// Creates a new visitor document
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="createDto">Visitor document creation data</param>
    /// <returns>Created visitor document</returns>
    [HttpPost]
    [Authorize(Policy = Permissions.VisitorDocument.Create)]
    public async Task<IActionResult> CreateVisitorDocument(int visitorId, [FromBody] CreateVisitorDocumentDto createDto)
    {
        var command = new CreateVisitorDocumentCommand
        {
            VisitorId = visitorId,
            Title = createDto.Title,
            Description = createDto.Description,
            DocumentType = createDto.DocumentType,
            FilePath = createDto.FilePath ?? string.Empty,
            OriginalFileName = createDto.OriginalFileName,
            FileSize = createDto.FileSize,
            MimeType = createDto.MimeType,
            IsSensitive = createDto.IsSensitive,
            IsRequired = createDto.IsRequired,
            ExpiryDate = createDto.ExpiryDate,
            Tags = createDto.Tags,
            CreatedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return CreatedResponse(result, Url.Action(nameof(GetVisitorDocument), new { visitorId, id = result.Id }));
    }

    /// <summary>
    /// Updates an existing visitor document
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="id">Document ID</param>
    /// <param name="updateDto">Visitor document update data</param>
    /// <returns>Updated visitor document</returns>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.VisitorDocument.Update)]
    public async Task<IActionResult> UpdateVisitorDocument(int visitorId, int id, [FromBody] UpdateVisitorDocumentDto updateDto)
    {
        var command = new UpdateVisitorDocumentCommand
        {
            Id = id,
            Title = updateDto.Title,
            Description = updateDto.Description,
            DocumentType = updateDto.DocumentType,
            IsSensitive = updateDto.IsSensitive,
            IsRequired = updateDto.IsRequired,
            ExpiryDate = updateDto.ExpiryDate,
            Tags = updateDto.Tags,
            ModifiedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Deletes a visitor document (soft delete)
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="id">Document ID</param>
    /// <param name="permanentDelete">Whether to permanently delete</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.VisitorDocument.Delete)]
    public async Task<IActionResult> DeleteVisitorDocument(int visitorId, int id, [FromQuery] bool permanentDelete = false)
    {
        var command = new DeleteVisitorDocumentCommand
        {
            Id = id,
            DeletedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated"),
            PermanentDelete = permanentDelete
        };

        var result = await _mediator.Send(command);
        return SuccessResponse(result);
    }

    /// <summary>
    /// Previews a visitor document (inline display)
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="id">Document ID</param>
    /// <returns>File stream with inline disposition</returns>
    [HttpGet("{id:int}/preview")]
    [Authorize(Policy = Permissions.VisitorDocument.Read)]
    public async Task<IActionResult> PreviewVisitorDocument(int visitorId, int id)
    {
        try
        {
            // Get document DTO
            var documentDto = await _mediator.Send(new GetVisitorDocumentByIdQuery { Id = id });

            if (documentDto == null)
            {
                _logger.LogWarning("Document not found: {DocumentId}", id);
                return NotFoundResponse("Visitor document", id);
            }

            // Verify document belongs to the specified visitor
            if (documentDto.VisitorId != visitorId)
            {
                _logger.LogWarning("Document {DocumentId} does not belong to visitor {VisitorId}", id, visitorId);
                return BadRequest("Document does not belong to the specified visitor");
            }

            // Use FilePath from DTO which contains the relative path like "uploads/visitor-documents/1/file.pdf"
            // Remove any leading slashes and replace forward slashes with the OS-specific separator
            var relativePath = (documentDto.FilePath ?? string.Empty).TrimStart('/').Replace('/', Path.DirectorySeparatorChar);

            var webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var filePath = Path.Combine(webRootPath, relativePath);

            _logger.LogDebug("📄 Document Preview - ID: {DocumentId}, RelativePath: {RelativePath}, FileExists: {FileExists}",
                id, relativePath, System.IO.File.Exists(filePath));

            // Check if file exists
            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("Document file not found: {FilePath} for document ID: {DocumentId}", filePath, id);
                return NotFound($"Document file not found on server. Expected path: {relativePath}");
            }

            // Get file stream
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            // Return file with inline disposition for preview
            return File(fileStream, documentDto.ContentType, enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing visitor document {DocumentId} for visitor {VisitorId}", id, visitorId);
            return BadRequest($"Error previewing document: {ex.Message}");
        }
    }

    /// <summary>
    /// Downloads a visitor document
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="id">Document ID</param>
    /// <returns>File stream</returns>
    [HttpGet("{id:int}/download")]
    [Authorize(Policy = Permissions.VisitorDocument.Download)]
    public async Task<IActionResult> DownloadVisitorDocument(int visitorId, int id)
    {
        try
        {
            // Get document DTO
            var documentDto = await _mediator.Send(new GetVisitorDocumentByIdQuery { Id = id });

            if (documentDto == null)
            {
                _logger.LogWarning("Document not found: {DocumentId}", id);
                return NotFoundResponse("Visitor document", id);
            }

            // Verify document belongs to the specified visitor
            if (documentDto.VisitorId != visitorId)
            {
                _logger.LogWarning("Document {DocumentId} does not belong to visitor {VisitorId}", id, visitorId);
                return BadRequest("Document does not belong to the specified visitor");
            }

            // Use FilePath from DTO which contains the relative path like "uploads/visitor-documents/1/file.pdf"
            // Remove any leading slashes and replace forward slashes with the OS-specific separator
            var relativePath = (documentDto.FilePath ?? string.Empty).TrimStart('/').Replace('/', Path.DirectorySeparatorChar);

            var webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var filePath = Path.Combine(webRootPath, relativePath);

            _logger.LogDebug("📄 Document Download - ID: {DocumentId}, RelativePath: {RelativePath}, FileExists: {FileExists}",
                id, relativePath, System.IO.File.Exists(filePath));

            // Check if file exists
            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("Document file not found: {FilePath} for document ID: {DocumentId}", filePath, id);
                return NotFound($"Document file not found on server. Expected path: {relativePath}");
            }

            // Get file bytes
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

            // Return file with attachment disposition for download
            return File(fileBytes, documentDto.ContentType, documentDto.OriginalFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading visitor document {DocumentId} for visitor {VisitorId}", id, visitorId);
            return BadRequest($"Error downloading document: {ex.Message}");
        }
    }

    /// <summary>
    /// Uploads a visitor document file
    /// </summary>
    /// <param name="visitorId">Visitor ID</param>
    /// <param name="file">Document file to upload</param>
    /// <param name="title">Document title</param>
    /// <param name="documentType">Document type</param>
    /// <param name="description">Document description</param>
    /// <param name="isSensitive">Whether document contains sensitive information</param>
    /// <param name="isRequired">Whether document is required for check-in</param>
    /// <param name="expiryDate">Document expiry date</param>
    /// <param name="tags">Additional tags</param>
    /// <returns>Created visitor document</returns>
    [HttpPost("upload")]
    [Authorize(Policy = Permissions.VisitorDocument.Create)]
    public async Task<IActionResult> UploadVisitorDocument(
        int visitorId,
        IFormFile file,
        [FromForm] string title,
        [FromForm] string documentType,
        [FromForm] string? description = null,
        [FromForm] bool isSensitive = false,
        [FromForm] bool isRequired = false,
        [FromForm] DateTime? expiryDate = null,
        [FromForm] string? tags = null)
    {
        try
        {
            // Validate file
            if (file == null || file.Length == 0)
            {
                return BadRequestResponse("File is required");
            }

            // Validate required parameters
            if (string.IsNullOrWhiteSpace(title))
            {
                return BadRequestResponse("Title is required");
            }

            if (string.IsNullOrWhiteSpace(documentType))
            {
                return BadRequestResponse("Document type is required");
            }

            // Check if visitor exists
            var visitor = await _mediator.Send(new GetVisitorByIdQuery { Id = visitorId });
            if (visitor == null)
            {
                return NotFound($"Visitor with ID {visitorId} not found");
            }

            // Upload file using file upload service
            var fileUploadService = HttpContext.RequestServices.GetRequiredService<IFileUploadService>();
            
            if (!fileUploadService.IsValidDocumentFile(file))
            {
                var allowedExtensions = string.Join(", ", fileUploadService.GetAllowedDocumentExtensions());
                var maxSizeMB = fileUploadService.GetMaxDocumentFileSize() / (1024 * 1024);
                return BadRequestResponse($"Invalid file. Allowed types: {allowedExtensions}. Maximum size: {maxSizeMB}MB");
            }

            var filePath = await fileUploadService.UploadVisitorDocumentAsync(visitorId, file, documentType);

            // Create document record
            var command = new CreateVisitorDocumentCommand
            {
                VisitorId = visitorId,
                Title = title.Trim(),
                Description = description?.Trim(),
                DocumentType = documentType.Trim(),
                FilePath = filePath,
                OriginalFileName = file.FileName,
                FileSize = file.Length,
                MimeType = file.ContentType,
                IsSensitive = isSensitive,
                IsRequired = isRequired,
                ExpiryDate = expiryDate,
                Tags = tags?.Trim(),
                CreatedBy = GetCurrentUserId() ?? throw new UnauthorizedAccessException("User must be authenticated")
            };

            var result = await _mediator.Send(command);
            return CreatedResponse(result, Url.Action(nameof(GetVisitorDocument), new { visitorId, id = result.Id }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload visitor document for visitor {VisitorId}", visitorId);
            return BadRequestResponse("Failed to upload document");
        }
    }

    /// <summary>
    /// Gets allowed document types and file restrictions
    /// </summary>
    /// <returns>File upload restrictions</returns>
    [HttpGet("upload-info")]
    [Authorize(Policy = Permissions.VisitorDocument.Read)]
    public IActionResult GetUploadInfo()
    {
        var fileUploadService = HttpContext.RequestServices.GetRequiredService<IFileUploadService>();
        
        var uploadInfo = new
        {
            AllowedExtensions = fileUploadService.GetAllowedDocumentExtensions(),
            MaxFileSizeMB = fileUploadService.GetMaxDocumentFileSize() / (1024 * 1024),
            AllowedDocumentTypes = new[]
            {
                "Passport",
                "National ID", 
                "Driver License",
                "Visa",
                "Work Permit",
                "Health Certificate",
                "Background Check",
                "Photo",
                "Other"
            }
        };

        return SuccessResponse(uploadInfo);
    }
}
