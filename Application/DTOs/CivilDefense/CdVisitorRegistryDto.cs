namespace VisitorManagementSystem.Api.Application.DTOs.CivilDefense;

public class CdVisitorRegistryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public bool IsVip { get; set; }
    public bool IsBlacklisted { get; set; }
    public string? BlacklistReason { get; set; }
    public int VisitCount { get; set; }
    public DateTime? LastVisitDate { get; set; }
    public string? ProfilePhotoUrl { get; set; }
}

public class CdVisitorRegistryResultDto
{
    public List<CdVisitorRegistryDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
}
