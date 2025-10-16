namespace VisitorManagementSystem.Api.Domain.Constants;

/// <summary>
/// Contains all validation error messages used throughout the application
/// </summary>
public static class ValidationMessages
{
    /// <summary>
    /// General validation messages
    /// </summary>
    public static class General
    {
        public const string Required = "This field is required.";
        public const string InvalidFormat = "The format is invalid.";
        public const string TooLong = "This field is too long.";
        public const string TooShort = "This field is too short.";
        public const string InvalidRange = "Value is outside the valid range.";
        public const string InvalidDate = "Please enter a valid date.";
        public const string InvalidDateTime = "Please enter a valid date and time.";
        public const string FutureDate = "Date must be in the future.";
        public const string PastDate = "Date must be in the past.";
        public const string InvalidEmail = "Please enter a valid email address.";
        public const string InvalidPhone = "Please enter a valid phone number.";
        public const string InvalidUrl = "Please enter a valid URL.";
        public const string InvalidNumber = "Please enter a valid number.";
        public const string InvalidInteger = "Please enter a valid whole number.";
        public const string InvalidDecimal = "Please enter a valid decimal number.";
        public const string NotFound = "The requested item was not found.";
        public const string AccessDenied = "You do not have permission to perform this action.";
        public const string Duplicate = "This item already exists.";
        public const string InUse = "This item is currently in use and cannot be deleted.";
        public const string Expired = "This item has expired.";
        public const string InvalidOperation = "This operation is not valid in the current state.";
    }

    /// <summary>
    /// User validation messages
    /// </summary>
    public static class User
    {
        public const string FirstNameRequired = "First name is required.";
        public const string FirstNameTooLong = "First name cannot exceed 50 characters.";
        public const string LastNameRequired = "Last name is required.";
        public const string LastNameTooLong = "Last name cannot exceed 50 characters.";
        public const string EmailRequired = "Email address is required.";
        public const string EmailInvalid = "Please enter a valid email address.";
        public const string EmailTooLong = "Email address cannot exceed 256 characters.";
        public const string EmailAlreadyExists = "A user with this email address already exists.";
        public const string PasswordRequired = "Password is required.";
        public const string PasswordTooShort = "Password must be at least {0} characters long.";
        public const string PasswordTooLong = "Password cannot exceed {0} characters.";
        public const string PasswordComplexity = "Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character.";
        public const string PasswordHistory = "You cannot reuse any of your last {0} passwords.";
        public const string PasswordExpired = "Your password has expired and must be changed.";
        public const string InvalidCredentials = "Invalid email address or password.";
        public const string AccountLocked = "Your account has been locked due to too many failed login attempts.";
        public const string AccountInactive = "Your account is inactive. Please contact an administrator.";
        public const string AccountSuspended = "Your account has been suspended.";
        public const string AccountArchived = "Your account has been archived.";
        public const string RoleRequired = "User role is required.";
        public const string InvalidRole = "The specified role is not valid.";
        public const string CannotDeleteSelf = "You cannot delete your own account.";
        public const string CannotDeactivateSelf = "You cannot deactivate your own account.";
        public const string CannotChangeOwnRole = "You cannot change your own role.";
        public const string PhoneInvalid = "Please enter a valid phone number.";
        public const string DepartmentTooLong = "Department cannot exceed 100 characters.";
        public const string JobTitleTooLong = "Job title cannot exceed 100 characters.";
        public const string EmployeeIdTooLong = "Employee ID cannot exceed 50 characters.";
        public const string EmployeeIdAlreadyExists = "A user with this employee ID already exists.";
        public const string SecurityStampInvalid = "Your session has expired. Please log in again.";
        public const string TokenExpired = "Your session has expired. Please log in again.";
        public const string TokenInvalid = "Invalid security token.";
        public const string RefreshTokenExpired = "Your session has expired. Please log in again.";
        public const string RefreshTokenInvalid = "Invalid refresh token.";
        public const string RefreshTokenAlreadyUsed = "This refresh token has already been used.";
        public const string RefreshTokenRevoked = "This refresh token has been revoked.";
    }

