using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagementSystem.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffPresence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AffiliatedOrganization",
                table: "Invitations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCivilian",
                table: "Invitations",
                type: "bit",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StaffPresences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CheckedInAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckedOutAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    CheckedInById = table.Column<int>(type: "int", nullable: false),
                    CheckedOutById = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffPresences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffPresences_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StaffPresences_Users_CheckedInById",
                        column: x => x.CheckedInById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffPresences_Users_CheckedOutById",
                        column: x => x.CheckedOutById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StaffPresences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StaffPresences_CheckedInAt",
                table: "StaffPresences",
                column: "CheckedInAt");

            migrationBuilder.CreateIndex(
                name: "IX_StaffPresences_CheckedInById",
                table: "StaffPresences",
                column: "CheckedInById");

            migrationBuilder.CreateIndex(
                name: "IX_StaffPresences_CheckedOutById",
                table: "StaffPresences",
                column: "CheckedOutById");

            migrationBuilder.CreateIndex(
                name: "IX_StaffPresences_LocationId",
                table: "StaffPresences",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffPresences_Status",
                table: "StaffPresences",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StaffPresences_UserId_Status",
                table: "StaffPresences",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StaffPresences");

            migrationBuilder.DropColumn(
                name: "AffiliatedOrganization",
                table: "Invitations");

            migrationBuilder.DropColumn(
                name: "IsCivilian",
                table: "Invitations");
        }
    }
}
