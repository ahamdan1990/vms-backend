using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;

namespace VisitorManagementSystem.Api.Middleware
{
    /// <summary>
    /// Middleware that adds correlation ID and timestamp headers to each response
    /// </summary>
    public class ResponseMetadataMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ResponseMetadataMiddleware> _logger;

        public ResponseMetadataMiddleware(RequestDelegate next, ILogger<ResponseMetadataMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Get existing correlation ID or generate new one
            var correlationId = context.TraceIdentifier ?? Guid.NewGuid().ToString();

            // Store in context.Items for downstream retrieval (e.g. controllers)
            context.Items["CorrelationId"] = correlationId;

            // Hook into response starting event to add headers
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey("X-Correlation-ID"))
                {
                    context.Response.Headers["X-Correlation-ID"] = correlationId;
                }

                if (!context.Response.Headers.ContainsKey("X-Timestamp"))
                {
                    context.Response.Headers["X-Timestamp"] = DateTime.UtcNow.ToString("o"); // ISO8601 UTC
                }

                return Task.CompletedTask;
            });

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in ResponseMetadataMiddleware. CorrelationId: {CorrelationId}", correlationId);
                throw;
            }
        }
    }
}