    /// <summary>
    /// Invitation validation messages
    /// </summary>
    public static class Invitation
    {
        public const string VisitorNameRequired = "Visitor name is required.";
        public const string VisitorNameTooLong = "Visitor name cannot exceed 100 characters.";
        public const string VisitorEmailRequired = "Visitor email is required.";
        public const string VisitorEmailInvalid = "Please enter a valid visitor email address.";
        public const string VisitorPhoneInvalid = "Please enter a valid visitor phone number.";
        public const string PurposeRequired = "Purpose of visit is required.";
        public const string PurposeTooLong = "Purpose of visit cannot exceed 500 characters.";
        public const string VisitDateRequired = "Visit date is required.";
        public const string VisitDatePast = "Visit date cannot be in the past.";
        public const string VisitDateTooFar = "Visit date cannot be more than {0} days in the future.";
        public const string StartTimeRequired = "Start time is required.";
        public const string EndTimeRequired = "End time is required.";
        public const string EndTimeBeforeStart = "End time must be after start time.";
        public const string VisitTooLong = "Visit duration cannot exceed {0} hours.";
        public const string VisitTooShort = "Visit duration must be at least {0} minutes.";
        public const string HostRequired = "Host is required.";
        public const string InvalidHost = "The specified host is not valid.";
        public const string HostInactive = "The specified host is inactive.";
        public const string CapacityExceeded = "The capacity for the selected date and time has been exceeded.";
        public const string DuplicateInvitation = "An invitation for this visitor already exists for the selected date.";
        public const string NotAuthorized = "You are not authorized to perform this action on this invitation.";
        public const string CannotModifyApproved = "Cannot modify an approved invitation.";
        public const string CannotModifyExpired = "Cannot modify an expired invitation.";
        public const string CannotModifyCancelled = "Cannot modify a cancelled invitation.";
        public const string ApprovalRequired = "This invitation requires approval before the visitor can check in.";
        public const string AlreadyApproved = "This invitation has already been approved.";
        public const string AlreadyDenied = "This invitation has already been denied.";
        public const string AlreadyCancelled = "This invitation has already been cancelled.";
        public const string CompanyTooLong = "Company name cannot exceed 100 characters.";
        public const string SpecialInstructionsTooLong = "Special instructions cannot exceed 1000 characters.";
        public const string CustomFieldRequired = "The field '{0}' is required.";
        public const string CustomFieldInvalid = "The field '{0}' has an invalid value.";
        public const string InvalidStatus = "The invitation status is not valid.";
        public const string StatusTransitionInvalid = "Cannot change status from {0} to {1}.";
    }

    /// <summary>
    /// Visitor validation messages
    /// </summary>
    public static class Visitor
    {
        public const string NameRequired = "Visitor name is required.";
        public const string NameTooLong = "Visitor name cannot exceed 100 characters.";
        public const string EmailRequired = "Email address is required.";
        public const string EmailInvalid = "Please enter a valid email address.";
        public const string PhoneInvalid = "Please enter a valid phone number.";
        public const string CompanyTooLong = "Company name cannot exceed 100 characters.";
        public const string NotFound = "Visitor not found.";
        public const string AlreadyCheckedIn = "Visitor is already checked in.";
        public const string NotCheckedIn = "Visitor is not currently checked in.";
        public const string CheckInExpired = "Check-in session has expired.";
        public const string PhotoRequired = "Visitor photo is required.";
        public const string PhotoTooLarge = "Photo file size cannot exceed {0}MB.";
        public const string PhotoInvalidFormat = "Photo must be in JPG, PNG, or GIF format.";
        public const string InvalidDocumentType = "Invalid identification document type.";
        public const string DocumentNumberRequired = "Document number is required.";
        public const string DocumentNumberTooLong = "Document number cannot exceed 50 characters.";
        public const string AddressInvalid = "Please enter a valid address.";
        public const string EmergencyContactRequired = "Emergency contact information is required.";
        public const string CannotDeleteCheckedIn = "Cannot delete a visitor who is currently checked in.";
        public const string CannotModifyCheckedIn = "Cannot modify visitor information while checked in.";
    }

    /// <summary>
    /// Check-in validation messages
    /// </summary>
    public static class CheckIn
    {
        public const string VisitorRequired = "Visitor is required.";
        public const string VisitorNotFound = "Visitor not found.";
        public const string InvitationRequired = "Valid invitation is required.";
        public const string InvitationNotFound = "Invitation not found.";
        public const string InvitationExpired = "Invitation has expired.";
        public const string InvitationNotApproved = "Invitation has not been approved.";
        public const string AlreadyCheckedIn = "Visitor is already checked in.";
        public const string NotCheckedIn = "Visitor is not currently checked in.";
        public const string CheckInWindowClosed = "Check-in window for this invitation has closed.";
        public const string CheckInTooEarly = "Check-in is too early. Please return at the scheduled time.";
        public const string CheckInTooLate = "Check-in is too late. Please contact your host.";
        public const string OverrideReasonRequired = "Override reason is required.";
        public const string OverrideReasonTooLong = "Override reason cannot exceed 500 characters.";
        public const string OverrideNotAuthorized = "You are not authorized to override check-in rules.";
        public const string BadgePrintFailed = "Failed to print visitor badge.";
        public const string QRCodeInvalid = "Invalid QR code.";
        public const string QRCodeExpired = "QR code has expired.";
        public const string PhotoVerificationFailed = "Photo verification failed.";
        public const string DocumentVerificationFailed = "Document verification failed.";
        public const string SecurityCheckFailed = "Security check failed.";
        public const string CapacityExceeded = "Building capacity has been exceeded.";
        public const string AccessDenied = "Access denied for this visitor.";
        public const string BlacklistAlert = "Visitor is on the blacklist.";
        public const string WatchlistAlert = "Visitor requires special attention.";
        public const string FRSystemUnavailable = "Facial recognition system is currently unavailable.";
    }

