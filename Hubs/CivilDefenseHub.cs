using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Hubs;

/// <summary>
/// Real-time hub for the Civil Defense module.
/// Groups:
///   "cd-receptionist" — receptionists see face-match popups and occupancy changes
///   "cd-manager"      — managers see occupancy + camera status updates
/// </summary>
[Authorize]
public class CivilDefenseHub : BaseHub
{
    public CivilDefenseHub(ILogger<BaseHub> logger, IUnitOfWork unitOfWork)
        : base(logger, unitOfWork) { }

    public override async Task OnConnectedAsync()
    {
        var permissions = GetCurrentUserPermissions();

        if (permissions.Contains(Permissions.CivilDefense.CheckIn) ||
            permissions.Contains(Permissions.CivilDefense.RegisterVisitor))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "cd-receptionist");
            Logger.LogDebug("CD hub: receptionist {ConnId} joined cd-receptionist group", Context.ConnectionId);
        }

        if (permissions.Contains(Permissions.CivilDefense.ViewReports) ||
            permissions.Contains(Permissions.CivilDefense.ViewLiveBuilding))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "cd-manager");
            Logger.LogDebug("CD hub: manager {ConnId} joined cd-manager group", Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "cd-receptionist");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "cd-manager");
        await base.OnDisconnectedAsync(exception);
    }

    // ── Hub events sent TO clients ────────────────────────────────────────────
    // FaceRecognized      — { result: CdFaceMatchResultDto }
    // UnknownFaceDetected — { cameraId, cameraRole, frameThumb? }
    // OccupancyChanged    — { totalInside, staffInside, visitorsInside }
    // CameraStatusChanged — { cameraId, cameraName, status }
}
