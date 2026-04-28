using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagementSystem.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertRecipientConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    table.ForeignKey(
                        name: "FK_AlertRecipientConfigurations_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AlertRecipientConfigurations_Users_ModifiedBy",
                        column: x => x.ModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AlertRecipientConfigurations_Users_TargetUserId",
                        column: x => x.TargetUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertRecipientConfigurations");
        }
    }
}