    /// <summary>
    /// Walk-in validation messages
    /// </summary>
    public static class WalkIn
    {
        public const string NameRequired = "Visitor name is required.";
        public const string NameTooLong = "Visitor name cannot exceed 100 characters.";
        public const string PurposeRequired = "Purpose of visit is required.";
        public const string PurposeTooLong = "Purpose of visit cannot exceed 500 characters.";
        public const string HostRequired = "Host information is required for walk-in visitors.";
        public const string HostNotFound = "Host not found.";
        public const string HostUnavailable = "Host is currently unavailable.";
        public const string ContactInfoRequired = "Contact information is required.";
        public const string EmailOrPhoneRequired = "Either email or phone number is required.";
        public const string EmergencyContactRequired = "Emergency contact is required.";
        public const string PhotoRequired = "Photo is required for walk-in registration.";
        public const string DocumentRequired = "Identification document is required.";
        public const string SecurityClearanceRequired = "Security clearance is required for this area.";
        public const string CapacityExceeded = "Walk-in capacity has been exceeded.";
        public const string RegistrationFailed = "Walk-in registration failed.";
        public const string ConversionFailed = "Failed to convert walk-in to regular visitor.";
        public const string AlreadyConverted = "This walk-in has already been converted.";
        public const string CannotModifyAfterCheckIn = "Cannot modify walk-in after check-in.";
    }

    /// <summary>
    /// Bulk import validation messages
    /// </summary>
    public static class BulkImport
    {
        public const string FileRequired = "Import file is required.";
        public const string FileEmpty = "Import file is empty.";
        public const string FileTooLarge = "Import file size cannot exceed {0}MB.";
        public const string InvalidFileFormat = "Invalid file format. Only Excel (.xlsx) and CSV files are supported.";
        public const string InvalidTemplate = "File does not match the required template format.";
        public const string MissingColumns = "Required columns are missing: {0}";
        public const string ExtraColumns = "Unknown columns found: {0}";
        public const string InvalidData = "Invalid data in row {0}, column '{1}': {2}";
        public const string DuplicateEmail = "Duplicate email address in row {0}: {1}";
        public const string RowTooLong = "Row {0} exceeds maximum allowed columns.";
        public const string TooManyRows = "File contains too many rows. Maximum allowed: {0}";
        public const string ProcessingFailed = "Import processing failed.";
        public const string ValidationFailed = "Import validation failed. Please correct errors and try again.";
        public const string BatchNotFound = "Import batch not found.";
        public const string BatchAlreadyProcessed = "Import batch has already been processed.";
        public const string BatchCancelled = "Import batch has been cancelled.";
        public const string CannotModifyProcessed = "Cannot modify a processed import batch.";
        public const string NoValidRows = "No valid rows found in the import file.";
        public const string PartialImport = "Import completed with {0} successful and {1} failed records.";
        public const string ImportCompleted = "Import completed successfully. {0} records processed.";
    }

    /// <summary>
    /// Watchlist validation messages
    /// </summary>
    public static class Watchlist
    {
        public const string NameRequired = "Watchlist name is required.";
        public const string NameTooLong = "Watchlist name cannot exceed 100 characters.";
        public const string NameAlreadyExists = "A watchlist with this name already exists.";
        public const string TypeRequired = "Watchlist type is required.";
        public const string InvalidType = "Invalid watchlist type.";
        public const string DescriptionTooLong = "Description cannot exceed 500 characters.";
        public const string NotFound = "Watchlist not found.";
        public const string CannotDeleteInUse = "Cannot delete watchlist that has assigned visitors.";
        public const string CannotDeleteSystem = "Cannot delete system watchlists.";
        public const string AssignmentFailed = "Failed to assign visitor to watchlist.";
        public const string UnassignmentFailed = "Failed to unassign visitor from watchlist.";
        public const string AlreadyAssigned = "Visitor is already assigned to this watchlist.";
        public const string NotAssigned = "Visitor is not assigned to this watchlist.";
        public const string SyncFailed = "Failed to synchronize watchlist with facial recognition system.";
        public const string FRSystemUnavailable = "Facial recognition system is unavailable for watchlist operations.";
        public const string ConflictingTypes = "Cannot assign visitor to conflicting watchlist types.";
        public const string RequiresApproval = "Watchlist assignment requires approval.";
    }

