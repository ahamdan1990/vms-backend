using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Domain.Entities;

/// <summary>
/// Stored face-recognition template for a visitor or staff/user identity.
/// </summary>
public class FaceTemplate : SoftDeleteEntity
{
    [Required]
    [MaxLength(20)]
    public string PersonType { get; set; } = "Visitor";

    public int PersonId { get; set; }

    public int? VisitorId { get; set; }

    public int? UserId { get; set; }

    [Required]
    [MaxLength(128)]
    public string SubjectId { get; set; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string Engine { get; set; } = "Luxand";

    public byte[] TemplateData { get; set; } = Array.Empty<byte>();

    public int TemplateSize { get; set; }

    public bool IsPrimary { get; set; }

    public decimal? QualityScore { get; set; }

    [MaxLength(500)]
    public string? SourceImagePath { get; set; }

    [MaxLength(64)]
    public string? Source { get; set; }

    [MaxLength(128)]
    public string? ExternalImageId { get; set; }

    public DateTime? LastMatchedAt { get; set; }

    public int MatchCount { get; set; }

    public string? Metadata { get; set; }

    public virtual Visitor? Visitor { get; set; }

    public virtual User? User { get; set; }
}
