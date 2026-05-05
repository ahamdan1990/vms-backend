using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagementSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertEscalations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AlertType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AlertPriority = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TargetRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    EscalationDelayMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EscalationTargetRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EscalationTargetUserId = table.Column<int>(type: "int", nullable: true),
                    EscalationEmails = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EscalationPhones = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MaxAttempts = table.Column<int>(type: "int", nullable: false, defaultValue: 3),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RulePriority = table.Column<int>(type: "int", nullable: false, defaultValue: 10),
                    Configuration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertEscalations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlertRecipientConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlertType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TargetRole = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TargetUserId = table.Column<int>(type: "int", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertRecipientConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    RequestPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ResponseStatusCode = table.Column<int>(type: "int", nullable: true),
                    Duration = table.Column<long>(type: "bigint", nullable: true),
                    RequestSize = table.Column<long>(type: "bigint", nullable: true),
                    ResponseSize = table.Column<long>(type: "bigint", nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExceptionDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    RiskLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Low"),
                    RequiresAttention = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsReviewed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ReviewedBy = table.Column<int>(type: "int", nullable: true),
                    ReviewedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewComments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BackupRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TriggerType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TriggeredByUserId = table.Column<int>(type: "int", nullable: true),
                    TriggeredByDisplay = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DatabaseName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DataFileSizeMbAtBackup = table.Column<double>(type: "float", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BlacklistOverrideRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VisitorId = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlacklistOverrideRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cameras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CameraType = table.Column<int>(type: "int", nullable: false),
                    ConnectionString = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Password = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    ConfigurationJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastHealthCheck = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastOnlineTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FailureCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CameraRole = table.Column<int>(type: "int", nullable: false),
                    FrameSamplingIntervalSeconds = table.Column<int>(type: "int", nullable: false),
                    EnableFacialRecognition = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                    Manufacturer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FirmwareVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cameras", x => x.Id);
                    table.CheckConstraint("CK_Cameras_FailureCount", "[FailureCount] >= 0");
                    table.CheckConstraint("CK_Cameras_Priority", "[Priority] >= 1 AND [Priority] <= 10");
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Website = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Industry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TaxId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ContactPersonName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ContactPhoneRaw = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ContactPhoneFormatted = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    ContactPhoneDigitsOnly = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    ContactPhoneCountryCode = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    ContactPhoneAreaCode = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    ContactPhoneType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ContactPhoneIsVerified = table.Column<bool>(type: "bit", nullable: true),
                    CompanyAddressStreet1 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CompanyAddressStreet2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CompanyAddressCity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CompanyAddressState = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CompanyAddressPostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CompanyAddressCountry = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CompanyAddressLatitude = table.Column<double>(type: "float", nullable: true),
                    CompanyAddressLongitude = table.Column<double>(type: "float", nullable: true),
                    CompanyAddressType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CompanyAddressIsValidated = table.Column<bool>(type: "bit", nullable: true),
                    EmployeeCount = table.Column<int>(type: "int", nullable: true),
                    LogoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    VerifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedBy = table.Column<int>(type: "int", nullable: true),
                    BlacklistReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BlacklistedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BlacklistedBy = table.Column<int>(type: "int", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    VisitorCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConfigurationAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SystemConfigurationId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsAutomated = table.Column<bool>(type: "bit", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedBy = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ManagerId = table.Column<int>(type: "int", nullable: true),
                    ParentDepartmentId = table.Column<int>(type: "int", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Budget = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departments_ParentDepartment",
                        column: x => x.ParentDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmergencyContacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VisitorId = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Relationship = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PhoneNumberFormatted = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    PhoneNumberDigitsOnly = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    PhoneCountryCode = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    PhoneAreaCode = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    PhoneType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PhoneIsVerified = table.Column<bool>(type: "bit", nullable: false),
                    AlternatePhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AlternatePhoneNumberFormatted = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    AlternatePhoneNumberDigitsOnly = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    AlternatePhoneCountryCode = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    AlternatePhoneAreaCode = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    AlternatePhoneType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    AlternatePhoneIsVerified = table.Column<bool>(type: "bit", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AddressStreet1 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AddressStreet2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AddressCity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AddressState = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AddressPostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AddressCountry = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AddressLatitude = table.Column<double>(type: "float(18)", precision: 18, scale: 6, nullable: true),
                    AddressLongitude = table.Column<double>(type: "float(18)", precision: 18, scale: 6, nullable: true),
                    AddressType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AddressIsValidated = table.Column<bool>(type: "bit", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmergencyContacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvitationApprovals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvitationId = table.Column<int>(type: "int", nullable: false),
                    ApproverId = table.Column<int>(type: "int", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    Decision = table.Column<int>(type: "int", nullable: false),
                    DecisionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    EscalatedToUserId = table.Column<int>(type: "int", nullable: true),
                    EscalatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvitationApprovals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvitationEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvitationId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TriggeredBy = table.Column<int>(type: "int", nullable: true),
                    EventData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvitationEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Invitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvitationNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    VisitorId = table.Column<int>(type: "int", nullable: false),
                    HostId = table.Column<int>(type: "int", nullable: false),
                    VisitPurposeId = table.Column<int>(type: "int", nullable: true),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ScheduledStartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScheduledEndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpectedVisitorCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    SpecialInstructions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RequiresEscort = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RequiresBadge = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    NeedsParking = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ParkingInstructions = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    QrCode = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SentOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<int>(type: "int", nullable: true),
                    ApprovalComments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RejectedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedBy = table.Column<int>(type: "int", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CheckedInAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckedOutAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExternalId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ImportBatchId = table.Column<int>(type: "int", nullable: true),
                    TimeSlotId = table.Column<int>(type: "int", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invitations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvitationTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SubjectTemplate = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MessageTemplate = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DefaultVisitPurposeId = table.Column<int>(type: "int", nullable: true),
                    DefaultLocationId = table.Column<int>(type: "int", nullable: true),
                    DefaultDurationHours = table.Column<double>(type: "float", nullable: false, defaultValue: 2.0),
                    DefaultRequiresApproval = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DefaultRequiresEscort = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DefaultRequiresBadge = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DefaultSpecialInstructions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsShared = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsSystemTemplate = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    UsageCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastUsedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvitationTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Building = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Floor = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Room = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LocationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Zone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MaxOccupancy = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    MaxCapacity = table.Column<int>(type: "int", nullable: false),
                    RequiresEscort = table.Column<bool>(type: "bit", nullable: false),
                    RequiresSecurityClearance = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SecurityClearanceLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AccessLevel = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsAccessible = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    AccessInstructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ParentLocationId = table.Column<int>(type: "int", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Locations_Locations_ParentLocationId",
                        column: x => x.ParentLocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationAlerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TargetRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TargetUserId = table.Column<int>(type: "int", nullable: true),
                    TargetLocationId = table.Column<int>(type: "int", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RelatedEntityId = table.Column<int>(type: "int", nullable: true),
                    PayloadData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAcknowledged = table.Column<bool>(type: "bit", nullable: false),
                    AcknowledgedBy = table.Column<int>(type: "int", nullable: true),
                    AcknowledgedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentExternally = table.Column<bool>(type: "bit", nullable: false),
                    SentExternallyOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationAlerts_Locations_TargetLocationId",
                        column: x => x.TargetLocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OccupancyLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TimeSlotId = table.Column<int>(type: "int", nullable: true),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    CurrentCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MaxCapacity = table.Column<int>(type: "int", nullable: false),
                    ReservedCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AvailableCapacity = table.Column<int>(type: "int", nullable: false, computedColumnSql: "[MaxCapacity] - [CurrentCount] - [ReservedCount]", stored: false),
                    OccupancyPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false, computedColumnSql: "CASE WHEN [MaxCapacity] > 0 THEN CAST(([CurrentCount] + [ReservedCount]) * 100.0 / [MaxCapacity] AS DECIMAL(5,2)) ELSE 0 END", stored: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OccupancyLogs", x => x.Id);
                    table.CheckConstraint("CK_OccupancyLogs_CurrentCount", "[CurrentCount] >= 0");
                    table.CheckConstraint("CK_OccupancyLogs_MaxCapacity", "[MaxCapacity] > 0");
                    table.CheckConstraint("CK_OccupancyLogs_ReservedCount", "[ReservedCount] >= 0");
                    table.ForeignKey(
                        name: "FK_OccupancyLogs_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OperatorSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ConnectionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LastActivity = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SessionStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SessionEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatorSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperatorSessions_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PermissionChangeAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ChangedBy = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PreviousValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionChangeAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RiskLevel = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsSystemPermission = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    JwtId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RevokedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevocationReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    RevokedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeviceFingerprint = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ReplacedByTokenId = table.Column<int>(type: "int", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_RefreshTokens_ReplacedByTokenId",
                        column: x => x.ReplacedByTokenId,
                        principalTable: "RefreshTokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    GrantedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HierarchyLevel = table.Column<int>(type: "int", nullable: false),
                    IsSystemRole = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PasswordSalt = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PhoneRaw = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PhoneFormatted = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    PhoneDigitsOnly = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    PhoneCountryCode = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    PhoneAreaCode = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    PhoneType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PhoneIsVerified = table.Column<bool>(type: "bit", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    JobTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EmployeeId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RequiresApprovalOverride = table.Column<bool>(type: "bit", nullable: true),
                    ProfilePhotoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastLoginDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedLoginAttempts = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LockoutEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsLockedOut = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PasswordChangedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TimeZone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "UTC"),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "en-US"),
                    Theme = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "light"),
                    EmailVerificationToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EmailVerificationTokenExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsEmailVerified = table.Column<bool>(type: "bit", nullable: false),
                    EmailVerifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsLdapUser = table.Column<bool>(type: "bit", nullable: false),
                    LdapDistinguishedName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastLdapSyncOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AddressStreet1 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AddressStreet2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AddressCity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AddressState = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AddressPostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AddressCountry = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AddressLatitude = table.Column<double>(type: "float", nullable: true),
                    AddressLongitude = table.Column<double>(type: "float", nullable: true),
                    AddressType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AddressIsValidated = table.Column<bool>(type: "bit", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_User_CreatedBy_User",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_User_ModifiedBy_User",
                        column: x => x.ModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Department",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffAttendances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CheckInTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckOutTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Method = table.Column<int>(type: "int", nullable: false),
                    CameraId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffAttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffAttendances_Cameras_CameraId",
                        column: x => x.CameraId,
                        principalTable: "Cameras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StaffAttendances_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StaffAttendances_Users_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StaffAttendances_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SystemConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequiresRestart = table.Column<bool>(type: "bit", nullable: false),
                    IsEncrypted = table.Column<bool>(type: "bit", nullable: false),
                    IsReadOnly = table.Column<bool>(type: "bit", nullable: false),
                    IsSensitive = table.Column<bool>(type: "bit", nullable: false),
                    DefaultValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidationRules = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MinValue = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    MaxValue = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AllowedValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Group = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "All"),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemConfigurations_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SystemConfigurations_Users_ModifiedBy",
                        column: x => x.ModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TimeSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    MaxVisitors = table.Column<int>(type: "int", nullable: false, defaultValue: 50),
                    ActiveDays = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "1,2,3,4,5"),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    BufferMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 15),
                    AllowOverlapping = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeSlots", x => x.Id);
                    table.CheckConstraint("CK_TimeSlots_BufferMinutes", "[BufferMinutes] >= 0");
                    table.CheckConstraint("CK_TimeSlots_MaxVisitors", "[MaxVisitors] > 0");
                    table.ForeignKey(
                        name: "FK_TimeSlot_CreatedBy_User",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeSlot_DeletedBy_User",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeSlot_ModifiedBy_User",
                        column: x => x.ModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeSlots_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "VisitPurposes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ColorCode = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    IconName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RequiresSecurityClearance = table.Column<bool>(type: "bit", nullable: false),
                    MaxDurationHours = table.Column<int>(type: "int", nullable: false, defaultValue: 8),
                    RequiresBackgroundCheck = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    Requirements = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitPurposes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisitPurpose_CreatedBy_User",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VisitPurpose_ModifiedBy_User",
                        column: x => x.ModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VisitPurposes_Users_DeletedByUserId",
                        column: x => x.DeletedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TimeSlotBookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TimeSlotId = table.Column<int>(type: "int", nullable: false),
                    BookingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InvitationId = table.Column<int>(type: "int", nullable: true),
                    VisitorCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BookedBy = table.Column<int>(type: "int", nullable: false),
                    BookedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CancelledOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledBy = table.Column<int>(type: "int", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeSlotBookings", x => x.Id);
                    table.CheckConstraint("CK_TimeSlotBookings_VisitorCount", "[VisitorCount] > 0");
                    table.ForeignKey(
                        name: "FK_TimeSlotBooking_CreatedBy_User",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeSlotBooking_DeletedBy_User",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeSlotBooking_ModifiedBy_User",
                        column: x => x.ModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeSlotBookings_Invitations_InvitationId",
                        column: x => x.InvitationId,
                        principalTable: "Invitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TimeSlotBookings_TimeSlots_TimeSlotId",
                        column: x => x.TimeSlotId,
                        principalTable: "TimeSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeSlotBookings_Users_BookedBy",
                        column: x => x.BookedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeSlotBookings_Users_CancelledBy",
                        column: x => x.CancelledBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Visitors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PhoneNumberFormatted = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    PhoneNumberDigitsOnly = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    PhoneCountryCode = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    PhoneAreaCode = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    PhoneType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PhoneIsVerified = table.Column<bool>(type: "bit", nullable: true),
                    Company = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CompanyId = table.Column<int>(type: "int", nullable: true),
                    JobTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AddressStreet1 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AddressStreet2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AddressCity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AddressState = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AddressPostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AddressCountry = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AddressLatitude = table.Column<double>(type: "float(18)", precision: 18, scale: 6, nullable: true),
                    AddressLongitude = table.Column<double>(type: "float(18)", precision: 18, scale: 6, nullable: true),
                    AddressType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AddressIsValidated = table.Column<bool>(type: "bit", nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GovernmentId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GovernmentIdType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Nationality = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "en-US"),
                    IsCivilian = table.Column<bool>(type: "bit", nullable: false),
                    CivilianOrigin = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProfilePhotoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DietaryRequirements = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AccessibilityRequirements = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SecurityClearance = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsVip = table.Column<bool>(type: "bit", nullable: false),
                    IsBlacklisted = table.Column<bool>(type: "bit", nullable: false),
                    BlacklistReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BlacklistedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BlacklistedBy = table.Column<int>(type: "int", nullable: true),
                    VisitCount = table.Column<int>(type: "int", nullable: false),
                    LastVisitDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExternalId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PreferredLocationId = table.Column<int>(type: "int", nullable: true),
                    DefaultVisitPurposeId = table.Column<int>(type: "int", nullable: true),
                    TimeZone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visitors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Visitor_CreatedBy_User",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Visitor_ModifiedBy_User",
                        column: x => x.ModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Visitors_Company",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Visitors_Locations_PreferredLocationId",
                        column: x => x.PreferredLocationId,
                        principalTable: "Locations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Visitors_Users_BlacklistedBy",
                        column: x => x.BlacklistedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Visitors_Users_DeletedByUserId",
                        column: x => x.DeletedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Visitors_VisitPurposes_DefaultVisitPurposeId",
                        column: x => x.DefaultVisitPurposeId,
                        principalTable: "VisitPurposes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VisitorAccess",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    VisitorId = table.Column<int>(type: "int", nullable: false),
                    AccessType = table.Column<int>(type: "int", nullable: false),
                    GrantedBy = table.Column<int>(type: "int", nullable: true),
                    GrantedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitorAccess", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisitorAccess_Users_GrantedBy",
                        column: x => x.GrantedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VisitorAccess_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VisitorAccess_Visitors_VisitorId",
                        column: x => x.VisitorId,
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisitorDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VisitorId = table.Column<int>(type: "int", nullable: false),
                    DocumentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FileExtension = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FileHash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsEncrypted = table.Column<bool>(type: "bit", nullable: false),
                    IsSensitive = table.Column<bool>(type: "bit", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AccessLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Standard"),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitorDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisitorDocument_CreatedBy_User",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VisitorDocument_ModifiedBy_User",
                        column: x => x.ModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VisitorDocuments_Users_DeletedByUserId",
                        column: x => x.DeletedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VisitorDocuments_Visitors_VisitorId",
                        column: x => x.VisitorId,
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisitorNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VisitorId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "General"),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Medium"),
                    IsFlagged = table.Column<bool>(type: "bit", nullable: false),
                    IsConfidential = table.Column<bool>(type: "bit", nullable: false),
                    FollowUpDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitorNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisitorNote_CreatedBy_User",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VisitorNote_ModifiedBy_User",
                        column: x => x.ModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VisitorNotes_Users_DeletedByUserId",
                        column: x => x.DeletedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VisitorNotes_Visitors_VisitorId",
                        column: x => x.VisitorId,
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertEscalations_AlertPriority",
                table: "AlertEscalations",
                column: "AlertPriority");

            migrationBuilder.CreateIndex(
                name: "IX_AlertEscalations_AlertType",
                table: "AlertEscalations",
                column: "AlertType");

            migrationBuilder.CreateIndex(
                name: "IX_AlertEscalations_CreatedBy",
                table: "AlertEscalations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AlertEscalations_EscalationTargetUserId",
                table: "AlertEscalations",
                column: "EscalationTargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertEscalations_IsEnabled",
                table: "AlertEscalations",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_AlertEscalations_LocationId",
                table: "AlertEscalations",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertEscalations_ModifiedBy",
                table: "AlertEscalations",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AlertEscalations_RulePriority",
                table: "AlertEscalations",
                column: "RulePriority");

            migrationBuilder.CreateIndex(
                name: "IX_AlertEscalations_TargetRole",
                table: "AlertEscalations",
                column: "TargetRole");

            migrationBuilder.CreateIndex(
                name: "IX_AlertEscalations_Type_Priority_Enabled",
                table: "AlertEscalations",
                columns: new[] { "AlertType", "AlertPriority", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_AlertRecipientConfigurations_AlertType",
                table: "AlertRecipientConfigurations",
                column: "AlertType");

            migrationBuilder.CreateIndex(
                name: "IX_AlertRecipientConfigurations_AlertType_IsEnabled",
                table: "AlertRecipientConfigurations",
                columns: new[] { "AlertType", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_AlertRecipientConfigurations_CreatedBy",
                table: "AlertRecipientConfigurations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AlertRecipientConfigurations_ModifiedBy",
                table: "AlertRecipientConfigurations",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AlertRecipientConfigurations_TargetUserId",
                table: "AlertRecipientConfigurations",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_CreatedOn",
                table: "AuditLogs",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_IsActive",
                table: "AuditLogs",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_IsActive_CreatedOn",
                table: "AuditLogs",
                columns: new[] { "IsActive", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Attention_Review_CreatedOn",
                table: "AuditLogs",
                columns: new[] { "RequiresAttention", "IsReviewed", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CorrelationId",
                table: "AuditLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Entity_CreatedOn",
                table: "AuditLogs",
                columns: new[] { "EntityName", "EntityId", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName",
                table: "AuditLogs",
                column: "EntityName");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EventType",
                table: "AuditLogs",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EventType_CreatedOn",
                table: "AuditLogs",
                columns: new[] { "EventType", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_IpAddress",
                table: "AuditLogs",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_IsReviewed",
                table: "AuditLogs",
                column: "IsReviewed");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_IsSuccess",
                table: "AuditLogs",
                column: "IsSuccess");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_RequiresAttention",
                table: "AuditLogs",
                column: "RequiresAttention");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_RiskLevel",
                table: "AuditLogs",
                column: "RiskLevel");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Success_Risk_CreatedOn",
                table: "AuditLogs",
                columns: new[] { "IsSuccess", "RiskLevel", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId_CreatedOn",
                table: "AuditLogs",
                columns: new[] { "UserId", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_BackupRecords_CreatedBy",
                table: "BackupRecords",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_BackupRecords_ModifiedBy",
                table: "BackupRecords",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_BackupRecords_StartedAt",
                table: "BackupRecords",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BackupRecords_Status",
                table: "BackupRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BackupRecords_TriggeredByUserId",
                table: "BackupRecords",
                column: "TriggeredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BackupRecords_TriggerType",
                table: "BackupRecords",
                column: "TriggerType");

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistOverrideRequests_CreatedOn",
                table: "BlacklistOverrideRequests",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistOverrideRequests_RequestedByUserId",
                table: "BlacklistOverrideRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistOverrideRequests_ReviewedByUserId",
                table: "BlacklistOverrideRequests",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistOverrideRequests_Status",
                table: "BlacklistOverrideRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistOverrideRequests_Token",
                table: "BlacklistOverrideRequests",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistOverrideRequests_VisitorId",
                table: "BlacklistOverrideRequests",
                column: "VisitorId");

            migrationBuilder.CreateIndex(
                name: "IX_Camera_CreatedBy",
                table: "Cameras",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Camera_CreatedOn",
                table: "Cameras",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Camera_DeletedBy",
                table: "Cameras",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Camera_IsActive_CreatedOn",
                table: "Cameras",
                columns: new[] { "IsActive", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Camera_IsDeleted_DeletedOn",
                table: "Cameras",
                columns: new[] { "IsDeleted", "DeletedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Camera_ModifiedBy",
                table: "Cameras",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_CameraType",
                table: "Cameras",
                column: "CameraType");

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_CameraType_IsActive_IsDeleted",
                table: "Cameras",
                columns: new[] { "CameraType", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_EnableFacialRecognition",
                table: "Cameras",
                column: "EnableFacialRecognition");

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_EnableFacialRecognition_IsActive_IsDeleted",
                table: "Cameras",
                columns: new[] { "EnableFacialRecognition", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_FailureCount",
                table: "Cameras",
                column: "FailureCount");

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_HealthMonitoring",
                table: "Cameras",
                columns: new[] { "IsActive", "IsDeleted", "LastHealthCheck", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_IsActive",
                table: "Cameras",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_IsActive_IsDeleted_Status",
                table: "Cameras",
                columns: new[] { "IsActive", "IsDeleted", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_IsDeleted",
                table: "Cameras",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_LastHealthCheck",
                table: "Cameras",
                column: "LastHealthCheck");

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_LastOnlineTime",
                table: "Cameras",
                column: "LastOnlineTime");

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_LocationId",
                table: "Cameras",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_LocationId_Name_Unique",
                table: "Cameras",
                columns: new[] { "LocationId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_Name",
                table: "Cameras",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_Operational",
                table: "Cameras",
                columns: new[] { "IsActive", "IsDeleted", "Status", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_Priority",
                table: "Cameras",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_Status",
                table: "Cameras",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_BlacklistedBy",
                table: "Companies",
                column: "BlacklistedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Code_Unique",
                table: "Companies",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_DisplayOrder",
                table: "Companies",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_IsDeleted",
                table: "Companies",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_IsVerified",
                table: "Companies",
                column: "IsVerified");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_IsVerified_IsDeleted",
                table: "Companies",
                columns: new[] { "IsVerified", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Name",
                table: "Companies",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_VerifiedBy",
                table: "Companies",
                column: "VerifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Company_CreatedBy",
                table: "Companies",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Company_CreatedOn",
                table: "Companies",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Company_DeletedBy",
                table: "Companies",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Company_IsActive",
                table: "Companies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Company_IsActive_CreatedOn",
                table: "Companies",
                columns: new[] { "IsActive", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Company_IsDeleted_DeletedOn",
                table: "Companies",
                columns: new[] { "IsDeleted", "DeletedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Company_ModifiedBy",
                table: "Companies",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationAudits_Action",
                table: "ConfigurationAudits",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationAudits_ApprovedBy",
                table: "ConfigurationAudits",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationAudits_Category_Key",
                table: "ConfigurationAudits",
                columns: new[] { "Category", "Key" });

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationAudits_CreatedBy",
                table: "ConfigurationAudits",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationAudits_CreatedOn",
                table: "ConfigurationAudits",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationAudits_IsApproved",
                table: "ConfigurationAudits",
                column: "IsApproved");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationAudits_IsAutomated",
                table: "ConfigurationAudits",
                column: "IsAutomated");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationAudits_ModifiedBy",
                table: "ConfigurationAudits",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationAudits_RequiresApproval",
                table: "ConfigurationAudits",
                column: "RequiresApproval");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationAudits_SystemConfigurationId",
                table: "ConfigurationAudits",
                column: "SystemConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_Department_CreatedBy",
                table: "Departments",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Department_CreatedOn",
                table: "Departments",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Department_DeletedBy",
                table: "Departments",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Department_IsActive",
                table: "Departments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Department_IsActive_CreatedOn",
                table: "Departments",
                columns: new[] { "IsActive", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Department_IsDeleted_DeletedOn",
                table: "Departments",
                columns: new[] { "IsDeleted", "DeletedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Department_ModifiedBy",
                table: "Departments",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Code_Unique",
                table: "Departments",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_DisplayOrder",
                table: "Departments",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_IsDeleted",
                table: "Departments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ManagerId",
                table: "Departments",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Name",
                table: "Departments",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ParentDepartmentId",
                table: "Departments",
                column: "ParentDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ParentDepartmentId_IsDeleted",
                table: "Departments",
                columns: new[] { "ParentDepartmentId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyContact_CreatedBy",
                table: "EmergencyContacts",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyContact_CreatedOn",
                table: "EmergencyContacts",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyContact_IsActive",
                table: "EmergencyContacts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyContact_IsActive_CreatedOn",
                table: "EmergencyContacts",
                columns: new[] { "IsActive", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyContact_ModifiedBy",
                table: "EmergencyContacts",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyContacts_DeletedByUserId",
                table: "EmergencyContacts",
                column: "DeletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyContacts_IsPrimary",
                table: "EmergencyContacts",
                column: "IsPrimary");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyContacts_Priority",
                table: "EmergencyContacts",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyContacts_VisitorId",
                table: "EmergencyContacts",
                column: "VisitorId");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationApprovals_ApproverId",
                table: "InvitationApprovals",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationApprovals_CreatedByUserId",
                table: "InvitationApprovals",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationApprovals_Decision",
                table: "InvitationApprovals",
                column: "Decision");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationApprovals_EscalatedToUserId",
                table: "InvitationApprovals",
                column: "EscalatedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationApprovals_InvitationId_StepOrder",
                table: "InvitationApprovals",
                columns: new[] { "InvitationId", "StepOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvitationApprovals_ModifiedByUserId",
                table: "InvitationApprovals",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationEvents_CreatedByUserId",
                table: "InvitationEvents",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationEvents_EventTimestamp",
                table: "InvitationEvents",
                column: "EventTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationEvents_EventType",
                table: "InvitationEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationEvents_InvitationId",
                table: "InvitationEvents",
                column: "InvitationId");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationEvents_InvitationId_EventTimestamp",
                table: "InvitationEvents",
                columns: new[] { "InvitationId", "EventTimestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_InvitationEvents_ModifiedByUserId",
                table: "InvitationEvents",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationEvents_TriggeredBy",
                table: "InvitationEvents",
                column: "TriggeredBy");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_ApprovedBy",
                table: "Invitations",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_CreatedByUserId",
                table: "Invitations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_CreatedOn",
                table: "Invitations",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_DeletedByUserId",
                table: "Invitations",
                column: "DeletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_HostId",
                table: "Invitations",
                column: "HostId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_InvitationNumber",
                table: "Invitations",
                column: "InvitationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_LocationId",
                table: "Invitations",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_ModifiedByUserId",
                table: "Invitations",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_RejectedBy",
                table: "Invitations",
                column: "RejectedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_ScheduledStartTime",
                table: "Invitations",
                column: "ScheduledStartTime");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_Status",
                table: "Invitations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_Status_ScheduledStartTime",
                table: "Invitations",
                columns: new[] { "Status", "ScheduledStartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_TimeSlotId",
                table: "Invitations",
                column: "TimeSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_VisitorId",
                table: "Invitations",
                column: "VisitorId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_VisitPurposeId",
                table: "Invitations",
                column: "VisitPurposeId");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationTemplates_Category",
                table: "InvitationTemplates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationTemplates_Category_IsShared",
                table: "InvitationTemplates",
                columns: new[] { "Category", "IsShared" });

            migrationBuilder.CreateIndex(
                name: "IX_InvitationTemplates_CreatedByUserId",
                table: "InvitationTemplates",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationTemplates_DefaultLocationId",
                table: "InvitationTemplates",
                column: "DefaultLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationTemplates_DefaultVisitPurposeId",
                table: "InvitationTemplates",
                column: "DefaultVisitPurposeId");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationTemplates_DeletedByUserId",
                table: "InvitationTemplates",
                column: "DeletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationTemplates_IsShared",
                table: "InvitationTemplates",
                column: "IsShared");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationTemplates_IsSystemTemplate",
                table: "InvitationTemplates",
                column: "IsSystemTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationTemplates_ModifiedByUserId",
                table: "InvitationTemplates",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationTemplates_Name",
                table: "InvitationTemplates",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_InvitationTemplates_UsageCount",
                table: "InvitationTemplates",
                column: "UsageCount");

            migrationBuilder.CreateIndex(
                name: "IX_Location_CreatedBy",
                table: "Locations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Location_CreatedOn",
                table: "Locations",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Location_IsActive",
                table: "Locations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Location_IsActive_CreatedOn",
                table: "Locations",
                columns: new[] { "IsActive", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Location_ModifiedBy",
                table: "Locations",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_Code",
                table: "Locations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Locations_DeletedByUserId",
                table: "Locations",
                column: "DeletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_DisplayOrder",
                table: "Locations",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_LocationType",
                table: "Locations",
                column: "LocationType");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_Name",
                table: "Locations",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_ParentLocationId",
                table: "Locations",
                column: "ParentLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_SecurityClearanceLevel",
                table: "Locations",
                column: "SecurityClearanceLevel");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationAlerts_AcknowledgedBy",
                table: "NotificationAlerts",
                column: "AcknowledgedBy");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationAlerts_CreatedBy",
                table: "NotificationAlerts",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationAlerts_CreatedOn_IsAcknowledged",
                table: "NotificationAlerts",
                columns: new[] { "CreatedOn", "IsAcknowledged" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationAlerts_ExpiresOn",
                table: "NotificationAlerts",
                column: "ExpiresOn",
                filter: "[ExpiresOn] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationAlerts_IsAcknowledged",
                table: "NotificationAlerts",
                column: "IsAcknowledged");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationAlerts_ModifiedBy",
                table: "NotificationAlerts",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationAlerts_Priority",
                table: "NotificationAlerts",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationAlerts_TargetLocationId",
                table: "NotificationAlerts",
                column: "TargetLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationAlerts_TargetRole",
                table: "NotificationAlerts",
                column: "TargetRole");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationAlerts_TargetUserId",
                table: "NotificationAlerts",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationAlerts_Type",
                table: "NotificationAlerts",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_OccupancyLog_CreatedOn",
                table: "OccupancyLogs",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_OccupancyLog_IsActive",
                table: "OccupancyLogs",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_OccupancyLog_IsActive_CreatedOn",
                table: "OccupancyLogs",
                columns: new[] { "IsActive", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_OccupancyLogs_Date",
                table: "OccupancyLogs",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_OccupancyLogs_Date_Location",
                table: "OccupancyLogs",
                columns: new[] { "Date", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_OccupancyLogs_Date_TimeSlot",
                table: "OccupancyLogs",
                columns: new[] { "Date", "TimeSlotId" });

            migrationBuilder.CreateIndex(
                name: "IX_OccupancyLogs_LastUpdated",
                table: "OccupancyLogs",
                column: "LastUpdated");

            migrationBuilder.CreateIndex(
                name: "IX_OccupancyLogs_LocationId",
                table: "OccupancyLogs",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_OccupancyLogs_TimeSlotId",
                table: "OccupancyLogs",
                column: "TimeSlotId");

            migrationBuilder.CreateIndex(
                name: "UX_OccupancyLogs_Date_TimeSlot_Location",
                table: "OccupancyLogs",
                columns: new[] { "Date", "TimeSlotId", "LocationId" },
                unique: true,
                filter: "[TimeSlotId] IS NOT NULL AND [LocationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorSessions_ConnectionId",
                table: "OperatorSessions",
                column: "ConnectionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperatorSessions_CreatedBy",
                table: "OperatorSessions",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorSessions_LastActivity",
                table: "OperatorSessions",
                column: "LastActivity");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorSessions_LocationId",
                table: "OperatorSessions",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorSessions_ModifiedBy",
                table: "OperatorSessions",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorSessions_SessionEnd_Status",
                table: "OperatorSessions",
                columns: new[] { "SessionEnd", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OperatorSessions_Status",
                table: "OperatorSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorSessions_UserId",
                table: "OperatorSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionChangeAuditLogs_ChangedAt",
                table: "PermissionChangeAuditLogs",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionChangeAuditLogs_ChangedBy",
                table: "PermissionChangeAuditLogs",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionChangeAuditLogs_ChangeType_ChangedAt",
                table: "PermissionChangeAuditLogs",
                columns: new[] { "ChangeType", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PermissionChangeAuditLogs_PermissionId",
                table: "PermissionChangeAuditLogs",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionChangeAuditLogs_RoleId",
                table: "PermissionChangeAuditLogs",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionChangeAuditLogs_UserId",
                table: "PermissionChangeAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Category",
                table: "Permissions",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Category_DisplayOrder",
                table: "Permissions",
                columns: new[] { "Category", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_CreatedBy",
                table: "Permissions",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Name",
                table: "Permissions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_CreatedOn",
                table: "RefreshTokens",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_IsActive",
                table: "RefreshTokens",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_IsActive_CreatedOn",
                table: "RefreshTokens",
                columns: new[] { "IsActive", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_CreatedByIp",
                table: "RefreshTokens",
                column: "CreatedByIp");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_DeviceFingerprint",
                table: "RefreshTokens",
                column: "DeviceFingerprint");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiryDate",
                table: "RefreshTokens",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_IsRevoked",
                table: "RefreshTokens",
                column: "IsRevoked");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_IsUsed",
                table: "RefreshTokens",
                column: "IsUsed");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_JwtId_Unique",
                table: "RefreshTokens",
                column: "JwtId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ReplacedByTokenId",
                table: "RefreshTokens",
                column: "ReplacedByTokenId",
                unique: true,
                filter: "[ReplacedByTokenId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Status_ExpiryDate",
                table: "RefreshTokens",
                columns: new[] { "IsActive", "IsUsed", "IsRevoked", "ExpiryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token_Unique",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_IsActive_ExpiryDate",
                table: "RefreshTokens",
                columns: new[] { "UserId", "IsActive", "ExpiryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_GrantedBy",
                table: "RolePermissions",
                column: "GrantedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_CreatedBy",
                table: "Roles",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_HierarchyLevel",
                table: "Roles",
                column: "HierarchyLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_IsActive_DisplayOrder",
                table: "Roles",
                columns: new[] { "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_ModifiedBy",
                table: "Roles",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffAttendances_CameraId",
                table: "StaffAttendances",
                column: "CameraId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffAttendances_CheckInTime",
                table: "StaffAttendances",
                column: "CheckInTime");

            migrationBuilder.CreateIndex(
                name: "IX_StaffAttendances_CreatedByUserId",
                table: "StaffAttendances",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffAttendances_ModifiedByUserId",
                table: "StaffAttendances",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffAttendances_UserId",
                table: "StaffAttendances",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffAttendances_UserId_CheckOutTime",
                table: "StaffAttendances",
                columns: new[] { "UserId", "CheckOutTime" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_Category",
                table: "SystemConfigurations",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_Category_Key",
                table: "SystemConfigurations",
                columns: new[] { "Category", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_CreatedBy",
                table: "SystemConfigurations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_Environment",
                table: "SystemConfigurations",
                column: "Environment");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_Group",
                table: "SystemConfigurations",
                column: "Group");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_IsEncrypted",
                table: "SystemConfigurations",
                column: "IsEncrypted");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_ModifiedBy",
                table: "SystemConfigurations",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_RequiresRestart",
                table: "SystemConfigurations",
                column: "RequiresRestart");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBooking_CreatedBy",
                table: "TimeSlotBookings",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBooking_CreatedOn",
                table: "TimeSlotBookings",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBooking_DeletedBy",
                table: "TimeSlotBookings",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBooking_IsActive",
                table: "TimeSlotBookings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBooking_IsActive_CreatedOn",
                table: "TimeSlotBookings",
                columns: new[] { "IsActive", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBooking_IsDeleted",
                table: "TimeSlotBookings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBooking_IsDeleted_DeletedOn",
                table: "TimeSlotBookings",
                columns: new[] { "IsDeleted", "DeletedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBooking_ModifiedBy",
                table: "TimeSlotBookings",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBookings_Availability",
                table: "TimeSlotBookings",
                columns: new[] { "TimeSlotId", "BookingDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBookings_BookedBy",
                table: "TimeSlotBookings",
                column: "BookedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBookings_BookingDate",
                table: "TimeSlotBookings",
                column: "BookingDate");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBookings_CancelledBy",
                table: "TimeSlotBookings",
                column: "CancelledBy");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBookings_InvitationId",
                table: "TimeSlotBookings",
                column: "InvitationId",
                unique: true,
                filter: "[InvitationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBookings_Status",
                table: "TimeSlotBookings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBookings_TimeSlotId",
                table: "TimeSlotBookings",
                column: "TimeSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlot_CreatedBy",
                table: "TimeSlots",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlot_CreatedOn",
                table: "TimeSlots",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlot_DeletedBy",
                table: "TimeSlots",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlot_IsActive",
                table: "TimeSlots",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlot_IsActive_CreatedOn",
                table: "TimeSlots",
                columns: new[] { "IsActive", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlot_IsDeleted",
                table: "TimeSlots",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlot_IsDeleted_DeletedOn",
                table: "TimeSlots",
                columns: new[] { "IsDeleted", "DeletedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlot_ModifiedBy",
                table: "TimeSlots",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlots_Location_Time",
                table: "TimeSlots",
                columns: new[] { "LocationId", "StartTime", "EndTime" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlots_Name",
                table: "TimeSlots",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_User_CreatedBy",
                table: "Users",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_User_CreatedOn",
                table: "Users",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_User_IsActive",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_User_IsActive_CreatedOn",
                table: "Users",
                columns: new[] { "IsActive", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_User_ModifiedBy",
                table: "Users",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DeletedBy",
                table: "Users",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Department",
                table: "Users",
                column: "Department");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DepartmentId",
                table: "Users",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmployeeId_Unique",
                table: "Users",
                column: "EmployeeId",
                unique: true,
                filter: "[EmployeeId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Users_FailedLoginAttempts",
                table: "Users",
                column: "FailedLoginAttempts");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive_Status_IsDeleted",
                table: "Users",
                columns: new[] { "IsActive", "Status", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsDeleted",
                table: "Users",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsLockedOut",
                table: "Users",
                column: "IsLockedOut");

            migrationBuilder.CreateIndex(
                name: "IX_Users_LastLoginDate",
                table: "Users",
                column: "LastLoginDate");

            migrationBuilder.CreateIndex(
                name: "IX_Users_LockoutEnd",
                table: "Users",
                column: "LockoutEnd");

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedEmail_Unique",
                table: "Users",
                column: "NormalizedEmail",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PasswordChangedDate",
                table: "Users",
                column: "PasswordChangedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role",
                table: "Users",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Status",
                table: "Users",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorAccess_GrantedBy",
                table: "VisitorAccess",
                column: "GrantedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorAccess_UserId",
                table: "VisitorAccess",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorAccess_UserId_VisitorId",
                table: "VisitorAccess",
                columns: new[] { "UserId", "VisitorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisitorAccess_VisitorId",
                table: "VisitorAccess",
                column: "VisitorId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorDocument_CreatedBy",
                table: "VisitorDocuments",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorDocument_CreatedOn",
                table: "VisitorDocuments",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorDocument_IsActive",
                table: "VisitorDocuments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorDocument_IsActive_CreatedOn",
                table: "VisitorDocuments",
                columns: new[] { "IsActive", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_VisitorDocument_ModifiedBy",
                table: "VisitorDocuments",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorDocuments_DeletedByUserId",
                table: "VisitorDocuments",
                column: "DeletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorDocuments_DocumentType",
                table: "VisitorDocuments",
                column: "DocumentType");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorDocuments_ExpirationDate",
                table: "VisitorDocuments",
                column: "ExpirationDate");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorDocuments_FileHash",
                table: "VisitorDocuments",
                column: "FileHash");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorDocuments_VisitorId",
                table: "VisitorDocuments",
                column: "VisitorId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorNote_CreatedBy",
                table: "VisitorNotes",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorNote_CreatedOn",
                table: "VisitorNotes",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorNote_IsActive",
                table: "VisitorNotes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorNote_IsActive_CreatedOn",
                table: "VisitorNotes",
                columns: new[] { "IsActive", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_VisitorNote_ModifiedBy",
                table: "VisitorNotes",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorNotes_Category",
                table: "VisitorNotes",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorNotes_DeletedByUserId",
                table: "VisitorNotes",
                column: "DeletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorNotes_FollowUpDate",
                table: "VisitorNotes",
                column: "FollowUpDate");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorNotes_IsFlagged",
                table: "VisitorNotes",
                column: "IsFlagged");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorNotes_Priority",
                table: "VisitorNotes",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorNotes_VisitorId",
                table: "VisitorNotes",
                column: "VisitorId");

            migrationBuilder.CreateIndex(
                name: "IX_Visitor_CreatedBy",
                table: "Visitors",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Visitor_CreatedOn",
                table: "Visitors",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Visitor_IsActive",
                table: "Visitors",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Visitor_IsActive_CreatedOn",
                table: "Visitors",
                columns: new[] { "IsActive", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Visitor_ModifiedBy",
                table: "Visitors",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_BlacklistedBy",
                table: "Visitors",
                column: "BlacklistedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_Company",
                table: "Visitors",
                column: "Company");

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_CompanyId",
                table: "Visitors",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_DefaultVisitPurposeId",
                table: "Visitors",
                column: "DefaultVisitPurposeId");

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_DeletedByUserId",
                table: "Visitors",
                column: "DeletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_GovernmentId",
                table: "Visitors",
                column: "GovernmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_IsBlacklisted",
                table: "Visitors",
                column: "IsBlacklisted");

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_IsVip",
                table: "Visitors",
                column: "IsVip");

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_LastVisitDate",
                table: "Visitors",
                column: "LastVisitDate");

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_NormalizedEmail",
                table: "Visitors",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_PreferredLocationId",
                table: "Visitors",
                column: "PreferredLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitPurpose_CreatedBy",
                table: "VisitPurposes",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VisitPurpose_CreatedOn",
                table: "VisitPurposes",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_VisitPurpose_IsActive",
                table: "VisitPurposes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_VisitPurpose_IsActive_CreatedOn",
                table: "VisitPurposes",
                columns: new[] { "IsActive", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_VisitPurpose_ModifiedBy",
                table: "VisitPurposes",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VisitPurposes_Code",
                table: "VisitPurposes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisitPurposes_DeletedByUserId",
                table: "VisitPurposes",
                column: "DeletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitPurposes_DisplayOrder",
                table: "VisitPurposes",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_VisitPurposes_Name",
                table: "VisitPurposes",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_AlertEscalations_Locations_LocationId",
                table: "AlertEscalations",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AlertEscalations_Users_CreatedBy",
                table: "AlertEscalations",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AlertEscalations_Users_EscalationTargetUserId",
                table: "AlertEscalations",
                column: "EscalationTargetUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AlertEscalations_Users_ModifiedBy",
                table: "AlertEscalations",
                column: "ModifiedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AlertRecipientConfigurations_Users_CreatedBy",
                table: "AlertRecipientConfigurations",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AlertRecipientConfigurations_Users_ModifiedBy",
                table: "AlertRecipientConfigurations",
                column: "ModifiedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AlertRecipientConfigurations_Users_TargetUserId",
                table: "AlertRecipientConfigurations",
                column: "TargetUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Users_UserId",
                table: "AuditLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_BackupRecords_Users_CreatedBy",
                table: "BackupRecords",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BackupRecords_Users_ModifiedBy",
                table: "BackupRecords",
                column: "ModifiedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BackupRecords_Users_TriggeredByUserId",
                table: "BackupRecords",
                column: "TriggeredByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_BlacklistOverrideRequests_Users_RequestedByUserId",
                table: "BlacklistOverrideRequests",
                column: "RequestedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BlacklistOverrideRequests_Users_ReviewedByUserId",
                table: "BlacklistOverrideRequests",
                column: "ReviewedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BlacklistOverrideRequests_Visitors_VisitorId",
                table: "BlacklistOverrideRequests",
                column: "VisitorId",
                principalTable: "Visitors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Camera_CreatedBy_User",
                table: "Cameras",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Camera_DeletedBy_User",
                table: "Cameras",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Camera_ModifiedBy_User",
                table: "Cameras",
                column: "ModifiedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Cameras_Locations_LocationId",
                table: "Cameras",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_BlacklistedBy_User",
                table: "Companies",
                column: "BlacklistedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_VerifiedBy_User",
                table: "Companies",
                column: "VerifiedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Company_CreatedBy_User",
                table: "Companies",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Company_DeletedBy_User",
                table: "Companies",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Company_ModifiedBy_User",
                table: "Companies",
                column: "ModifiedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ConfigurationAudits_SystemConfigurations_SystemConfigurationId",
                table: "ConfigurationAudits",
                column: "SystemConfigurationId",
                principalTable: "SystemConfigurations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ConfigurationAudits_Users_ApprovedBy",
                table: "ConfigurationAudits",
                column: "ApprovedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ConfigurationAudits_Users_CreatedBy",
                table: "ConfigurationAudits",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ConfigurationAudits_Users_ModifiedBy",
                table: "ConfigurationAudits",
                column: "ModifiedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Department_CreatedBy_User",
                table: "Departments",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Department_DeletedBy_User",
                table: "Departments",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Department_ModifiedBy_User",
                table: "Departments",
                column: "ModifiedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Manager_User",
                table: "Departments",
                column: "ManagerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_EmergencyContact_CreatedBy_User",
                table: "EmergencyContacts",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmergencyContact_ModifiedBy_User",
                table: "EmergencyContacts",
                column: "ModifiedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmergencyContacts_Users_DeletedByUserId",
                table: "EmergencyContacts",
                column: "DeletedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EmergencyContacts_Visitors_VisitorId",
                table: "EmergencyContacts",
                column: "VisitorId",
                principalTable: "Visitors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InvitationApprovals_Invitations_InvitationId",
                table: "InvitationApprovals",
                column: "InvitationId",
                principalTable: "Invitations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InvitationApprovals_Users_ApproverId",
                table: "InvitationApprovals",
                column: "ApproverId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InvitationApprovals_Users_CreatedByUserId",
                table: "InvitationApprovals",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InvitationApprovals_Users_EscalatedToUserId",
                table: "InvitationApprovals",
                column: "EscalatedToUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InvitationApprovals_Users_ModifiedByUserId",
                table: "InvitationApprovals",
                column: "ModifiedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InvitationEvents_Invitations_InvitationId",
                table: "InvitationEvents",
                column: "InvitationId",
                principalTable: "Invitations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InvitationEvents_Users_CreatedByUserId",
                table: "InvitationEvents",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InvitationEvents_Users_ModifiedByUserId",
                table: "InvitationEvents",
                column: "ModifiedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InvitationEvents_Users_TriggeredBy",
                table: "InvitationEvents",
                column: "TriggeredBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_Locations_LocationId",
                table: "Invitations",
                column: "LocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_TimeSlots_TimeSlotId",
                table: "Invitations",
                column: "TimeSlotId",
                principalTable: "TimeSlots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_Users_ApprovedBy",
                table: "Invitations",
                column: "ApprovedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_Users_CreatedByUserId",
                table: "Invitations",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_Users_DeletedByUserId",
                table: "Invitations",
                column: "DeletedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_Users_HostId",
                table: "Invitations",
                column: "HostId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_Users_ModifiedByUserId",
                table: "Invitations",
                column: "ModifiedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_Users_RejectedBy",
                table: "Invitations",
                column: "RejectedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_VisitPurposes_VisitPurposeId",
                table: "Invitations",
                column: "VisitPurposeId",
                principalTable: "VisitPurposes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_Visitors_VisitorId",
                table: "Invitations",
                column: "VisitorId",
                principalTable: "Visitors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InvitationTemplates_Locations_DefaultLocationId",
                table: "InvitationTemplates",
                column: "DefaultLocationId",
                principalTable: "Locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InvitationTemplates_Users_CreatedByUserId",
                table: "InvitationTemplates",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InvitationTemplates_Users_DeletedByUserId",
                table: "InvitationTemplates",
                column: "DeletedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InvitationTemplates_Users_ModifiedByUserId",
                table: "InvitationTemplates",
                column: "ModifiedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InvitationTemplates_VisitPurposes_DefaultVisitPurposeId",
                table: "InvitationTemplates",
                column: "DefaultVisitPurposeId",
                principalTable: "VisitPurposes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Location_CreatedBy_User",
                table: "Locations",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Location_ModifiedBy_User",
                table: "Locations",
                column: "ModifiedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Locations_Users_DeletedByUserId",
                table: "Locations",
                column: "DeletedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationAlerts_Users_AcknowledgedBy",
                table: "NotificationAlerts",
                column: "AcknowledgedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationAlerts_Users_CreatedBy",
                table: "NotificationAlerts",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationAlerts_Users_ModifiedBy",
                table: "NotificationAlerts",
                column: "ModifiedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationAlerts_Users_TargetUserId",
                table: "NotificationAlerts",
                column: "TargetUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OccupancyLogs_TimeSlots_TimeSlotId",
                table: "OccupancyLogs",
                column: "TimeSlotId",
                principalTable: "TimeSlots",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_OperatorSessions_Users_CreatedBy",
                table: "OperatorSessions",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OperatorSessions_Users_ModifiedBy",
                table: "OperatorSessions",
                column: "ModifiedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OperatorSessions_Users_UserId",
                table: "OperatorSessions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PermissionChangeAuditLogs_Permissions_PermissionId",
                table: "PermissionChangeAuditLogs",
                column: "PermissionId",
                principalTable: "Permissions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PermissionChangeAuditLogs_Roles_RoleId",
                table: "PermissionChangeAuditLogs",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PermissionChangeAuditLogs_Users_ChangedBy",
                table: "PermissionChangeAuditLogs",
                column: "ChangedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PermissionChangeAuditLogs_Users_UserId",
                table: "PermissionChangeAuditLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Permissions_Users_CreatedBy",
                table: "Permissions",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_Users_UserId",
                table: "RefreshTokens",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissions_Roles_RoleId",
                table: "RolePermissions",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissions_Users_GrantedBy",
                table: "RolePermissions",
                column: "GrantedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Roles_Users_CreatedBy",
                table: "Roles",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Roles_Users_ModifiedBy",
                table: "Roles",
                column: "ModifiedBy",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Department_CreatedBy_User",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Department_DeletedBy_User",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Department_ModifiedBy_User",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Manager_User",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Roles_Users_CreatedBy",
                table: "Roles");

            migrationBuilder.DropForeignKey(
                name: "FK_Roles_Users_ModifiedBy",
                table: "Roles");

            migrationBuilder.DropTable(
                name: "AlertEscalations");

            migrationBuilder.DropTable(
                name: "AlertRecipientConfigurations");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BackupRecords");

            migrationBuilder.DropTable(
                name: "BlacklistOverrideRequests");

            migrationBuilder.DropTable(
                name: "ConfigurationAudits");

            migrationBuilder.DropTable(
                name: "EmergencyContacts");

            migrationBuilder.DropTable(
                name: "InvitationApprovals");

            migrationBuilder.DropTable(
                name: "InvitationEvents");

            migrationBuilder.DropTable(
                name: "InvitationTemplates");

            migrationBuilder.DropTable(
                name: "NotificationAlerts");

            migrationBuilder.DropTable(
                name: "OccupancyLogs");

            migrationBuilder.DropTable(
                name: "OperatorSessions");

            migrationBuilder.DropTable(
                name: "PermissionChangeAuditLogs");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "StaffAttendances");

            migrationBuilder.DropTable(
                name: "TimeSlotBookings");

            migrationBuilder.DropTable(
                name: "VisitorAccess");

            migrationBuilder.DropTable(
                name: "VisitorDocuments");

            migrationBuilder.DropTable(
                name: "VisitorNotes");

            migrationBuilder.DropTable(
                name: "SystemConfigurations");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Cameras");

            migrationBuilder.DropTable(
                name: "Invitations");

            migrationBuilder.DropTable(
                name: "TimeSlots");

            migrationBuilder.DropTable(
                name: "Visitors");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "VisitPurposes");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
