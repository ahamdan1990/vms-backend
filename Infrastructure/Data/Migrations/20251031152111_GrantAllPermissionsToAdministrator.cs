using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagementSystem.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class GrantAllPermissionsToAdministrator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Grant ALL missing permissions to Administrator role
            migrationBuilder.Sql(@"
                DECLARE @AdminRoleId INT;
                SELECT @AdminRoleId = Id FROM Roles WHERE Name = 'Administrator';

                -- Insert role-permission mappings for ALL permissions that Administrator doesn't have yet
                INSERT INTO RolePermissions (RoleId, PermissionId, GrantedAt, GrantedBy)
                SELECT
                    @AdminRoleId,
                    p.Id,
                    GETUTCDATE(),
                    @AdminRoleId
                FROM Permissions p
                WHERE p.IsActive = 1
                AND NOT EXISTS (
                    SELECT 1 FROM RolePermissions rp
                    WHERE rp.RoleId = @AdminRoleId AND rp.PermissionId = p.Id
                );

                PRINT 'Granted missing permissions to Administrator role';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This migration is not reversible as it would require knowing which permissions
            // were added by this migration vs existing permissions
            // In practice, reverting this would require manual intervention
        }
    }
}
