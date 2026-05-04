namespace VisitorManagementSystem.Api.Application.DTOs.CivilDefense;

public class CdOcrResultDto
{
    public bool Success { get; set; }
    public string? FullName { get; set; }
    public string? NationalId { get; set; }
    public string? Nationality { get; set; }
    public string? DateOfBirth { get; set; }
    public string? ExpiryDate { get; set; }
    public string? RawText { get; set; }
    public string? DocumentType { get; set; }
    public string? ErrorMessage { get; set; }
    public double Confidence { get; set; }
}
