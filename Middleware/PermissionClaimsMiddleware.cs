using System.Security.Claims;
using VisitorManagementSystem.Api.Application.Services.Auth;

namespace VisitorManagementSystem.Api.Middleware
{
    public class PermissionClaimsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PermissionClaimsMiddleware> _logger;

        public PermissionClaimsMiddleware(RequestDelegate next, ILogger<PermissionClaimsMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out var userId))
                {
                    var permissionService = context.RequestServices.GetRequiredService<IPermissionService>();
                    var permissions = await permissionService.GetUserPermissionsAsync(userId);

                    if (permissions.Any())
                    {
                        var identity = context.User.Identity as ClaimsIdentity;
                        var existing = identity.Claims.Where(c => c.Type == "permission").Select(c => c.Value).ToHashSet();

                        foreach (var permission in permissions.Except(existing))
                            identity.AddClaim(new Claim("permission", permission));

                        _logger.LogDebug("Added {Count} permissions to user {UserId}", permissions.Count, userId);
                    }
                }
            }

            await _next(context);
        }


    }
}
