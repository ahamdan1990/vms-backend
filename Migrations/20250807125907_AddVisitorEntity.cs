using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagementSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitorEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    MaxOccupancy = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    RequiresSecurityClearance = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SecurityClearanceLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
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
                    ModifiedBy = table.Column<int>(type: "int", nullable: true)
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
                    ModifiedBy = table.Column<int>(type: "int", nullable: true)
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
                    ModifiedBy = table.Column<int>(type: "int", nullable: true)
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
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileExtension = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FileHash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsEncrypted = table.Column<bool>(type: "bit", nullable: false),
                    IsSensitive = table.Column<bool>(type: "bit", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AccessLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Standard"),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true)
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
                    ModifiedBy = table.Column<int>(type: "int", nullable: true)
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
                        name: "FK_VisitorNotes_Visitors_VisitorId",
                        column: x => x.VisitorId,
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "EmergencyContacts");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "VisitorDocuments");

            migrationBuilder.DropTable(
                name: "VisitorNotes");

            migrationBuilder.DropTable(
                name: "VisitPurposes");

            migrationBuilder.DropTable(
                name: "Visitors");
        }
    }
}
