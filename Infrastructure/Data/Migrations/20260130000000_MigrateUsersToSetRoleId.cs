using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagementSystem.Api.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Migration to set RoleId for existing users based on their Role enum value
    /// This ensures all users transition to the new database-driven permission system
    /// </summary>
    public partial class MigrateUsersToSetRoleId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migrate users to set RoleId based on their Role enum
            // Role enum values: 0=Staff, 1=Receptionist, 2=Administrator
            migrationBuilder.Sql(@"
                -- Update Staff users (Role = 0)
                UPDATE Users
                SET RoleId = (SELECT Id FROM Roles WHERE Name = 'Staff')
                WHERE RoleId IS NULL
                  AND Role = 0
                  AND IsDeleted = 0;

                -- Update Receptionist users (Role = 1)
                UPDATE Users
                SET RoleId = (SELECT Id FROM Roles WHERE Name = 'Receptionist')
                WHERE RoleId IS NULL
                  AND Role = 1
                  AND IsDeleted = 0;

                -- Update Administrator users (Role = 2)
                UPDATE Users
                SET RoleId = (SELECT Id FROM Roles WHERE Name = 'Administrator')
                WHERE RoleId IS NULL
                  AND Role = 2
                  AND IsDeleted = 0;

                -- Default any remaining users without RoleId to Staff role
                UPDATE Users
                SET RoleId = (SELECT Id FROM Roles WHERE Name = 'Staff')
                WHERE RoleId IS NULL
                  AND IsDeleted = 0;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback: Clear RoleId (optional, as the old system still works via fallback)
            migrationBuilder.Sql(@"
                UPDATE Users
                SET RoleId = NULL
                WHERE RoleId IS NOT NULL;
            ");
        }
    }
}
