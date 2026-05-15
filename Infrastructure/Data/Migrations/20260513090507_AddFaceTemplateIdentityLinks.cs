using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagementSystem.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFaceTemplateIdentityLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "FaceTemplates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VisitorId",
                table: "FaceTemplates",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE ft
                SET VisitorId = ft.PersonId
                FROM FaceTemplates ft
                INNER JOIN Visitors v ON v.Id = ft.PersonId
                WHERE ft.VisitorId IS NULL
                  AND ft.PersonType = 'Visitor'
                """);

            migrationBuilder.Sql("""
                UPDATE ft
                SET UserId = ft.PersonId
                FROM FaceTemplates ft
                INNER JOIN Users u ON u.Id = ft.PersonId
                WHERE ft.UserId IS NULL
                  AND ft.PersonType IN ('Staff', 'User')
                """);

            migrationBuilder.CreateIndex(
                name: "IX_FaceTemplates_UserId",
                table: "FaceTemplates",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FaceTemplates_VisitorId",
                table: "FaceTemplates",
                column: "VisitorId");

            migrationBuilder.AddForeignKey(
                name: "FK_FaceTemplates_Users_UserId",
                table: "FaceTemplates",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FaceTemplates_Visitors_VisitorId",
                table: "FaceTemplates",
                column: "VisitorId",
                principalTable: "Visitors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FaceTemplates_Users_UserId",
                table: "FaceTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_FaceTemplates_Visitors_VisitorId",
                table: "FaceTemplates");

            migrationBuilder.DropIndex(
                name: "IX_FaceTemplates_UserId",
                table: "FaceTemplates");

            migrationBuilder.DropIndex(
                name: "IX_FaceTemplates_VisitorId",
                table: "FaceTemplates");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "FaceTemplates");

            migrationBuilder.DropColumn(
                name: "VisitorId",
                table: "FaceTemplates");
        }
    }
}
