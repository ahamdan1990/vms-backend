using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Capacity;

namespace VisitorManagementSystem.Api.Application.Queries.Capacity;

/// <summary>
/// Query to validate capacity for an invitation
/// </summary>
public class ValidateCapacityQuery : IRequest<CapacityValidationResponseDto>
{
    /// <summary>
    /// Location ID to check capacity for
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>
    /// Time slot ID to check capacity for
    /// </summary>
    public int? TimeSlotId { get; set; }

    /// <summary>
    /// Date and time for the appointment
    /// </summary>
    public DateTime DateTime { get; set; }

    /// <summary>
    /// Number of expected visitors
    /// </summary>
    public int ExpectedVisitors { get; set; } = 1;

    /// <summary>
    /// Whether this is a VIP request (can override capacity)
    /// </summary>
    public bool IsVipRequest { get; set; } = false;

    /// <summary>
    /// Invitation ID to exclude from capacity calculation (for updates)
    /// </summary>
    public int? ExcludeInvitationId { get; set; }
}
