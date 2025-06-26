using System.ComponentModel.DataAnnotations;
using System.Text;

namespace VisitorManagementSystem.Api.Domain.ValueObjects;

/// <summary>
/// Value object representing a physical address
/// </summary>
public class Address : IEquatable<Address>
{
    /// <summary>
    /// Street address line 1
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string? Street1 { get; private set; }

    /// <summary>
    /// Street address line 2 (optional)
    /// </summary>
    [MaxLength(100)]
    public string? Street2 { get; private set; }

    /// <summary>
    /// City name
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string? City { get; private set; }

    /// <summary>
    /// State or province
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string? State { get; private set; }

    /// <summary>
    /// Postal or ZIP code
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string?   PostalCode { get; private set; }

    /// <summary>
    /// Country name or code
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string? Country { get; private set; }

    /// <summary>
    /// Optional latitude coordinate
    /// </summary>
    public double? Latitude { get; private set; }

    /// <summary>
    /// Optional longitude coordinate
    /// </summary>
    public double? Longitude { get; private set; }

    /// <summary>
    /// Address type (Home, Work, Billing, Shipping, etc.)
    /// </summary>
    [MaxLength(20)]
    public string? AddressType { get; private set; }

    /// <summary>
    /// Whether this address has been validated
    /// </summary>
    public bool? IsValidated { get; private set; }

    /// <summary>
    /// Private constructor for Entity Framework
    /// </summary>
    private Address()
    {
        Street1 = string.Empty;
        City = string.Empty;
        State = string.Empty;
        PostalCode = string.Empty;
        Country = string.Empty;
        AddressType = "Unknown";
    }

    /// <summary>
    /// Creates a new Address value object
    /// </summary>
    /// <param name="street1">Street address line 1</param>
    /// <param name="city">City name</param>
    /// <param name="state">State or province</param>
    /// <param name="postalCode">Postal or ZIP code</param>
    /// <param name="country">Country</param>
    /// <param name="street2">Optional street address line 2</param>
    /// <param name="addressType">Type of address</param>
    /// <param name="latitude">Optional latitude</param>
    /// <param name="longitude">Optional longitude</param>
    public Address(
        string street1,
        string city,
        string state,
        string postalCode,
        string country,
        string? street2 = null,
        string addressType = "Unknown",
        double? latitude = null,
        double? longitude = null)
    {
        if (string.IsNullOrWhiteSpace(street1))
            throw new ArgumentException("Street address is required.", nameof(street1));

        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City is required.", nameof(city));

        if (string.IsNullOrWhiteSpace(state))
            throw new ArgumentException("State is required.", nameof(state));

        if (string.IsNullOrWhiteSpace(postalCode))
            throw new ArgumentException("Postal code is required.", nameof(postalCode));

        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country is required.", nameof(country));

        Street1 = street1.Trim();
        Street2 = string.IsNullOrWhiteSpace(street2) ? null : street2.Trim();
        City = city.Trim();
        State = state.Trim();
        PostalCode = postalCode.Trim().ToUpperInvariant();
        Country = country.Trim();
        AddressType = addressType;
        Latitude = latitude;
        Longitude = longitude;
        IsValidated = false;

        ValidateCoordinates();
        ValidatePostalCode();
    }

    /// <summary>
    /// Validates the coordinate values
    /// </summary>
    private void ValidateCoordinates()
    {
        if (Latitude.HasValue && (Latitude.Value < -90 || Latitude.Value > 90))
            throw new ArgumentException("Latitude must be between -90 and 90 degrees.");

        if (Longitude.HasValue && (Longitude.Value < -180 || Longitude.Value > 180))
            throw new ArgumentException("Longitude must be between -180 and 180 degrees.");
    }

