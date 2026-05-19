using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagementSystem.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTemporaryLeave : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TemporaryLeaves",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    StaffPresenceId = table.Column<int>(type: "int", nullable: true),
                    InvitationId = table.Column<int>(type: "int", nullable: true),
                    LeftAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReturnedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RecordedById = table.Column<int>(type: "int", nullable: false),
                    ReturnedById = table.Column<int>(type: "int", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemporaryLeaves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemporaryLeaves_Invitations_InvitationId",
                        column: x => x.InvitationId,
                        principalTable: "Invitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemporaryLeaves_StaffPresences_StaffPresenceId",
                        column: x => x.StaffPresenceId,
                        principalTable: "StaffPresences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemporaryLeaves_Users_RecordedById",
                        column: x => x.RecordedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemporaryLeaves_Users_ReturnedById",
                        column: x => x.ReturnedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TemporaryLeaves_InvitationId",
                table: "TemporaryLeaves",
                column: "InvitationId");

            migrationBuilder.CreateIndex(
                name: "IX_TemporaryLeaves_PersonType_IsActive",
                table: "TemporaryLeaves",
                columns: new[] { "PersonType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TemporaryLeaves_RecordedById",
                table: "TemporaryLeaves",
                column: "RecordedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemporaryLeaves_ReturnedById",
                table: "TemporaryLeaves",
                column: "ReturnedById");

            migrationBuilder.CreateIndex(
                name: "IX_TemporaryLeaves_StaffPresenceId",
                table: "TemporaryLeaves",
                column: "StaffPresenceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemporaryLeaves");
        }
    }
}
