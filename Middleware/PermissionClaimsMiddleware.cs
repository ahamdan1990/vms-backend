using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using VisitorManagementSystem.Api.Application.Services.Auth;



namespace VisitorManagementSystem.Api.Middleware
{
    public class PermissionClaimsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PermissionClaimsMiddleware> _logger;

        // Permissions for a given user are stable between role changes.
        // 5 minutes gives a good balance between freshness and DB pressure.
        // Invalidated immediately on role/permission changes via IPermissionService.InvalidateUserPermissionCache().
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

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
                    var permissions = await GetPermissionsAsync(context, userId);

                    if (permissions.Count > 0)
                    {
                        var identity = context.User.Identity as ClaimsIdentity;
                        if (identity == null)
                        {
                            _logger.LogWarning("ClaimsIdentity is null for user {UserId}", userId);
                            await _next(context);
                            return;
                        }

                        var existing = identity.Claims
                            .Where(c => c.Type == "permission")
                            .Select(c => c.Value)
                            .ToHashSet();

                        var newPermissions = permissions.Except(existing).ToList();
                        foreach (var permission in newPermissions)
                            identity.AddClaim(new Claim("permission", permission));

                        _logger.LogDebug(
                            "Loaded {Total} permissions for user {UserId} ({New} new, from {Source})",
                            permissions.Count, userId, newPermissions.Count,
                            newPermissions.Count == 0 ? "cache" : "db+cache");
                    }
                    else
                    {
                        _logger.LogWarning("No permissions found for user {UserId}", userId);
                    }
                }
                else
                {
                    _logger.LogWarning("Could not parse userId from claim: {UserIdClaim}", userIdClaim);
                }
            }

            await _next(context);
        }

        private async Task<List<string>> GetPermissionsAsync(HttpContext context, int userId)
        {
            var cache = context.RequestServices.GetService<IDistributedCache>();

            if (cache != null)
            {
                var cacheKey = PermissionService.GetDistributedCacheKey(userId);
                try
                {
                    var cached = await cache.GetStringAsync(cacheKey);
                    if (cached != null)
                    {
                        return JsonSerializer.Deserialize<List<string>>(cached) ?? new List<string>();
                    }
                }
                catch (Exception ex)
                {
                    // Cache is unavailable — fall through to the DB without disrupting the request.
                    _logger.LogWarning(ex, "Permission cache read failed for user {UserId}; falling back to database", userId);
                }
            }

            var permissionService = context.RequestServices.GetRequiredService<IPermissionService>();
            var permissions = await permissionService.GetUserPermissionsAsync(userId);

            if (cache != null && permissions.Count > 0)
            {
                try
                {
                    var json = JsonSerializer.Serialize(permissions);
                    await cache.SetStringAsync(
                        PermissionService.GetDistributedCacheKey(userId),
                        json,
                        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Permission cache write failed for user {UserId}; continuing without caching", userId);
                }
            }

            return permissions;
        }

        public static string GetCacheKey(int userId) => $"permissions:user:{userId}";
    }
}
