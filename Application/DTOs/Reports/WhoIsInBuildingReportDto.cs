using System;
using System.Collections.Generic;

namespace VisitorManagementSystem.Api.Application.DTOs.Reports;

/// <summary>
/// Represents a single visitor currently inside the building.
/// </summary>
public class InBuildingVisitorDto
{
    public int InvitationId { get; set; }
    public int VisitorId { get; set; }
    public string VisitorName { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? VisitorPhone { get; set; }
    public string? VisitorEmail { get; set; }
    public int HostId { get; set; }
    public string HostName { get; set; } = string.Empty;
    public string? HostDepartment { get; set; }
    public string? HostEmail { get; set; }
    public string? HostPhone { get; set; }
    public int? LocationId { get; set; }
    public string? LocationName { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public DateTime? ScheduledEndTime { get; set; }
    public bool IsOverdue { get; set; }
    public int MinutesOnSite { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// DTO for the "Who is in the building" report.
/// </summary>
public class WhoIsInBuildingReportDto
{
    public DateTime GeneratedAt { get; set; }
    public DateTime LastUpdated { get; set; }
    public int TotalVisitors { get; set; }
    public int OverdueVisitors { get; set; }
    public int? LocationId { get; set; }
    public string? LocationName { get; set; }
    public List<InBuildingVisitorDto> Occupants { get; set; } = new();
}
