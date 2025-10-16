using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagementSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitorsFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultVisitPurposeId",
                table: "Visitors",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreferredLocationId",
                table: "Visitors",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeZone",
                table: "Visitors",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_DefaultVisitPurposeId",
                table: "Visitors",
                column: "DefaultVisitPurposeId");

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_PreferredLocationId",
                table: "Visitors",
                column: "PreferredLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Visitors_Locations_PreferredLocationId",
                table: "Visitors",
                column: "PreferredLocationId",
                principalTable: "Locations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Visitors_VisitPurposes_DefaultVisitPurposeId",
                table: "Visitors",
                column: "DefaultVisitPurposeId",
                principalTable: "VisitPurposes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Visitors_Locations_PreferredLocationId",
                table: "Visitors");

            migrationBuilder.DropForeignKey(
                name: "FK_Visitors_VisitPurposes_DefaultVisitPurposeId",
                table: "Visitors");

            migrationBuilder.DropIndex(
                name: "IX_Visitors_DefaultVisitPurposeId",
                table: "Visitors");

            migrationBuilder.DropIndex(
                name: "IX_Visitors_PreferredLocationId",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "DefaultVisitPurposeId",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "PreferredLocationId",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "TimeZone",
                table: "Visitors");
        }
    }
}
