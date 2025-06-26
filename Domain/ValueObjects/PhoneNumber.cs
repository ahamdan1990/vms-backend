using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace VisitorManagementSystem.Api.Domain.ValueObjects;

/// <summary>
/// Value object representing a phone number with validation and formatting
/// </summary>
public class PhoneNumber : IEquatable<PhoneNumber>
{
    private static readonly Regex PhoneRegex = new(
        @"^(\+\d{1,3}[- ]?)?\(?([0-9]{3})\)?[-. ]?([0-9]{3})[-. ]?([0-9]{4})$",
        RegexOptions.Compiled);

    private static readonly Regex DigitsOnlyRegex = new(@"\D", RegexOptions.Compiled);

    /// <summary>
    /// The raw phone number value as provided
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Value { get; private set; }

    /// <summary>
    /// The formatted phone number for display
    /// </summary>
    [MaxLength(25)]
    public string FormattedValue { get; private set; }

    /// <summary>
    /// The phone number with only digits
    /// </summary>
    [MaxLength(15)]
    public string DigitsOnly { get; private set; }

    /// <summary>
    /// The country code (if detected)
    /// </summary>
    [MaxLength(4)]
    public string? CountryCode { get; private set; }

    /// <summary>
    /// The area code
    /// </summary>
    [MaxLength(4)]
    public string? AreaCode { get; private set; }

    /// <summary>
    /// The phone number type (Mobile, Landline, Unknown)
    /// </summary>
    [MaxLength(10)]
    public string PhoneType { get; private set; }

    /// <summary>
    /// Whether the phone number is verified
    /// </summary>
    public bool IsVerified { get; private set; }

    /// <summary>
    /// Private constructor for Entity Framework
    /// </summary>
    private PhoneNumber()
    {
        Value = string.Empty;
        FormattedValue = string.Empty;
        DigitsOnly = string.Empty;
        PhoneType = "Unknown";
    }

    /// <summary>
    /// Creates a new PhoneNumber value object
    /// </summary>
    /// <param name="value">The phone number</param>
    /// <param name="countryCode">Optional country code if not included in the number</param>
    /// <exception cref="ArgumentException">Thrown when phone number is invalid</exception>
    public PhoneNumber(string value, string? countryCode = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Phone number cannot be null or empty.", nameof(value));

        value = value.Trim();

        if (!IsValidPhoneNumber(value))
            throw new ArgumentException($"Invalid phone number format: {value}", nameof(value));

        Value = value;
        DigitsOnly = ExtractDigitsOnly(value);

        // Parse country code and area code
        ParsePhoneComponents(value, countryCode);

        // Format the phone number
        FormattedValue = FormatPhoneNumber();

        // Determine phone type
        PhoneType = DeterminePhoneType();

        IsVerified = false;
    }

    /// <summary>
    /// Validates a phone number format
    /// </summary>
    /// <param name="phoneNumber">Phone number to validate</param>
    /// <returns>True if the phone number format is valid</returns>
    public static bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        // Remove all non-digit characters for length check
        var digitsOnly = DigitsOnlyRegex.Replace(phoneNumber, "");

        // Phone number should have between 7 and 15 digits
        if (digitsOnly.Length < 7 || digitsOnly.Length > 15)
            return false;

