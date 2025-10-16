using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Domain.ValueObjects;

/// <summary>
/// Value object representing complete contact information
/// </summary>
public class ContactInfo : IEquatable<ContactInfo>
{
    /// <summary>
    /// Primary email address
    /// </summary>
    public Email? PrimaryEmail { get; private set; }

    /// <summary>
    /// Secondary email address
    /// </summary>
    public Email? SecondaryEmail { get; private set; }

    /// <summary>
    /// Primary phone number
    /// </summary>
    public PhoneNumber? PrimaryPhone { get; private set; }

    /// <summary>
    /// Secondary phone number
    /// </summary>
    public PhoneNumber? SecondaryPhone { get; private set; }

    /// <summary>
    /// Mobile phone number
    /// </summary>
    public PhoneNumber? MobilePhone { get; private set; }

    /// <summary>
    /// Work phone number
    /// </summary>
    public PhoneNumber? WorkPhone { get; private set; }

    /// <summary>
    /// Fax number
    /// </summary>
    public PhoneNumber? FaxNumber { get; private set; }

    /// <summary>
    /// Primary address
    /// </summary>
    public Address? PrimaryAddress { get; private set; }

    /// <summary>
    /// Work address
    /// </summary>
    public Address? WorkAddress { get; private set; }

    /// <summary>
    /// Billing address
    /// </summary>
    public Address? BillingAddress { get; private set; }

    /// <summary>
    /// Website URL
    /// </summary>
    [MaxLength(200)]
    public string? Website { get; private set; }

    /// <summary>
    /// LinkedIn profile URL
    /// </summary>
    [MaxLength(200)]
    public string? LinkedIn { get; private set; }

    /// <summary>
    /// Twitter handle
    /// </summary>
    [MaxLength(50)]
    public string? Twitter { get; private set; }

    /// <summary>
    /// Preferred contact method
    /// </summary>
    [MaxLength(20)]
    public string PreferredContactMethod { get; private set; }

    /// <summary>
    /// Best time to contact
    /// </summary>
    [MaxLength(50)]
    public string? BestTimeToContact { get; private set; }

    /// <summary>
    /// Time zone for contact
    /// </summary>
    [MaxLength(50)]
    public string? TimeZone { get; private set; }

    /// <summary>
    /// Language preference for communication
    /// </summary>
    [MaxLength(10)]
    public string LanguagePreference { get; private set; }

    /// <summary>
    /// Special contact instructions
    /// </summary>
    [MaxLength(500)]
    public string? ContactInstructions { get; private set; }

    /// <summary>
    /// Whether contact info has been verified
    /// </summary>
    public bool IsVerified { get; private set; }

    /// <summary>
    /// Date when contact info was last verified
    /// </summary>
    public DateTime? LastVerifiedDate { get; private set; }

    /// <summary>
    /// Private constructor for Entity Framework
    /// </summary>
    private ContactInfo()
    {
        PreferredContactMethod = "Email";
        LanguagePreference = "en-US";
    }

    /// <summary>
    /// Creates a new ContactInfo value object
    /// </summary>
    /// <param name="primaryEmail">Primary email address</param>
    /// <param name="primaryPhone">Primary phone number</param>
    /// <param name="primaryAddress">Primary address</param>
    /// <param name="preferredContactMethod">Preferred contact method</param>
    /// <param name="languagePreference">Language preference</param>
    public ContactInfo(
        Email? primaryEmail = null,
        PhoneNumber? primaryPhone = null,
        Address? primaryAddress = null,
        string preferredContactMethod = "Email",
        string languagePreference = "en-US")
    {
        PrimaryEmail = primaryEmail;
        PrimaryPhone = primaryPhone;
        PrimaryAddress = primaryAddress;
        PreferredContactMethod = preferredContactMethod;
        LanguagePreference = languagePreference;
        IsVerified = false;

        ValidateContactInfo();
    }

    /// <summary>
    /// Validates that at least one contact method is provided
    /// </summary>
    private void ValidateContactInfo()
    {
        if (PrimaryEmail == null && PrimaryPhone == null && PrimaryAddress == null)
        {
            throw new ArgumentException("At least one contact method (email, phone, or address) must be provided.");
        }

        ValidatePreferredContactMethod();
    }

