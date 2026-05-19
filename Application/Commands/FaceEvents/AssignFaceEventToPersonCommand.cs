using MediatR;

namespace VisitorManagementSystem.Api.Application.Commands.FaceEvents;

public class AssignFaceEventToPersonCommand : IRequest<AssignFaceEventToPersonResult>
{
    public int FaceEventId { get; set; }
    public string PersonType { get; set; } = string.Empty;
    public int PersonId { get; set; }
    public bool EnrollSnapshot { get; set; }
    public string? Notes { get; set; }
    public int ReviewedById { get; set; }
}

public class AssignFaceEventToPersonResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int? TemplateId { get; set; }
    public decimal? NewTemplateQuality { get; set; }
    public decimal? PrimaryTemplateQuality { get; set; }
    public bool SuggestPromoteToPrimary { get; set; }
}