        // Check various formats
        return PhoneRegex.IsMatch(phoneNumber) ||
               IsValidInternationalFormat(phoneNumber) ||
               IsValidSimpleFormat(phoneNumber);
    }

    /// <summary>
    /// Checks if the phone number is in valid international format
    /// </summary>
    /// <param name="phoneNumber">Phone number to check</param>
    /// <returns>True if valid international format</returns>
    private static bool IsValidInternationalFormat(string phoneNumber)
    {
        // International format: +[country code][number]
        if (!phoneNumber.StartsWith('+'))
            return false;

        var digitsOnly = DigitsOnlyRegex.Replace(phoneNumber.Substring(1), "");
        return digitsOnly.Length >= 7 && digitsOnly.Length <= 14;
    }

    /// <summary>
    /// Checks if the phone number is in valid simple format
    /// </summary>
    /// <param name="phoneNumber">Phone number to check</param>
    /// <returns>True if valid simple format</returns>
    private static bool IsValidSimpleFormat(string phoneNumber)
    {
        var digitsOnly = DigitsOnlyRegex.Replace(phoneNumber, "");
        return digitsOnly.Length >= 7 && digitsOnly.Length <= 15;
    }

    /// <summary>
    /// Extracts only digits from the phone number
    /// </summary>
    /// <param name="phoneNumber">Phone number</param>
    /// <returns>Digits only</returns>
    private static string ExtractDigitsOnly(string phoneNumber)
    {
        return DigitsOnlyRegex.Replace(phoneNumber, "");
    }

    /// <summary>
    /// Parses phone number components (country code, area code)
    /// </summary>
    /// <param name="phoneNumber">Phone number</param>
    /// <param name="defaultCountryCode">Default country code</param>
    private void ParsePhoneComponents(string phoneNumber, string? defaultCountryCode)
    {
        var digits = DigitsOnly;

        // Detect country code
        if (phoneNumber.StartsWith('+'))
        {
            // International format
            if (digits.StartsWith("1") && digits.Length == 11)
            {
                CountryCode = "1"; // US/Canada
                AreaCode = digits.Substring(1, 3);
            }
            else if (digits.StartsWith("44") && digits.Length >= 10)
            {
                CountryCode = "44"; // UK
            }
            else if (digits.StartsWith("91") && digits.Length >= 10)
            {
                CountryCode = "91"; // India
            }
            else
            {
                // Try to extract country code (1-4 digits)
                for (int i = 1; i <= Math.Min(4, digits.Length - 7); i++)
                {
                    var potentialCode = digits.Substring(0, i);
                    if (IsValidCountryCode(potentialCode))
                    {
                        CountryCode = potentialCode;
                        break;
                    }
                }
            }
        }
        else if (!string.IsNullOrWhiteSpace(defaultCountryCode))
        {
            CountryCode = defaultCountryCode;
        }
        else if (digits.Length == 10)
        {
            // Assume US format
            CountryCode = "1";
            AreaCode = digits.Substring(0, 3);
        }

        // Extract area code if not already set
        if (string.IsNullOrWhiteSpace(AreaCode) && !string.IsNullOrWhiteSpace(CountryCode))
        {
            var nationalNumber = digits.Substring(CountryCode.Length);
            if (nationalNumber.Length >= 7)
            {
                AreaCode = nationalNumber.Substring(0, 3);
            }
        }
    }

    /// <summary>
    /// Checks if a string is a valid country code
    /// </summary>
    /// <param name="code">Potential country code</param>
    /// <returns>True if valid country code</returns>
    private static bool IsValidCountryCode(string code)
    {
        var validCodes = new[] { "1", "7", "20", "27", "30", "31", "32", "33", "34", "36", "39", "40", "41", "43", "44", "45", "46", "47", "48", "49", "51", "52", "53", "54", "55", "56", "57", "58", "60", "61", "62", "63", "64", "65", "66", "81", "82", "84", "86", "90", "91", "92", "93", "94", "95", "98", "212", "213", "216", "218", "220", "221", "222", "223", "224", "225", "226", "227", "228", "229", "230", "231", "232", "233", "234", "235", "236", "237", "238", "239", "240", "241", "242", "243", "244", "245", "246", "247", "248", "249", "250", "251", "252", "253", "254", "255", "256", "257", "258", "260", "261", "262", "263", "264", "265", "266", "267", "268", "269", "290", "291", "297", "298", "299", "350", "351", "352", "353", "354", "355", "356", "357", "358", "359", "370", "371", "372", "373", "374", "375", "376", "377", "378", "380", "381", "382", "383", "385", "386", "387", "389", "420", "421", "423", "500", "501", "502", "503", "504", "505", "506", "507", "508", "509", "590", "591", "592", "593", "594", "595", "596", "597", "598", "599", "670", "672", "673", "674", "675", "676", "677", "678", "679", "680", "681", "682", "683", "684", "685", "686", "687", "688", "689", "690", "691", "692", "850", "852", "853", "855", "856", "880", "886", "960", "961", "962", "963", "964", "965", "966", "967", "968", "970", "971", "972", "973", "974", "975", "976", "977", "992", "993", "994", "995", "996", "998" };
        return validCodes.Contains(code);
    }

    /// <summary>
    /// Formats the phone number for display
    /// </summary>
    /// <returns>Formatted phone number</returns>
    private string FormatPhoneNumber()
    {
        if (string.IsNullOrWhiteSpace(CountryCode))
        {
            // Format as local number
            if (DigitsOnly.Length == 10)
            {
                return $"({DigitsOnly.Substring(0, 3)}) {DigitsOnly.Substring(3, 3)}-{DigitsOnly.Substring(6)}";
            }
            else if (DigitsOnly.Length == 7)
            {
                return $"{DigitsOnly.Substring(0, 3)}-{DigitsOnly.Substring(3)}";
            }
        }
        else
        {
            // Format with country code
            var nationalNumber = DigitsOnly.Substring(CountryCode.Length);
            if (CountryCode == "1" && nationalNumber.Length == 10)
            {
                return $"+1 ({nationalNumber.Substring(0, 3)}) {nationalNumber.Substring(3, 3)}-{nationalNumber.Substring(6)}";
            }
            else
            {
                return $"+{CountryCode} {nationalNumber}";
            }
        }

        return Value; // Fallback to original value
    }

    /// <summary>
    /// Determines the type of phone number
    /// </summary>
    /// <returns>Phone type (Mobile, Landline, Unknown)</returns>
    private string DeterminePhoneType()
    {
        if (string.IsNullOrWhiteSpace(CountryCode) || string.IsNullOrWhiteSpace(AreaCode))
            return "Unknown";

        // US mobile prefixes (simplified)
        if (CountryCode == "1")
        {
            var mobilePrefixes = new[] { "201", "202", "203", "205", "206", "207", "208", "209", "210", "212", "213", "214", "215", "216", "217", "218", "219", "224", "225", "227", "228", "229", "231", "234", "239", "240", "248", "251", "252", "253", "254", "256", "260", "262", "267", "269", "270", "276", "281", "283", "301", "302", "303", "304", "305", "307", "308", "309", "310", "312", "313", "314", "315", "316", "317", "318", "319", "320", "321", "323", "325", "330", "331", "334", "336", "337", "339", "347", "351", "352", "360", "361", "364", "369", "380", "385", "386", "401", "402", "404", "405", "406", "407", "408", "409", "410", "412", "413", "414", "415", "417", "419", "423", "424", "425", "430", "432", "434", "435", "440", "442", "443", "445", "447", "458", "463", "464", "469", "470", "475", "478", "479", "480", "484", "501", "502", "503", "504", "505", "507", "508", "509", "510", "512", "513", "515", "516", "517", "518", "520", "530", "540", "541", "551", "559", "561", "562", "563", "564", "567", "570", "571", "573", "574", "575", "580", "585", "586", "601", "602", "603", "605", "606", "607", "608", "609", "610", "612", "614", "615", "616", "617", "618", "619", "620", "623", "626", "628", "629", "630", "631", "636", "641", "646", "650", "651", "657", "660", "661", "662", "667", "669", "678", "681", "682", "701", "702", "703", "704", "706", "707", "708", "712", "713", "714", "715", "716", "717", "718", "719", "720", "724", "725", "727", "731", "732", "734", "737", "740", "743", "747", "754", "757", "760", "762", "763", "765", "769", "770", "772", "773", "774", "775", "779", "781", "785", "786", "787", "801", "802", "803", "804", "805", "806", "808", "810", "812", "813", "814", "815", "816", "817", "818", "828", "830", "831", "832", "843", "845", "847", "848", "850", "856", "857", "858", "859", "860", "862", "863", "864", "865", "870", "872", "878", "901", "903", "904", "906", "907", "908", "909", "910", "912", "913", "914", "915", "916", "917", "918", "919", "920", "925", "928", "929", "930", "931", "934", "936", "937", "940", "941", "947", "949", "951", "952", "954", "956", "959", "970", "971", "972", "973", "978", "979", "980", "984", "985", "989" };

            // This is a simplified check - in reality, mobile vs landline determination is more complex
            return "Unknown";
        }

        return "Unknown";
    }

    /// <summary>
    /// Marks the phone number as verified
    /// </summary>
    /// <returns>New PhoneNumber instance with verified status</returns>
    public PhoneNumber MarkAsVerified()
    {
        return new PhoneNumber(Value, CountryCode)
        {
            IsVerified = true
        };
    }

    /// <summary>
    /// Gets the international format of the phone number
    /// </summary>
    /// <returns>Phone number in international format</returns>
    public string GetInternationalFormat()
    {
        if (!string.IsNullOrWhiteSpace(CountryCode))
        {
            return $"+{CountryCode}{DigitsOnly.Substring(CountryCode.Length)}";
        }

        return DigitsOnly;
    }

    /// <summary>
    /// Gets the national format of the phone number
    /// </summary>
    /// <returns>Phone number in national format</returns>
    public string GetNationalFormat()
    {
        if (!string.IsNullOrWhiteSpace(CountryCode))
        {
            return DigitsOnly.Substring(CountryCode.Length);
        }

        return DigitsOnly;
    }

    /// <summary>
    /// Checks if this is a mobile number
    /// </summary>
    /// <returns>True if mobile number</returns>
    public bool IsMobile()
    {
        return PhoneType == "Mobile";
    }

    /// <summary>
    /// Checks if this is a landline number
    /// </summary>
    /// <returns>True if landline number</returns>
    public bool IsLandline()
    {
        return PhoneType == "Landline";
    }

    #region Equality and Operators

    public bool Equals(PhoneNumber? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(DigitsOnly, other.DigitsOnly, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return obj is PhoneNumber other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(DigitsOnly);
    }

    public static bool operator ==(PhoneNumber? left, PhoneNumber? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(PhoneNumber? left, PhoneNumber? right)
    {
        return !Equals(left, right);
    }

    public static implicit operator string(PhoneNumber phoneNumber)
    {
        return phoneNumber.FormattedValue;
    }

    public static explicit operator PhoneNumber(string phoneNumber)
    {
        return new PhoneNumber(phoneNumber);
    }

    #endregion

    public override string ToString()
    {
        return FormattedValue;
    }
}