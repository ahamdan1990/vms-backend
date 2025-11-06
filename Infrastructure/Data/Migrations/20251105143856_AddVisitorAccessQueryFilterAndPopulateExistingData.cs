using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagementSystem.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitorAccessQueryFilterAndPopulateExistingData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Populate VisitorAccess for all existing visitors
            // Grant Creator access to the user who created each visitor
            migrationBuilder.Sql(@"
                INSERT INTO VisitorAccess (UserId, VisitorId, AccessType, GrantedBy, GrantedOn, CreatedOn, IsActive)
                SELECT
                    v.CreatedBy AS UserId,
                    v.Id AS VisitorId,
                    1 AS AccessType,  -- VisitorAccessType.Creator = 1
                    v.CreatedBy AS GrantedBy,
                    v.CreatedOn AS GrantedOn,
                    GETUTCDATE() AS CreatedOn,
                    1 AS IsActive
                FROM Visitors v
                WHERE v.CreatedBy IS NOT NULL
                  AND v.IsDeleted = 0
                  AND NOT EXISTS (
                      SELECT 1
                      FROM VisitorAccess va
                      WHERE va.UserId = v.CreatedBy
                        AND va.VisitorId = v.Id
                  );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove Creator access records that were added by this migration
            migrationBuilder.Sql(@"
                DELETE FROM VisitorAccess
                WHERE AccessType = 1;  -- VisitorAccessType.Creator = 1
            ");
        }
    }
}
