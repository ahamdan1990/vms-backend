using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.CivilDefense;

namespace VisitorManagementSystem.Api.Application.Commands.CivilDefense;

/// <summary>
/// Registers a new visitor (or finds an existing one by phone) and immediately checks them in.
/// No email is collected — a synthetic {phone}@cd.local address is used internally.
/// </summary>
public class CdRegisterAndCheckInCommand : IRequest<CdRegisterAndCheckInResultDto>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public string? Nationality { get; set; }
    public string? CivilianOrigin { get; set; }
    public int OperatorUserId { get; set; }
    public int? CameraId { get; set; }
}
