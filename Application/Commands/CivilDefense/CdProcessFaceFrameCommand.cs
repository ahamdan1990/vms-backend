using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.CivilDefense;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.Commands.CivilDefense;

public class CdProcessFaceFrameCommand : IRequest<CdFaceMatchResultDto>
{
    public byte[] FrameBytes { get; set; } = [];
    public int CameraId { get; set; }
    public CameraRole CameraRole { get; set; }
    public double ConfidenceThreshold { get; set; } = 0.80;
}
