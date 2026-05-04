using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagementSystem.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixInvitationNumberAndDeprecatedUserRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enforce unique invitation numbers at the database level.
            // GenerateInvitationNumber() already uses a cryptographic suffix,
            // but this index is the final safety net against any collision.
            migrationBuilder.CreateIndex(
                name: "IX_Invitations_InvitationNumber_Unique",
                table: "Invitations",
                column: "InvitationNumber",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invitations_InvitationNumber_Unique",
                table: "Invitations");
        }
    }
}
