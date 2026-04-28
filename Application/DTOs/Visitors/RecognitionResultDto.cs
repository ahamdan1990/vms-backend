namespace VisitorManagementSystem.Api.Application.DTOs.Visitors;

/// <summary>
/// Result of a face recognition + classification check.
/// Returned from POST /api/Visitors/recognize.
/// </summary>
public class RecognitionResultDto
{
    public bool IsRecognized { get; set; }
    public double? Similarity { get; set; }
    public VisitorDto? Visitor { get; set; }

    // Classification flags — populated only when IsRecognized == true
    public bool IsBlacklisted { get; set; }
    public bool IsVip { get; set; }
    public string? BlacklistReason { get; set; }

    // Whether an alert was already triggered server-side
    public bool AlertTriggered { get; set; }
    public string? AlertMessage { get; set; }
}
