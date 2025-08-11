using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagementSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInvitationModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "VisitorNotes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "VisitorNotes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOn",
                table: "VisitorNotes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "VisitorNotes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "VisitorDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "VisitorDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOn",
                table: "VisitorDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "VisitorDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "VisitorDocuments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRequired",
                table: "VisitorDocuments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MimeType",
                table: "VisitorDocuments",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "VisitorDocuments",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AccessLevel",
                table: "Locations",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxCapacity",
                table: "Locations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresEscort",
                table: "Locations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Zone",
                table: "Locations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "EmergencyContacts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "EmergencyContacts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOn",
                table: "EmergencyContacts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "EmergencyContacts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Invitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvitationNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
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
                name: "IX_VisitorNotes_DeletedByUserId",
                table: "VisitorNotes",
                column: "DeletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorDocuments_DeletedByUserId",
                table: "VisitorDocuments",
                column: "DeletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyContacts_DeletedByUserId",
                table: "EmergencyContacts",
                column: "DeletedByUserId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_EmergencyContacts_Users_DeletedByUserId",
                table: "EmergencyContacts",
                column: "DeletedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VisitorDocuments_Users_DeletedByUserId",
                table: "VisitorDocuments",
                column: "DeletedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VisitorNotes_Users_DeletedByUserId",
                table: "VisitorNotes",
                column: "DeletedByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmergencyContacts_Users_DeletedByUserId",
                table: "EmergencyContacts");

            migrationBuilder.DropForeignKey(
                name: "FK_VisitorDocuments_Users_DeletedByUserId",
                table: "VisitorDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_VisitorNotes_Users_DeletedByUserId",
                table: "VisitorNotes");

            migrationBuilder.DropTable(
                name: "InvitationApprovals");

            migrationBuilder.DropTable(
                name: "InvitationEvents");

            migrationBuilder.DropTable(
                name: "InvitationTemplates");

            migrationBuilder.DropTable(
                name: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_VisitorNotes_DeletedByUserId",
                table: "VisitorNotes");

            migrationBuilder.DropIndex(
                name: "IX_VisitorDocuments_DeletedByUserId",
                table: "VisitorDocuments");

            migrationBuilder.DropIndex(
                name: "IX_EmergencyContacts_DeletedByUserId",
                table: "EmergencyContacts");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "VisitorNotes");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "VisitorNotes");

            migrationBuilder.DropColumn(
                name: "DeletedOn",
                table: "VisitorNotes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "VisitorNotes");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "VisitorDocuments");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "VisitorDocuments");

            migrationBuilder.DropColumn(
                name: "DeletedOn",
                table: "VisitorDocuments");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "VisitorDocuments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "VisitorDocuments");

            migrationBuilder.DropColumn(
                name: "IsRequired",
                table: "VisitorDocuments");

            migrationBuilder.DropColumn(
                name: "MimeType",
                table: "VisitorDocuments");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "VisitorDocuments");

            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "MaxCapacity",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "RequiresEscort",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "Zone",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "EmergencyContacts");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "EmergencyContacts");

            migrationBuilder.DropColumn(
                name: "DeletedOn",
                table: "EmergencyContacts");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "EmergencyContacts");
        }
    }
}
