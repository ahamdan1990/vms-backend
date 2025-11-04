using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagementSystem.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class GrantRolePermissionsToAdministrator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Grant all Role.* permissions to Administrator role
            migrationBuilder.Sql(@"
                -- Get the Administrator role ID
                DECLARE @AdminRoleId INT;
                SELECT @AdminRoleId = Id FROM Roles WHERE Name = 'Administrator';

                -- Insert role-permission mappings for all Role.* permissions
                INSERT INTO RolePermissions (RoleId, PermissionId, GrantedAt, GrantedBy)
                SELECT
                    @AdminRoleId,
                    p.Id,
                    GETUTCDATE(),
                    @AdminRoleId
                FROM Permissions p
                WHERE p.Name LIKE 'Role.%'
                AND NOT EXISTS (
                    SELECT 1 FROM RolePermissions rp
                    WHERE rp.RoleId = @AdminRoleId AND rp.PermissionId = p.Id
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove Role.* permissions from Administrator role
            migrationBuilder.Sql(@"
                -- Get the Administrator role ID
                DECLARE @AdminRoleId INT;
                SELECT @AdminRoleId = Id FROM Roles WHERE Name = 'Administrator';

                -- Delete role-permission mappings for Role.* permissions
                DELETE rp
                FROM RolePermissions rp
                INNER JOIN Permissions p ON rp.PermissionId = p.Id
                WHERE rp.RoleId = @AdminRoleId AND p.Name LIKE 'Role.%';
            ");
        }
    }
}
