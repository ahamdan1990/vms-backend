namespace VisitorManagementSystem.Api.Application.DTOs.Visitors;

/// <summary>
/// DTO for visitor search parameters
/// </summary>
public class VisitorSearchDto
{
    /// <summary>
    /// Search term for name, email, or company
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Company filter
    /// </summary>
    public string? Company { get; set; }

    /// <summary>
    /// VIP status filter
    /// </summary>
    public bool? IsVip { get; set; }

    /// <summary>
    /// Blacklisted status filter
    /// </summary>
    public bool? IsBlacklisted { get; set; }

    /// <summary>
    /// Active status filter
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Nationality filter
    /// </summary>
    public string? Nationality { get; set; }

    /// <summary>
    /// Security clearance filter
    /// </summary>
    public string? SecurityClearance { get; set; }

    /// <summary>
    /// Minimum visit count
    /// </summary>
    public int? MinVisitCount { get; set; }

    /// <summary>
    /// Maximum visit count
    /// </summary>
    public int? MaxVisitCount { get; set; }

    /// <summary>
    /// Created date from
    /// </summary>
    public DateTime? CreatedFrom { get; set; }

    /// <summary>
    /// Created date to
    /// </summary>
    public DateTime? CreatedTo { get; set; }

    /// <summary>
    /// Last visit date from
    /// </summary>
    public DateTime? LastVisitFrom { get; set; }

    /// <summary>
    /// Last visit date to
    /// </summary>
    public DateTime? LastVisitTo { get; set; }

    /// <summary>
    /// Page index for pagination
    /// </summary>
    public int PageIndex { get; set; } = 0;

    /// <summary>
    /// Page size for pagination
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Sort field
    /// </summary>
    public string SortBy { get; set; } = "FullName";

    /// <summary>
    /// Sort direction
    /// </summary>
    public string SortDirection { get; set; } = "asc";

    /// <summary>
    /// Include deleted visitors
    /// </summary>
    public bool IncludeDeleted { get; set; } = false;
}
