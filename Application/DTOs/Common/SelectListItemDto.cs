namespace VisitorManagementSystem.Api.Application.DTOs.Common;

/// <summary>
/// Select list item data transfer object
/// </summary>
public class SelectListItemDto
{
    /// <summary>
    /// Item value
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Item display text
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Item description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether item is selected
    /// </summary>
    public bool Selected { get; set; } = false;

    /// <summary>
    /// Whether item is disabled
    /// </summary>
    public bool Disabled { get; set; } = false;

    /// <summary>
    /// Additional data
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();
}