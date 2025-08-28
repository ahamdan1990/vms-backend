using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using VisitorManagementSystem.Api.Application.Services.Auth;

namespace VisitorManagementSystem.Api.Hubs
{
    public class PermissionHubFilter : IHubFilter
    {
        private readonly ILogger<PermissionHubFilter> _logger;
        private readonly IServiceProvider _serviceProvider;

        public PermissionHubFilter(ILogger<PermissionHubFilter> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async ValueTask<object?> InvokeMethodAsync(
            HubInvocationContext invocationContext,
            Func<HubInvocationContext, ValueTask<object?>> next)
        {
            using var scope = _serviceProvider.CreateScope();
            var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();

            var userIdClaim = invocationContext.Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                var permissions = await permissionService.GetUserPermissionsAsync(userId);
                // add permissions to HubCallerContext.User if needed
            }

            return await next(invocationContext);
        }
    }


}
