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
                    _logger.LogInformation("🔑 PermissionClaimsMiddleware - Loading permissions for user {UserId}", userId);

                    var permissionService = context.RequestServices.GetRequiredService<IPermissionService>();
                    var permissions = await permissionService.GetUserPermissionsAsync(userId);

                    _logger.LogInformation("🔑 Retrieved {Count} permissions from PermissionService for user {UserId}", permissions.Count, userId);

                    if (permissions.Any())
                    {
                        var identity = context.User.Identity as ClaimsIdentity;
                        if (identity == null)
                        {
                            _logger.LogWarning("⚠️ ClaimsIdentity is null for user {UserId}", userId);
                            await _next(context);
                            return;
                        }

                        var existing = identity.Claims.Where(c => c.Type == "permission").Select(c => c.Value).ToHashSet();

                        var newPermissions = permissions.Except(existing).ToList();
                        foreach (var permission in newPermissions)
                            identity.AddClaim(new Claim("permission", permission));

                        _logger.LogInformation("✅ Added {NewCount} new permissions to user {UserId} (Total: {TotalCount})",
                            newPermissions.Count, userId, permissions.Count);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ No permissions found for user {UserId}!", userId);
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ Could not parse userId from claim: {UserIdClaim}", userIdClaim);
                }
            }

            await _next(context);
        }


    }
}
