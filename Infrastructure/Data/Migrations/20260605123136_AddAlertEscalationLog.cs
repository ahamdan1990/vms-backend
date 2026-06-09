using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagementSystem.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertEscalationLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertEscalationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NotificationAlertId = table.Column<int>(type: "int", nullable: false),
                    AlertEscalationId = table.Column<int>(type: "int", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetInfo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertEscalationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertEscalationLogs_AlertEscalations_AlertEscalationId",
                        column: x => x.AlertEscalationId,
                        principalTable: "AlertEscalations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AlertEscalationLogs_NotificationAlerts_NotificationAlertId",
                        column: x => x.NotificationAlertId,
                        principalTable: "NotificationAlerts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertEscalationLogs_AlertEscalationId",
                table: "AlertEscalationLogs",
                column: "AlertEscalationId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertEscalationLogs_AlertId_RuleId",
                table: "AlertEscalationLogs",
                columns: new[] { "NotificationAlertId", "AlertEscalationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertEscalationLogs");
        }
    }
}
