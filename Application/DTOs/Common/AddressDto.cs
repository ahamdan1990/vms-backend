using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Application.DTOs.Common;

/// <summary>
/// Data transfer object for address information
/// </summary>
public class AddressDto
{
    [MaxLength(100)]
    public string? Street1 { get; set; }

    [MaxLength(100)]
    public string? Street2 { get; set; }

    [MaxLength(50)]
    public string? City { get; set; }

    [MaxLength(50)]
    public string? State { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(50)]
    public string? Country { get; set; }

    public string FormattedAddress { get; set; } = string.Empty;
}

/// <summary>
/// Data transfer object for creating address information
/// </summary>
public class CreateAddressDto
{
    [MaxLength(100)]
    public string? Street1 { get; set; }

    [MaxLength(100)]
    public string? Street2 { get; set; }

    [MaxLength(50)]
    public string? City { get; set; }

    [MaxLength(50)]
    public string? State { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(50)]
    public string? Country { get; set; }
}