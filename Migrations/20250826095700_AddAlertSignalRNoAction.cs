using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagementSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertSignalRNoAction : Migration
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
                    table.ForeignKey(
                        name: "FK_AlertEscalations_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AlertEscalations_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AlertEscalations_Users_EscalationTargetUserId",
                        column: x => x.EscalationTargetUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AlertEscalations_Users_ModifiedBy",
                        column: x => x.ModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
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
                    table.ForeignKey(
                        name: "FK_NotificationAlerts_Users_AcknowledgedBy",
                        column: x => x.AcknowledgedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NotificationAlerts_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NotificationAlerts_Users_ModifiedBy",
                        column: x => x.ModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NotificationAlerts_Users_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
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
                    table.ForeignKey(
                        name: "FK_OperatorSessions_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OperatorSessions_Users_ModifiedBy",
                        column: x => x.ModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OperatorSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertEscalations");

            migrationBuilder.DropTable(
                name: "NotificationAlerts");

            migrationBuilder.DropTable(
                name: "OperatorSessions");
        }
    }
}
