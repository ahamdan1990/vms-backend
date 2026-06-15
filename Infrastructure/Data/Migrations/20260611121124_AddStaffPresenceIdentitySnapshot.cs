using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagementSystem.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffPresenceIdentitySnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserDepartment",
                table: "StaffPresences",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserDisplayName",
                table: "StaffPresences",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserJobTitle",
                table: "StaffPresences",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE sp
                SET
                    [UserDisplayName] = COALESCE(NULLIF(LTRIM(RTRIM(CONCAT(u.[FirstName], ' ', u.[LastName]))), ''), CONCAT('User #', sp.[UserId])),
                    [UserDepartment] = u.[Department],
                    [UserJobTitle] = u.[JobTitle]
                FROM [StaffPresences] sp
                LEFT JOIN [Users] u ON u.[Id] = sp.[UserId];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserDepartment",
                table: "StaffPresences");

            migrationBuilder.DropColumn(
                name: "UserDisplayName",
                table: "StaffPresences");

            migrationBuilder.DropColumn(
                name: "UserJobTitle",
                table: "StaffPresences");
        }
    }
}
