namespace VisitorManagementSystem.Api.Application.DTOs.CivilDefense;

public class CivilDefenseReportEntryDto
{
    public string EntryType { get; set; } = string.Empty; // "Staff" | "Visitor"
    public int PersonId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? CompanyOrDepartment { get; set; }
    public bool? IsCivilian { get; set; }
    public string? AffiliatedOrganization { get; set; }
    public string? HostName { get; set; }
    public string? Purpose { get; set; }
    public string? LocationName { get; set; }
    public DateTime CheckedInAt { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
}

public class CivilDefenseReportDto
{
    public List<CivilDefenseReportEntryDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
