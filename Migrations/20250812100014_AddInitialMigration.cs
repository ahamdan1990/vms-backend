using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagementSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    JobTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EmployeeId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
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
                        name: "FK_Users_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
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
                        name: "FK_Location_CreatedBy_User",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Location_ModifiedBy_User",
                        column: x => x.ModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Locations_Locations_ParentLocationId",
                        column: x => x.ParentLocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Locations_Users_DeletedByUserId",
                        column: x => x.DeletedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
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
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    table.ForeignKey(
                        name: "FK_ConfigurationAudits_SystemConfigurations_SystemConfigurationId",
                        column: x => x.SystemConfigurationId,
                        principalTable: "SystemConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConfigurationAudits_Users_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfigurationAudits_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfigurationAudits_Users_ModifiedBy",
                        column: x => x.ModifiedBy,
                        principalTable: "Users",
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
                    table.ForeignKey(
                        name: "FK_EmergencyContact_CreatedBy_User",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmergencyContact_ModifiedBy_User",
                        column: x => x.ModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmergencyContacts_Users_DeletedByUserId",
                        column: x => x.DeletedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmergencyContacts_Visitors_VisitorId",
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
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
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
                    table.ForeignKey(
                        name: "FK_Invitations_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Invitations_Users_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Invitations_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Invitations_Users_DeletedByUserId",
                        column: x => x.DeletedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Invitations_Users_HostId",
                        column: x => x.HostId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invitations_Users_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Invitations_Users_RejectedBy",
                        column: x => x.RejectedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Invitations_VisitPurposes_VisitPurposeId",
                        column: x => x.VisitPurposeId,
                        principalTable: "VisitPurposes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Invitations_Visitors_VisitorId",
                        column: x => x.VisitorId,
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    table.ForeignKey(
                        name: "FK_InvitationTemplates_Locations_DefaultLocationId",
                        column: x => x.DefaultLocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InvitationTemplates_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InvitationTemplates_Users_DeletedByUserId",
                        column: x => x.DeletedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InvitationTemplates_Users_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InvitationTemplates_VisitPurposes_DefaultVisitPurposeId",
                        column: x => x.DefaultVisitPurposeId,
                        principalTable: "VisitPurposes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
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
                    table.ForeignKey(
                        name: "FK_InvitationApprovals_Invitations_InvitationId",
                        column: x => x.InvitationId,
                        principalTable: "Invitations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InvitationApprovals_Users_ApproverId",
                        column: x => x.ApproverId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InvitationApprovals_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InvitationApprovals_Users_EscalatedToUserId",
                        column: x => x.EscalatedToUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InvitationApprovals_Users_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
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
                    table.ForeignKey(
                        name: "FK_InvitationEvents_Invitations_InvitationId",
                        column: x => x.InvitationId,
                        principalTable: "Invitations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InvitationEvents_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InvitationEvents_Users_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InvitationEvents_Users_TriggeredBy",
                        column: x => x.TriggeredBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

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
                name: "IX_Users_Status",
                table: "Users",
                column: "Status");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

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
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "VisitorDocuments");

            migrationBuilder.DropTable(
                name: "VisitorNotes");

            migrationBuilder.DropTable(
                name: "SystemConfigurations");

            migrationBuilder.DropTable(
                name: "Invitations");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "VisitPurposes");

            migrationBuilder.DropTable(
                name: "Visitors");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