    /// <summary>
    /// Validates the postal code format based on country
    /// </summary>
    private void ValidatePostalCode()
    {
        // Basic validation - in a real system, you'd have comprehensive postal code validation
        if (string.IsNullOrWhiteSpace(PostalCode))
            return;

        var normalizedCountry = Country.ToUpperInvariant();

        switch (normalizedCountry)
        {
            case "US" or "USA" or "UNITED STATES":
                ValidateUSPostalCode();
                break;
            case "CA" or "CAN" or "CANADA":
                ValidateCanadianPostalCode();
                break;
            case "UK" or "GB" or "UNITED KINGDOM" or "GREAT BRITAIN":
                ValidateUKPostalCode();
                break;
                // Add more countries as needed
        }
    }

    /// <summary>
    /// Validates US ZIP code format
    /// </summary>
    private void ValidateUSPostalCode()
    {
        // US ZIP: 12345 or 12345-6789
        var usZipRegex = @"^\d{5}(-\d{4})?$";
        if (!System.Text.RegularExpressions.Regex.IsMatch(PostalCode, usZipRegex))
            throw new ArgumentException($"Invalid US ZIP code format: {PostalCode}");
    }

    /// <summary>
    /// Validates Canadian postal code format
    /// </summary>
    private void ValidateCanadianPostalCode()
    {
        // Canadian: A1A 1A1
        var canadianRegex = @"^[A-Z]\d[A-Z] \d[A-Z]\d$";
        if (!System.Text.RegularExpressions.Regex.IsMatch(PostalCode, canadianRegex))
            throw new ArgumentException($"Invalid Canadian postal code format: {PostalCode}");
    }

    /// <summary>
    /// Validates UK postal code format
    /// </summary>
    private void ValidateUKPostalCode()
    {
        // UK postcode: SW1A 1AA, M1 1AA, B33 8TH, etc.
        var ukRegex = @"^[A-Z]{1,2}\d[A-Z\d]? \d[A-Z]{2}$";
        if (!System.Text.RegularExpressions.Regex.IsMatch(PostalCode, ukRegex))
            throw new ArgumentException($"Invalid UK postal code format: {PostalCode}");
    }