    /// <summary>
    /// Custom field validation messages
    /// </summary>
    public static class CustomField
    {
        public const string NameRequired = "Field name is required.";
        public const string NameTooLong = "Field name cannot exceed 100 characters.";
        public const string NameAlreadyExists = "A field with this name already exists.";
        public const string LabelRequired = "Field label is required.";
        public const string LabelTooLong = "Field label cannot exceed 200 characters.";
        public const string TypeRequired = "Field type is required.";
        public const string InvalidType = "Invalid field type.";
        public const string OptionsRequired = "Options are required for selection fields.";
        public const string OptionsTooLong = "Options text is too long.";
        public const string ValidationRuleInvalid = "Invalid validation rule.";
        public const string NotFound = "Custom field not found.";
        public const string CannotDeleteInUse = "Cannot delete custom field that is in use.";
        public const string CannotModifyType = "Cannot modify field type after creation.";
        public const string DependencyLoop = "Field dependency creates a circular reference.";
        public const string InvalidDependency = "Invalid field dependency.";
        public const string RequiredFieldMissing = "Required field '{0}' is missing.";
        public const string FieldValueInvalid = "Invalid value for field '{0}'.";
        public const string FieldValueTooLong = "Value for field '{0}' is too long.";
        public const string SelectionRequired = "Please select a value for '{0}'.";
        public const string DateRangeInvalid = "Invalid date range for field '{0}'.";
        public const string NumberOutOfRange = "Number is out of valid range for field '{0}'.";
        public const string FileTypeNotAllowed = "File type not allowed for field '{0}'.";
        public const string FileSizeTooLarge = "File size too large for field '{0}'.";
    }

    /// <summary>
    /// Facial recognition validation messages
    /// </summary>
    public static class FacialRecognition
    {
        public const string SystemUnavailable = "Facial recognition system is currently unavailable.";
        public const string ProfileNotFound = "Facial recognition profile not found.";
        public const string ProfileCreationFailed = "Failed to create facial recognition profile.";
        public const string ProfileUpdateFailed = "Failed to update facial recognition profile.";
        public const string ProfileDeletionFailed = "Failed to delete facial recognition profile.";
        public const string SyncFailed = "Synchronization with facial recognition system failed.";
        public const string ConnectionFailed = "Cannot connect to facial recognition system.";
        public const string AuthenticationFailed = "Authentication with facial recognition system failed.";
        public const string ApiKeyInvalid = "Invalid API key for facial recognition system.";
        public const string RateLimitExceeded = "Rate limit exceeded for facial recognition operations.";
        public const string PhotoRequired = "Photo is required for facial recognition profile.";
        public const string PhotoQualityPoor = "Photo quality is too poor for facial recognition.";
        public const string FaceNotDetected = "No face detected in the provided photo.";
        public const string MultipleFacesDetected = "Multiple faces detected. Please provide a photo with a single face.";
        public const string BiometricDataInvalid = "Invalid biometric data.";
        public const string EnrollmentFailed = "Facial recognition enrollment failed.";
        public const string VerificationFailed = "Facial recognition verification failed.";
        public const string IdentificationFailed = "Facial recognition identification failed.";
        public const string ConfidenceScoreLow = "Facial recognition confidence score is too low.";
        public const string SystemError = "Facial recognition system error: {0}";
        public const string ConfigurationInvalid = "Invalid facial recognition system configuration.";
        public const string LicenseExpired = "Facial recognition system license has expired.";
        public const string MaintenanceMode = "Facial recognition system is in maintenance mode.";
    }

    /// <summary>
    /// System validation messages
    /// </summary>
    public static class System
    {
        public const string ConfigurationRequired = "System configuration is required.";
        public const string ConfigurationInvalid = "System configuration is invalid.";
        public const string DatabaseConnectionFailed = "Database connection failed.";
        public const string ServiceUnavailable = "Service is currently unavailable.";
        public const string MaintenanceMode = "System is in maintenance mode.";
        public const string LicenseExpired = "System license has expired.";
        public const string CapacityExceeded = "System capacity has been exceeded.";
        public const string InvalidConfiguration = "Invalid system configuration: {0}";
        public const string BackupFailed = "System backup failed.";
        public const string RestoreFailed = "System restore failed.";
        public const string UpdateRequired = "System update is required.";
        public const string IntegrationFailed = "External system integration failed.";
        public const string SecurityCheckFailed = "Security check failed.";
        public const string AuditLogFull = "Audit log storage is full.";
        public const string DataCorrupted = "Data corruption detected.";
        public const string StorageSpaceLow = "Storage space is running low.";
        public const string PerformanceDegraded = "System performance is degraded.";
        public const string HealthCheckFailed = "Health check failed for component: {0}";
    }