    /// <summary>
    /// Validates the preferred contact method
    /// </summary>
    private void ValidatePreferredContactMethod()
    {
        var validMethods = new[] { "Email", "Phone", "SMS", "Mail", "Website", "LinkedIn", "Twitter" };

        if (!validMethods.Contains(PreferredContactMethod, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Invalid preferred contact method: {PreferredContactMethod}");
        }

        // Validate that the preferred method is available
        switch (PreferredContactMethod.ToLowerInvariant())
        {
            case "email" when PrimaryEmail == null && SecondaryEmail == null:
                throw new ArgumentException("Email is preferred contact method but no email address is provided.");

            case "phone" when PrimaryPhone == null && SecondaryPhone == null && MobilePhone == null && WorkPhone == null:
                throw new ArgumentException("Phone is preferred contact method but no phone number is provided.");

            case "sms" when MobilePhone == null && PrimaryPhone?.IsMobile() != true:
                throw new ArgumentException("SMS is preferred contact method but no mobile phone number is provided.");

            case "mail" when PrimaryAddress == null:
                throw new ArgumentException("Mail is preferred contact method but no address is provided.");

            case "website" when string.IsNullOrWhiteSpace(Website):
                throw new ArgumentException("Website is preferred contact method but no website URL is provided.");

            case "linkedin" when string.IsNullOrWhiteSpace(LinkedIn):
                throw new ArgumentException("LinkedIn is preferred contact method but no LinkedIn profile is provided.");

            case "twitter" when string.IsNullOrWhiteSpace(Twitter):
                throw new ArgumentException("Twitter is preferred contact method but no Twitter handle is provided.");
        }
    }

    /// <summary>
    /// Gets the primary email address
    /// </summary>
    /// <returns>Primary email or secondary if primary is null</returns>
    public Email? GetPrimaryEmail()
    {
        return PrimaryEmail ?? SecondaryEmail;
    }

    /// <summary>
    /// Gets the primary phone number
    /// </summary>
    /// <returns>Primary phone or mobile if primary is null</returns>
    public PhoneNumber? GetPrimaryPhone()
    {
        return PrimaryPhone ?? MobilePhone ?? WorkPhone ?? SecondaryPhone;
    }

    /// <summary>
    /// Gets the primary address
    /// </summary>
    /// <returns>Primary address or work address if primary is null</returns>
    public Address? GetPrimaryAddress()
    {
        return PrimaryAddress ?? WorkAddress;
    }

    /// <summary>
    /// Gets all email addresses
    /// </summary>
    /// <returns>List of all email addresses</returns>
    public List<Email> GetAllEmails()
    {
        var emails = new List<Email>();

        if (PrimaryEmail != null) emails.Add(PrimaryEmail);
        if (SecondaryEmail != null) emails.Add(SecondaryEmail);

        return emails;
    }

    /// <summary>
    /// Gets all phone numbers
    /// </summary>
    /// <returns>List of all phone numbers</returns>
    public List<PhoneNumber> GetAllPhoneNumbers()
    {
        var phones = new List<PhoneNumber>();

        if (PrimaryPhone != null) phones.Add(PrimaryPhone);
        if (SecondaryPhone != null) phones.Add(SecondaryPhone);
        if (MobilePhone != null) phones.Add(MobilePhone);
        if (WorkPhone != null) phones.Add(WorkPhone);
        if (FaxNumber != null) phones.Add(FaxNumber);

        return phones;
    }

    /// <summary>
    /// Gets all addresses
    /// </summary>
    /// <returns>List of all addresses</returns>
    public List<Address> GetAllAddresses()
    {
        var addresses = new List<Address>();

        if (PrimaryAddress != null) addresses.Add(PrimaryAddress);
        if (WorkAddress != null) addresses.Add(WorkAddress);
        if (BillingAddress != null) addresses.Add(BillingAddress);

        return addresses;
    }

    /// <summary>
    /// Checks if any contact method is verified
    /// </summary>
    /// <returns>True if any contact method is verified</returns>
    public bool HasVerifiedContact()
    {
        var emails = GetAllEmails();
        var phones = GetAllPhoneNumbers();

        return emails.Any() || phones.Any(p => p.IsVerified) || IsVerified;
    }

    /// <summary>
    /// Gets the preferred contact information based on the preferred method
    /// </summary>
    /// <returns>Contact information for preferred method</returns>
    public string? GetPreferredContactInfo()
    {
        return PreferredContactMethod.ToLowerInvariant() switch
        {
            "email" => GetPrimaryEmail()?.Value,
            "phone" => GetPrimaryPhone()?.FormattedValue,
            "sms" => MobilePhone?.FormattedValue ?? GetPrimaryPhone()?.FormattedValue,
            "mail" => GetPrimaryAddress()?.GetSingleLine(),
            "website" => Website,
            "linkedin" => LinkedIn,
            "twitter" => Twitter,
            _ => GetPrimaryEmail()?.Value ?? GetPrimaryPhone()?.FormattedValue
        };
    }

    /// <summary>
    /// Creates a new ContactInfo with additional email
    /// </summary>
    /// <param name="email">Email to add</param>
    /// <param name="isPrimary">Whether this should be the primary email</param>
    /// <returns>New ContactInfo with the email added</returns>
    public ContactInfo WithEmail(Email email, bool isPrimary = false)
    {
        var newContactInfo = new ContactInfo(
            isPrimary ? email : PrimaryEmail,
            PrimaryPhone,
            PrimaryAddress,
            PreferredContactMethod,
            LanguagePreference)
        {
            SecondaryEmail = isPrimary ? PrimaryEmail : (SecondaryEmail ?? email),
            SecondaryPhone = SecondaryPhone,
            MobilePhone = MobilePhone,
            WorkPhone = WorkPhone,
            FaxNumber = FaxNumber,
            WorkAddress = WorkAddress,
            BillingAddress = BillingAddress,
            Website = Website,
            LinkedIn = LinkedIn,
            Twitter = Twitter,
            BestTimeToContact = BestTimeToContact,
            TimeZone = TimeZone,
            ContactInstructions = ContactInstructions,
            IsVerified = IsVerified,
            LastVerifiedDate = LastVerifiedDate
        };

        return newContactInfo;
    }

    /// <summary>
    /// Creates a new ContactInfo with additional phone number
    /// </summary>
    /// <param name="phoneNumber">Phone number to add</param>
    /// <param name="phoneType">Type of phone (Primary, Secondary, Mobile, Work, Fax)</param>
    /// <returns>New ContactInfo with the phone number added</returns>
    public ContactInfo WithPhoneNumber(PhoneNumber phoneNumber, string phoneType = "Secondary")
    {
        var newContactInfo = new ContactInfo(PrimaryEmail, PrimaryPhone, PrimaryAddress, PreferredContactMethod, LanguagePreference);

        switch (phoneType.ToLowerInvariant())
        {
            case "primary":
                newContactInfo = new ContactInfo(PrimaryEmail, phoneNumber, PrimaryAddress, PreferredContactMethod, LanguagePreference);
                newContactInfo.SecondaryPhone = SecondaryPhone;
                break;
            case "secondary":
                newContactInfo.SecondaryPhone = phoneNumber;
                break;
            case "mobile":
                newContactInfo.MobilePhone = phoneNumber;
                break;
            case "work":
                newContactInfo.WorkPhone = phoneNumber;
                break;
            case "fax":
                newContactInfo.FaxNumber = phoneNumber;
                break;
        }

        // Copy other properties
        newContactInfo.WorkAddress = WorkAddress;
        newContactInfo.BillingAddress = BillingAddress;
        newContactInfo.Website = Website;
        newContactInfo.LinkedIn = LinkedIn;
        newContactInfo.Twitter = Twitter;
        newContactInfo.BestTimeToContact = BestTimeToContact;
        newContactInfo.TimeZone = TimeZone;
        newContactInfo.ContactInstructions = ContactInstructions;
        newContactInfo.IsVerified = IsVerified;
        newContactInfo.LastVerifiedDate = LastVerifiedDate;

        return newContactInfo;
    }

    /// <summary>
    /// Creates a new ContactInfo with additional address
    /// </summary>
    /// <param name="address">Address to add</param>
    /// <param name="addressType">Type of address (Primary, Work, Billing)</param>
    /// <returns>New ContactInfo with the address added</returns>
    public ContactInfo WithAddress(Address address, string addressType = "Work")
    {
        var newContactInfo = new ContactInfo(PrimaryEmail, PrimaryPhone, PrimaryAddress, PreferredContactMethod, LanguagePreference);

        switch (addressType.ToLowerInvariant())
        {
            case "primary":
                newContactInfo = new ContactInfo(PrimaryEmail, PrimaryPhone, address, PreferredContactMethod, LanguagePreference);
                break;
            case "work":
                newContactInfo.WorkAddress = address;
                break;
            case "billing":
                newContactInfo.BillingAddress = address;
                break;
        }

        // Copy other properties
        newContactInfo.SecondaryEmail = SecondaryEmail;
        newContactInfo.SecondaryPhone = SecondaryPhone;
        newContactInfo.MobilePhone = MobilePhone;
        newContactInfo.WorkPhone = WorkPhone;
        newContactInfo.FaxNumber = FaxNumber;
        newContactInfo.Website = Website;
        newContactInfo.LinkedIn = LinkedIn;
        newContactInfo.Twitter = Twitter;
        newContactInfo.BestTimeToContact = BestTimeToContact;
        newContactInfo.TimeZone = TimeZone;
        newContactInfo.ContactInstructions = ContactInstructions;
        newContactInfo.IsVerified = IsVerified;
        newContactInfo.LastVerifiedDate = LastVerifiedDate;

        return newContactInfo;
    }

    /// <summary>
    /// Creates a new ContactInfo marked as verified
    /// </summary>
    /// <returns>New ContactInfo marked as verified</returns>
    public ContactInfo MarkAsVerified()
    {
        var newContactInfo = new ContactInfo(PrimaryEmail, PrimaryPhone, PrimaryAddress, PreferredContactMethod, LanguagePreference)
        {
            SecondaryEmail = SecondaryEmail,
            SecondaryPhone = SecondaryPhone,
            MobilePhone = MobilePhone,
            WorkPhone = WorkPhone,
            FaxNumber = FaxNumber,
            WorkAddress = WorkAddress,
            BillingAddress = BillingAddress,
            Website = Website,
            LinkedIn = LinkedIn,
            Twitter = Twitter,
            BestTimeToContact = BestTimeToContact,
            TimeZone = TimeZone,
            ContactInstructions = ContactInstructions,
            IsVerified = true,
            LastVerifiedDate = DateTime.UtcNow
        };

        return newContactInfo;
    }

    /// <summary>
    /// Updates the preferred contact method
    /// </summary>
    /// <param name="preferredMethod">New preferred contact method</param>
    /// <returns>New ContactInfo with updated preferred method</returns>
    public ContactInfo WithPreferredContactMethod(string preferredMethod)
    {
        var newContactInfo = new ContactInfo(PrimaryEmail, PrimaryPhone, PrimaryAddress, preferredMethod, LanguagePreference)
        {
            SecondaryEmail = SecondaryEmail,
            SecondaryPhone = SecondaryPhone,
            MobilePhone = MobilePhone,
            WorkPhone = WorkPhone,
            FaxNumber = FaxNumber,
            WorkAddress = WorkAddress,
            BillingAddress = BillingAddress,
            Website = Website,
            LinkedIn = LinkedIn,
            Twitter = Twitter,
            BestTimeToContact = BestTimeToContact,
            TimeZone = TimeZone,
            ContactInstructions = ContactInstructions,
            IsVerified = IsVerified,
            LastVerifiedDate = LastVerifiedDate
        };

        return newContactInfo;
    }

    /// <summary>
    /// Gets a summary of all contact methods
    /// </summary>
    /// <returns>Summary string of contact methods</returns>
    public string GetContactSummary()
    {
        var summary = new List<string>();

        var email = GetPrimaryEmail();
        if (email != null)
            summary.Add($"Email: {email.Value}");

        var phone = GetPrimaryPhone();
        if (phone != null)
            summary.Add($"Phone: {phone.FormattedValue}");

        var address = GetPrimaryAddress();
        if (address != null)
            summary.Add($"Address: {address.GetSingleLine()}");

        if (!string.IsNullOrWhiteSpace(Website))
            summary.Add($"Website: {Website}");

        return string.Join(" | ", summary);
    }

    #region Equality and Operators

    public bool Equals(ContactInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return Equals(PrimaryEmail, other.PrimaryEmail) &&
               Equals(PrimaryPhone, other.PrimaryPhone) &&
               Equals(PrimaryAddress, other.PrimaryAddress) &&
               string.Equals(PreferredContactMethod, other.PreferredContactMethod, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return obj is ContactInfo other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(PrimaryEmail, PrimaryPhone, PrimaryAddress, PreferredContactMethod);
    }

    public static bool operator ==(ContactInfo? left, ContactInfo? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ContactInfo? left, ContactInfo? right)
    {
        return !Equals(left, right);
    }

    #endregion

    public override string ToString()
    {
        return GetContactSummary();
    }
}