    /// <summary>
    /// Gets the full address as a single string
    /// </summary>
    /// <param name="includeName">Whether to include address type</param>
    /// <returns>Formatted address string</returns>
    public string GetFullAddress(bool includeName = false)
    {
        var sb = new StringBuilder();

        if (includeName && !string.IsNullOrWhiteSpace(AddressType) && AddressType != "Unknown")
        {
            sb.AppendLine($"{AddressType} Address:");
        }

        sb.AppendLine(Street1);

        if (!string.IsNullOrWhiteSpace(Street2))
        {
            sb.AppendLine(Street2);
        }

        sb.AppendLine($"{City}, {State} {PostalCode}");
        sb.AppendLine(Country);

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Gets the address formatted for mailing labels
    /// </summary>
    /// <returns>Mailing label formatted address</returns>
    public string GetMailingFormat()
    {
        var lines = new List<string> { Street1 };

        if (!string.IsNullOrWhiteSpace(Street2))
        {
            lines.Add(Street2);
        }

        lines.Add($"{City}, {State} {PostalCode}");
        lines.Add(Country.ToUpperInvariant());

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Gets a single line representation of the address
    /// </summary>
    /// <returns>Single line address</returns>
    public string GetSingleLine()
    {
        var parts = new List<string> { Street1 };

        if (!string.IsNullOrWhiteSpace(Street2))
        {
            parts.Add(Street2);
        }

        parts.Add(City);
        parts.Add(State);
        parts.Add(PostalCode);
        parts.Add(Country);

        return string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    /// <summary>
    /// Checks if the address has geographic coordinates
    /// </summary>
    /// <returns>True if both latitude and longitude are set</returns>
    public bool HasCoordinates()
    {
        return Latitude.HasValue && Longitude.HasValue;
    }

    /// <summary>
    /// Calculates the distance to another address (if both have coordinates)
    /// </summary>
    /// <param name="other">Other address</param>
    /// <returns>Distance in kilometers, or null if coordinates are missing</returns>
    public double? CalculateDistanceTo(Address other)
    {
        if (!HasCoordinates() || !other.HasCoordinates())
            return null;

        return CalculateHaversineDistance(
            Latitude!.Value, Longitude!.Value,
            other.Latitude!.Value, other.Longitude!.Value);
    }

    /// <summary>
    /// Calculates distance using the Haversine formula
    /// </summary>
    /// <param name="lat1">Latitude 1</param>
    /// <param name="lon1">Longitude 1</param>
    /// <param name="lat2">Latitude 2</param>
    /// <param name="lon2">Longitude 2</param>
    /// <returns>Distance in kilometers</returns>
    private static double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadius = 6371; // Earth radius in kilometers

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadius * c;
    }

    /// <summary>
    /// Converts degrees to radians
    /// </summary>
    /// <param name="degrees">Degrees</param>
    /// <returns>Radians</returns>
    private static double ToRadians(double degrees)
    {
        return degrees * (Math.PI / 180);
    }

    /// <summary>
    /// Creates a new address with coordinates
    /// </summary>
    /// <param name="latitude">Latitude</param>
    /// <param name="longitude">Longitude</param>
    /// <returns>New address with coordinates</returns>
    public Address WithCoordinates(double latitude, double longitude)
    {
        return new Address(Street1, City, State, PostalCode, Country, Street2, AddressType, latitude, longitude)
        {
            IsValidated = IsValidated
        };
    }

    /// <summary>
    /// Creates a new address marked as validated
    /// </summary>
    /// <returns>New address marked as validated</returns>
    public Address MarkAsValidated()
    {
        return new Address(Street1, City, State, PostalCode, Country, Street2, AddressType, Latitude, Longitude)
        {
            IsValidated = true
        };
    }

    /// <summary>
    /// Checks if this address is in the same city as another address
    /// </summary>
    /// <param name="other">Other address</param>
    /// <returns>True if same city</returns>
    public bool IsSameCity(Address other)
    {
        return string.Equals(City, other.City, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(State, other.State, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(Country, other.Country, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if this address is in the same country as another address
    /// </summary>
    /// <param name="other">Other address</param>
    /// <returns>True if same country</returns>
    public bool IsSameCountry(Address other)
    {
        return string.Equals(Country, other.Country, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the country code (ISO format if possible)
    /// </summary>
    /// <returns>Country code</returns>
    public string GetCountryCode()
    {
        return Country.ToUpperInvariant() switch
        {
            "UNITED STATES" or "USA" => "US",
            "CANADA" or "CAN" => "CA",
            "UNITED KINGDOM" or "GREAT BRITAIN" or "GB" => "UK",
            "GERMANY" or "DEUTSCHLAND" => "DE",
            "FRANCE" => "FR",
            "ITALY" => "IT",
            "SPAIN" => "ES",
            "JAPAN" => "JP",
            "CHINA" => "CN",
            "INDIA" => "IN",
            "AUSTRALIA" => "AU",
            _ => Country.Length <= 3 ? Country.ToUpperInvariant() : Country
        };
    }

    #region Equality and Operators

    public bool Equals(Address? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return string.Equals(Street1, other.Street1, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(Street2, other.Street2, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(City, other.City, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(State, other.State, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(PostalCode, other.PostalCode, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(Country, other.Country, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return obj is Address other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            StringComparer.OrdinalIgnoreCase.GetHashCode(Street1),
            StringComparer.OrdinalIgnoreCase.GetHashCode(Street2 ?? ""),
            StringComparer.OrdinalIgnoreCase.GetHashCode(City),
            StringComparer.OrdinalIgnoreCase.GetHashCode(State),
            StringComparer.OrdinalIgnoreCase.GetHashCode(PostalCode),
            StringComparer.OrdinalIgnoreCase.GetHashCode(Country));
    }

    public static bool operator ==(Address? left, Address? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Address? left, Address? right)
    {
        return !Equals(left, right);
    }

    #endregion

    public override string ToString()
    {
        return GetSingleLine();
    }
}