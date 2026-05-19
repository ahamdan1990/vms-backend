using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.CivilDefense;

public class GetCivilDefenseCamerasQuery : IRequest<CivilDefenseCamerasDto> { }

public class CivilDefenseCameraDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string WorkflowMode { get; set; } = string.Empty;
    public int? LocationId { get; set; }
    public string? LocationName { get; set; }
}

public class CivilDefenseCamerasDto
{
    public List<CivilDefenseCameraDto> EntryCameras { get; set; } = new();
    public List<CivilDefenseCameraDto> ExitCameras { get; set; } = new();
}

public class GetCivilDefenseCamerasQueryHandler : IRequestHandler<GetCivilDefenseCamerasQuery, CivilDefenseCamerasDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCivilDefenseCamerasQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<CivilDefenseCamerasDto> Handle(
        GetCivilDefenseCamerasQuery request,
        CancellationToken cancellationToken)
    {
        var cameras = await _unitOfWork.Repository<Camera>()
            .GetQueryable()
            .Include(c => c.Location)
            .Where(c => !c.IsDeleted)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Filter in memory after deserializing configuration JSON
        var cdCameras = cameras
            .Select(c => (camera: c, config: c.GetConfiguration()))
            .Where(x => x.config.IsCivilDefenseCamera)
            .ToList();

        var toDto = (Camera c, string role, string mode, string? locName) => new CivilDefenseCameraDto
        {
            Id = c.Id,
            Name = c.Name,
            Status = c.Status.ToString(),
            WorkflowMode = mode,
            LocationId = c.LocationId,
            LocationName = locName
        };

        return new CivilDefenseCamerasDto
        {
            EntryCameras = cdCameras
                .Where(x => x.config.CameraRole == Domain.Enums.CameraRole.Entry)
                .Select(x => toDto(x.camera, "Entry", x.config.WorkflowMode.ToString(), x.camera.Location?.Name))
                .ToList(),
            ExitCameras = cdCameras
                .Where(x => x.config.CameraRole == Domain.Enums.CameraRole.Exit)
                .Select(x => toDto(x.camera, "Exit", x.config.WorkflowMode.ToString(), x.camera.Location?.Name))
                .ToList()
        };
    }
}
