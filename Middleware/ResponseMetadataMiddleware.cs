using Serilog.Context;

namespace VisitorManagementSystem.Api.Middleware
{
    /// <summary>
    /// Middleware that adds correlation ID and timestamp headers to each response.
    /// </summary>
    public class ResponseMetadataMiddleware
    {
        public const string CorrelationIdItemKey = "__VMS_CORRELATION_ID";

        private readonly RequestDelegate _next;
        private readonly ILogger<ResponseMetadataMiddleware> _logger;

        public ResponseMetadataMiddleware(RequestDelegate next, ILogger<ResponseMetadataMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = ResolveCorrelationId(context);

            context.TraceIdentifier = correlationId;
            context.Items[CorrelationIdItemKey] = correlationId;
            context.Items["CorrelationId"] = correlationId;

            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey("X-Correlation-ID"))
                {
                    context.Response.Headers["X-Correlation-ID"] = correlationId;
                }

                if (!context.Response.Headers.ContainsKey("X-Timestamp"))
                {
                    context.Response.Headers["X-Timestamp"] = DateTime.UtcNow.ToString("o");
                }

                return Task.CompletedTask;
            });

            try
            {
                using (LogContext.PushProperty("CorrelationId", correlationId))
                {
                    await _next(context);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in ResponseMetadataMiddleware. CorrelationId: {CorrelationId}", correlationId);
                throw;
            }
        }

        private static string ResolveCorrelationId(HttpContext context)
        {
            var incomingCorrelationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();
            var incomingRequestId = context.Request.Headers["X-Request-ID"].FirstOrDefault();
            var existingCorrelationId = context.Items[CorrelationIdItemKey]?.ToString();
            var legacyCorrelationId = context.Items["CorrelationId"]?.ToString();
            var traceIdentifier = context.TraceIdentifier;

            if (IsValidCorrelationId(incomingCorrelationId))
            {
                return incomingCorrelationId!;
            }

            if (IsValidCorrelationId(incomingRequestId))
            {
                return incomingRequestId!;
            }

            if (IsValidCorrelationId(existingCorrelationId))
            {
                return existingCorrelationId!;
            }

            if (IsValidCorrelationId(legacyCorrelationId))
            {
                return legacyCorrelationId!;
            }

            if (IsValidCorrelationId(traceIdentifier))
            {
                return traceIdentifier;
            }

            return Guid.NewGuid().ToString("D");
        }

        private static bool IsValidCorrelationId(string? value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.Length <= 128 &&
                   !value.Any(char.IsControl);
        }
    }
}
