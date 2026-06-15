using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagementSystem.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PreserveAlertHistoryReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AlertEscalationLogs_NotificationAlerts_NotificationAlertId",
                table: "AlertEscalationLogs");

            migrationBuilder.AddColumn<int>(
                name: "NotificationAlertReferenceId",
                table: "AlertEscalationLogs",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("UPDATE [AlertEscalationLogs] SET [NotificationAlertReferenceId] = [NotificationAlertId] WHERE [NotificationAlertReferenceId] IS NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_AlertEscalationLogs_NotificationAlertReferenceId",
                table: "AlertEscalationLogs",
                column: "NotificationAlertReferenceId");

            migrationBuilder.AddForeignKey(
                name: "FK_AlertEscalationLogs_NotificationAlerts_NotificationAlertReferenceId",
                table: "AlertEscalationLogs",
                column: "NotificationAlertReferenceId",
                principalTable: "NotificationAlerts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AlertEscalationLogs_NotificationAlerts_NotificationAlertReferenceId",
                table: "AlertEscalationLogs");

            migrationBuilder.DropIndex(
                name: "IX_AlertEscalationLogs_NotificationAlertReferenceId",
                table: "AlertEscalationLogs");

            migrationBuilder.DropColumn(
                name: "NotificationAlertReferenceId",
                table: "AlertEscalationLogs");

            migrationBuilder.AddForeignKey(
                name: "FK_AlertEscalationLogs_NotificationAlerts_NotificationAlertId",
                table: "AlertEscalationLogs",
                column: "NotificationAlertId",
                principalTable: "NotificationAlerts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