    /// <summary>
    /// Security validation messages
    /// </summary>
    public static class Security
    {
        public const string UnauthorizedAccess = "Unauthorized access attempt.";
        public const string InsufficientPermissions = "You do not have sufficient permissions to perform this action.";
        public const string SecurityViolation = "Security violation detected.";
        public const string SuspiciousActivity = "Suspicious activity detected.";
        public const string LoginAttemptsExceeded = "Too many failed login attempts. Account locked.";
        public const string SessionExpired = "Your session has expired. Please log in again.";
        public const string TokenInvalid = "Invalid security token.";
        public const string EncryptionFailed = "Data encryption failed.";
        public const string DecryptionFailed = "Data decryption failed.";
        public const string KeyRotationRequired = "Security key rotation is required.";
        public const string CertificateExpired = "Security certificate has expired.";
        public const string SignatureInvalid = "Invalid digital signature.";
        public const string HashMismatch = "Data integrity check failed.";
        public const string AuditTrailCompromised = "Audit trail integrity compromised.";
        public const string SecurityPolicyViolation = "Security policy violation: {0}";
        public const string TwoFactorRequired = "Two-factor authentication is required.";
        public const string BiometricVerificationFailed = "Biometric verification failed.";
        public const string GeofenceViolation = "Access denied: outside authorized location.";
        public const string TimeRestrictionViolation = "Access denied: outside authorized time window.";
        public const string DeviceNotAuthorized = "Device is not authorized for access.";
        public const string ComplianceViolation = "Compliance violation detected: {0}";
    }

    /// <summary>
    /// Gets a formatted validation message with parameters
    /// </summary>
    /// <param name="message">Message template</param>
    /// <param name="parameters">Parameters to format</param>
    /// <returns>Formatted message</returns>
    public static string Format(string message, params object[] parameters)
    {
        return string.Format(message, parameters);
    }

    /// <summary>
    /// Gets a validation message for a required field
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <returns>Required field message</returns>
    public static string RequiredField(string fieldName)
    {
        return $"{fieldName} is required.";
    }

    /// <summary>
    /// Gets a validation message for field length
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="maxLength">Maximum length</param>
    /// <returns>Field length message</returns>
    public static string FieldTooLong(string fieldName, int maxLength)
    {
        return $"{fieldName} cannot exceed {maxLength} characters.";
    }

    /// <summary>
    /// Gets a validation message for field range
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="min">Minimum value</param>
    /// <param name="max">Maximum value</param>
    /// <returns>Field range message</returns>
    public static string FieldOutOfRange(string fieldName, object min, object max)
    {
        return $"{fieldName} must be between {min} and {max}.";
    }

    /// <summary>
    /// Gets a validation message for invalid format
    /// </summary>
    /// <param name="fieldName">Field name</param>
    /// <param name="expectedFormat">Expected format</param>
    /// <returns>Invalid format message</returns>
    public static string InvalidFormat(string fieldName, string expectedFormat)
    {
        return $"{fieldName} must be in the format: {expectedFormat}.";
    }

    /// <summary>
    /// Gets a validation message for already exists
    /// </summary>
    /// <param name="itemType">Type of item</param>
    /// <param name="value">Value that already exists</param>
    /// <returns>Already exists message</returns>
    public static string AlreadyExists(string itemType, string value)
    {
        return $"A {itemType} with the value '{value}' already exists.";
    }

    /// <summary>
    /// Gets a validation message for not found
    /// </summary>
    /// <param name="itemType">Type of item</param>
    /// <param name="identifier">Item identifier</param>
    /// <returns>Not found message</returns>
    public static string NotFound(string itemType, string identifier)
    {
        return $"{itemType} with identifier '{identifier}' was not found.";
    }

    /// <summary>
    /// Gets a validation message for invalid state transition
    /// </summary>
    /// <param name="fromState">Current state</param>
    /// <param name="toState">Target state</param>
    /// <returns>Invalid transition message</returns>
    public static string InvalidStateTransition(string fromState, string toState)
    {
        return $"Cannot change from {fromState} to {toState}.";
    }
}