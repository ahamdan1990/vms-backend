namespace VisitorManagementSystem.Api.Application.DTOs.CivilDefense;

public class CdLiveOccupancyDto
{
    public int TotalInside { get; set; }
    public int StaffInside { get; set; }
    public int VisitorsInside { get; set; }
    public DateTime AsOf { get; set; }
    public List<CdOccupantDto> Occupants { get; set; } = [];
}
