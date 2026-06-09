using PhoneNumbers;

namespace VisitorManagementSystem.Api.Application.Common;

/// <summary>
/// Normalizes phone numbers to E.164 format using Google's libphonenumber.
/// Handles Lebanese trunk code stripping, country code duplication prevention,
/// and international number parsing for all supported countries.
/// </summary>
public static class PhoneNormalizer
{
    private static readonly PhoneNumberUtil PhoneUtil = PhoneNumberUtil.GetInstance();

    /// <summary>
    /// Tries to normalize a phone number to E.164 format (e.g. "+9613988760").
    /// </summary>
    /// <param name="rawNumber">Raw input as entered by the user (e.g. "03988760", "+9613988760", "225541125").</param>
    /// <param name="dialCode">Dial code without the + sign (e.g. "961", "971"). Ignored if rawNumber starts with '+'.</param>
    /// <returns>E.164 string such as "+9613988760", or null if the number cannot be parsed or is invalid.</returns>
    public static string? TryNormalizeToE164(string? rawNumber, string? dialCode)
    {
        if (string.IsNullOrWhiteSpace(rawNumber))
            return null;

        rawNumber = rawNumber.Trim();

        try
        {
            PhoneNumber parsed;

            if (rawNumber.StartsWith('+'))
            {
                // Number already has an international prefix — parse directly.
                // dialCode is intentionally ignored here to prevent duplication
                // (e.g. "+9613988760" with dialCode "961" must NOT become "+961+9613988760").
                parsed = PhoneUtil.Parse(rawNumber, null);
            }
            else if (!string.IsNullOrWhiteSpace(dialCode) &&
                     int.TryParse(dialCode, out var countryCallingCode))
            {
                // Map the numeric dial code to a region so libphonenumber can apply
                // country-specific trunk-prefix rules (e.g. Lebanon strips leading "0").
                var region = PhoneUtil.GetRegionCodeForCountryCode(countryCallingCode);

                // "ZZ" = unknown, "001" = non-geographic (satellite, etc.)
                if (string.IsNullOrEmpty(region) || region == "ZZ" || region == "001")
                    region = null;

                parsed = PhoneUtil.Parse(rawNumber, region);
            }
            else
            {
                // No region hint — try to parse as-is (works for full international strings
                // that somehow lack the leading '+', e.g. "9613988760").
                parsed = PhoneUtil.Parse(rawNumber, null);
            }

            if (!PhoneUtil.IsValidNumber(parsed))
                return null;

            return PhoneUtil.Format(parsed, PhoneNumberFormat.E164);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Strips all non-digit characters from a phone string. Useful for digit-only
    /// comparisons in search queries.
    /// </summary>
    public static string DigitsOnly(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;
        return new string(phone.Where(char.IsDigit).ToArray());
    }
}
