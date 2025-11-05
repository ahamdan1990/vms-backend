using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagementSystem.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Phase 0: Permission System Cleanup
    /// - Removes unused permission categories (Camera, Alert, Manual, Integration, Sync, Offline, Configuration)
    /// - Simplifies over-complex permissions (Dashboard, Report, Watchlist, Badge, SystemConfig)
    /// - Consolidates Alert permissions into Notification
    /// - Removes Operator role (merges into Receptionist)
    /// - Updates role hierarchy (Staff=1, Receptionist=2, Administrator=3)
    /// </summary>
    public partial class Phase0CleanupPermissionsAndRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================================================
            // STEP 1: Migrate users from Operator role to Receptionist role
            // ============================================================================
            migrationBuilder.Sql(@"
                DECLARE @OperatorRoleId INT;
                DECLARE @ReceptionistRoleId INT;

                SELECT @OperatorRoleId = Id FROM Roles WHERE Name = 'Operator';
                SELECT @ReceptionistRoleId = Id FROM Roles WHERE Name = 'Receptionist';

                -- Migrate users from Operator to Receptionist
                UPDATE Users
                SET RoleId = @ReceptionistRoleId
                WHERE RoleId = @OperatorRoleId;

                PRINT CONCAT('Migrated ', @@ROWCOUNT, ' users from Operator to Receptionist role');
            ");

            // ============================================================================
            // STEP 2: Delete Operator role permissions
            // ============================================================================
            migrationBuilder.Sql(@"
                DECLARE @OperatorRoleId INT;
                SELECT @OperatorRoleId = Id FROM Roles WHERE Name = 'Operator';

                DELETE FROM RolePermissions WHERE RoleId = @OperatorRoleId;
                PRINT 'Deleted Operator role permissions';
            ");

            // ============================================================================
            // STEP 3: Delete Operator role
            // ============================================================================
            migrationBuilder.Sql(@"
                DELETE FROM Roles WHERE Name = 'Operator';
                PRINT 'Deleted Operator role';
            ");

            // ============================================================================
            // STEP 4: Update role hierarchy levels (Staff=1, Receptionist=2, Admin=3)
            // ============================================================================
            migrationBuilder.Sql(@"
                UPDATE Roles SET HierarchyLevel = 1, DisplayOrder = 1 WHERE Name = 'Staff';
                UPDATE Roles SET HierarchyLevel = 2, DisplayOrder = 2 WHERE Name = 'Receptionist';
                UPDATE Roles SET HierarchyLevel = 3, DisplayOrder = 3 WHERE Name = 'Administrator';
                PRINT 'Updated role hierarchy levels';
            ");

            // ============================================================================
            // STEP 5: Delete obsolete Camera permissions (26 permissions)
            // ============================================================================
            migrationBuilder.Sql(@"
                -- Delete role-permission mappings for Camera permissions
                DELETE FROM RolePermissions
                WHERE PermissionId IN (
                    SELECT Id FROM Permissions WHERE Name LIKE 'Camera.%'
                );

                -- Delete Camera permissions
                DELETE FROM Permissions WHERE Name LIKE 'Camera.%';
                PRINT 'Deleted Camera permissions';
            ");

            // ============================================================================
            // STEP 6: Delete obsolete Alert permissions (will be merged into Notification)
            // ============================================================================
            migrationBuilder.Sql(@"
                -- Delete role-permission mappings for Alert permissions
                DELETE FROM RolePermissions
                WHERE PermissionId IN (
                    SELECT Id FROM Permissions WHERE Name LIKE 'Alert.%'
                );

                -- Delete Alert permissions
                DELETE FROM Permissions WHERE Name LIKE 'Alert.%';
                PRINT 'Deleted Alert permissions (merged into Notification)';
            ");

            // ============================================================================
            // STEP 7: Delete obsolete Manual permissions
            // ============================================================================
            migrationBuilder.Sql(@"
                -- Delete role-permission mappings
                DELETE FROM RolePermissions
                WHERE PermissionId IN (
                    SELECT Id FROM Permissions WHERE Name LIKE 'Manual.%'
                );

                -- Delete Manual permissions
                DELETE FROM Permissions WHERE Name LIKE 'Manual.%';
                PRINT 'Deleted Manual permissions (merged into CheckIn.Override)';
            ");

            // ============================================================================
            // STEP 8: Delete obsolete Integration permissions
            // ============================================================================
            migrationBuilder.Sql(@"
                DELETE FROM RolePermissions
                WHERE PermissionId IN (
                    SELECT Id FROM Permissions WHERE Name LIKE 'Integration.%'
                );

                DELETE FROM Permissions WHERE Name LIKE 'Integration.%';
                PRINT 'Deleted Integration permissions';
            ");

            // ============================================================================
            // STEP 9: Delete obsolete Sync permissions
            // ============================================================================
            migrationBuilder.Sql(@"
                DELETE FROM RolePermissions
                WHERE PermissionId IN (
                    SELECT Id FROM Permissions WHERE Name LIKE 'Sync.%'
                );

                DELETE FROM Permissions WHERE Name LIKE 'Sync.%';
                PRINT 'Deleted Sync permissions';
            ");

            // ============================================================================
            // STEP 10: Delete obsolete Offline permissions
            // ============================================================================
            migrationBuilder.Sql(@"
                DELETE FROM RolePermissions
                WHERE PermissionId IN (
                    SELECT Id FROM Permissions WHERE Name LIKE 'Offline.%'
                );

                DELETE FROM Permissions WHERE Name LIKE 'Offline.%';
                PRINT 'Deleted Offline permissions';
            ");

            // ============================================================================
            // STEP 11: Delete obsolete Configuration permissions (merged into SystemConfig)
            // ============================================================================
            migrationBuilder.Sql(@"
                DELETE FROM RolePermissions
                WHERE PermissionId IN (
                    SELECT Id FROM Permissions WHERE Name LIKE 'Configuration.%'
                );

                DELETE FROM Permissions WHERE Name LIKE 'Configuration.%';
                PRINT 'Deleted Configuration permissions (merged into SystemConfig)';
            ");

            // ============================================================================
            // STEP 12: Delete simplified Dashboard permissions (keep only 3)
            // ============================================================================
            migrationBuilder.Sql(@"
                -- Delete role-permission mappings for removed Dashboard permissions
                DELETE FROM RolePermissions
                WHERE PermissionId IN (
                    SELECT Id FROM Permissions
                    WHERE Name IN (
                        'Dashboard.View.Analytics',
                        'Dashboard.Customize',
                        'Dashboard.Export',
                        'Dashboard.ViewRealTime',
                        'Dashboard.ViewMetrics'
                    )
                );

                -- Delete removed Dashboard permissions
                DELETE FROM Permissions
                WHERE Name IN (
                    'Dashboard.View.Analytics',
                    'Dashboard.Customize',
                    'Dashboard.Export',
                    'Dashboard.ViewRealTime',
                    'Dashboard.ViewMetrics'
                );
                PRINT 'Deleted simplified Dashboard permissions';
            ");

            // ============================================================================
            // STEP 13: Delete simplified Report permissions (keep only 4)
            // ============================================================================
            migrationBuilder.Sql(@"
                DELETE FROM RolePermissions
                WHERE PermissionId IN (
                    SELECT Id FROM Permissions
                    WHERE Name IN (
                        'Report.Schedule',
                        'Report.CreateTemplates',
                        'Report.ManageSubscriptions',
                        'Report.ViewAnalytics',
                        'Report.ExportData',
                        'Report.ViewMetrics'
                    )
                );

                DELETE FROM Permissions
                WHERE Name IN (
                    'Report.Schedule',
                    'Report.CreateTemplates',
                    'Report.ManageSubscriptions',
                    'Report.ViewAnalytics',
                    'Report.ExportData',
                    'Report.ViewMetrics'
                );
                PRINT 'Deleted simplified Report permissions';
            ");

            // ============================================================================
            // STEP 14: Delete simplified Watchlist permissions (keep only 3)
            // ============================================================================
            migrationBuilder.Sql(@"
                DELETE FROM RolePermissions
                WHERE PermissionId IN (
                    SELECT Id FROM Permissions
                    WHERE Name IN (
                        'Watchlist.Create',
                        'Watchlist.Read.All',
                        'Watchlist.Update',
                        'Watchlist.Delete',
                        'Watchlist.Assign',
                        'Watchlist.Unassign',
                        'Watchlist.ViewAssignments',
                        'Watchlist.SyncWithFR',
                        'Watchlist.ViewSyncStatus'
                    )
                );

                DELETE FROM Permissions
                WHERE Name IN (
                    'Watchlist.Create',
                    'Watchlist.Read.All',
                    'Watchlist.Update',
                    'Watchlist.Delete',
                    'Watchlist.Assign',
                    'Watchlist.Unassign',
                    'Watchlist.ViewAssignments',
                    'Watchlist.SyncWithFR',
                    'Watchlist.ViewSyncStatus'
                );
                PRINT 'Deleted simplified Watchlist permissions';
            ");

            // ============================================================================
            // STEP 15: Delete simplified Badge permissions (keep only 2)
            // ============================================================================
            migrationBuilder.Sql(@"
                DELETE FROM RolePermissions
                WHERE PermissionId IN (
                    SELECT Id FROM Permissions
                    WHERE Name IN (
                        'Badge.Design',
                        'Badge.Configure',
                        'Badge.ViewQueue',
                        'Badge.ManageTemplates',
                        'Badge.ViewHistory'
                    )
                );

                DELETE FROM Permissions
                WHERE Name IN (
                    'Badge.Design',
                    'Badge.Configure',
                    'Badge.ViewQueue',
                    'Badge.ManageTemplates',
                    'Badge.ViewHistory'
                );
                PRINT 'Deleted simplified Badge permissions';
            ");

            // ============================================================================
            // STEP 16: Delete simplified SystemConfig permissions (keep only 2)
            // ============================================================================
            migrationBuilder.Sql(@"
                DELETE FROM RolePermissions
                WHERE PermissionId IN (
                    SELECT Id FROM Permissions
                    WHERE Name IN (
                        'SystemConfig.Create',
                        'SystemConfig.Delete',
                        'SystemConfig.ViewAll',
                        'SystemConfig.ManageIntegrations',
                        'SystemConfig.ManageNotifications',
                        'SystemConfig.ManageSecurity',
                        'SystemConfig.ManageCapacity',
                        'SystemConfig.ViewLogs',
                        'SystemConfig.Backup',
                        'SystemConfig.Restore'
                    )
                );

                DELETE FROM Permissions
                WHERE Name IN (
                    'SystemConfig.Create',
                    'SystemConfig.Delete',
                    'SystemConfig.ViewAll',
                    'SystemConfig.ManageIntegrations',
                    'SystemConfig.ManageNotifications',
                    'SystemConfig.ManageSecurity',
                    'SystemConfig.ManageCapacity',
                    'SystemConfig.ViewLogs',
                    'SystemConfig.Backup',
                    'SystemConfig.Restore'
                );
                PRINT 'Deleted simplified SystemConfig permissions';
            ");

            // ============================================================================
            // STEP 17: Add new consolidated Notification permissions (Receive, Acknowledge)
            // ============================================================================
            migrationBuilder.Sql(@"
                -- Add Notification.Receive if it doesn't exist
                IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'Notification.Receive')
                BEGIN
                    INSERT INTO Permissions (Name, DisplayName, Description, Category, RiskLevel, IsActive, CreatedAt)
                    VALUES (
                        'Notification.Receive',
                        'Receive Notifications & Alerts',
                        'Receive real-time notifications and alerts',
                        'Notification',
                        2,
                        1,
                        GETUTCDATE()
                    );
                END

                -- Add Notification.Acknowledge if it doesn't exist
                IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'Notification.Acknowledge')
                BEGIN
                    INSERT INTO Permissions (Name, DisplayName, Description, Category, RiskLevel, IsActive, CreatedAt)
                    VALUES (
                        'Notification.Acknowledge',
                        'Acknowledge Notifications',
                        'Mark notifications and alerts as acknowledged',
                        'Notification',
                        1,
                        1,
                        GETUTCDATE()
                    );
                END

                PRINT 'Added consolidated Notification permissions';
            ");

            // ============================================================================
            // STEP 18: Add new simplified Watchlist.View permission
            // ============================================================================
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM Permissions WHERE Name = 'Watchlist.View')
                BEGIN
                    INSERT INTO Permissions (Name, DisplayName, Description, Category, RiskLevel, IsActive, CreatedAt)
                    VALUES (
                        'Watchlist.View',
                        'View Watchlist',
                        'View blacklist and VIP lists',
                        'Watchlist',
                        2,
                        1,
                        GETUTCDATE()
                    );
                END

                PRINT 'Added Watchlist.View permission';
            ");

            // ============================================================================
            // STEP 19: Grant new permissions to Receptionist role
            // ============================================================================
            migrationBuilder.Sql(@"
                DECLARE @ReceptionistRoleId INT;
                DECLARE @AdminRoleId INT;

                SELECT @ReceptionistRoleId = Id FROM Roles WHERE Name = 'Receptionist';
                SELECT @AdminRoleId = Id FROM Roles WHERE Name = 'Administrator';

                -- Grant Notification.Receive to Receptionist
                IF NOT EXISTS (
                    SELECT 1 FROM RolePermissions rp
                    INNER JOIN Permissions p ON rp.PermissionId = p.Id
                    WHERE rp.RoleId = @ReceptionistRoleId AND p.Name = 'Notification.Receive'
                )
                BEGIN
                    INSERT INTO RolePermissions (RoleId, PermissionId, GrantedAt, GrantedBy)
                    SELECT @ReceptionistRoleId, Id, GETUTCDATE(), @AdminRoleId
                    FROM Permissions WHERE Name = 'Notification.Receive';
                END

                -- Grant Notification.Acknowledge to Receptionist
                IF NOT EXISTS (
                    SELECT 1 FROM RolePermissions rp
                    INNER JOIN Permissions p ON rp.PermissionId = p.Id
                    WHERE rp.RoleId = @ReceptionistRoleId AND p.Name = 'Notification.Acknowledge'
                )
                BEGIN
                    INSERT INTO RolePermissions (RoleId, PermissionId, GrantedAt, GrantedBy)
                    SELECT @ReceptionistRoleId, Id, GETUTCDATE(), @AdminRoleId
                    FROM Permissions WHERE Name = 'Notification.Acknowledge';
                END

                -- Grant CheckIn.Override to Receptionist
                IF NOT EXISTS (
                    SELECT 1 FROM RolePermissions rp
                    INNER JOIN Permissions p ON rp.PermissionId = p.Id
                    WHERE rp.RoleId = @ReceptionistRoleId AND p.Name = 'CheckIn.Override'
                )
                BEGIN
                    INSERT INTO RolePermissions (RoleId, PermissionId, GrantedAt, GrantedBy)
                    SELECT @ReceptionistRoleId, Id, GETUTCDATE(), @AdminRoleId
                    FROM Permissions WHERE Name = 'CheckIn.Override';
                END

                -- Grant CheckIn.ManualVerification to Receptionist
                IF NOT EXISTS (
                    SELECT 1 FROM RolePermissions rp
                    INNER JOIN Permissions p ON rp.PermissionId = p.Id
                    WHERE rp.RoleId = @ReceptionistRoleId AND p.Name = 'CheckIn.ManualVerification'
                )
                BEGIN
                    INSERT INTO RolePermissions (RoleId, PermissionId, GrantedAt, GrantedBy)
                    SELECT @ReceptionistRoleId, Id, GETUTCDATE(), @AdminRoleId
                    FROM Permissions WHERE Name = 'CheckIn.ManualVerification';
                END

                -- Grant Profile.ChangePassword to Receptionist
                IF NOT EXISTS (
                    SELECT 1 FROM RolePermissions rp
                    INNER JOIN Permissions p ON rp.PermissionId = p.Id
                    WHERE rp.RoleId = @ReceptionistRoleId AND p.Name = 'Profile.ChangePassword'
                )
                BEGIN
                    INSERT INTO RolePermissions (RoleId, PermissionId, GrantedAt, GrantedBy)
                    SELECT @ReceptionistRoleId, Id, GETUTCDATE(), @AdminRoleId
                    FROM Permissions WHERE Name = 'Profile.ChangePassword';
                END

                PRINT 'Granted new permissions to Receptionist role';
            ");

            // ============================================================================
            // STEP 20: Grant ALL new permissions to Administrator (they get everything)
            // ============================================================================
            migrationBuilder.Sql(@"
                DECLARE @AdminRoleId INT;
                SELECT @AdminRoleId = Id FROM Roles WHERE Name = 'Administrator';

                -- Grant ALL missing permissions to Administrator
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

                PRINT 'Granted all permissions to Administrator role';
            ");

            // ============================================================================
            // SUMMARY
            // ============================================================================
            migrationBuilder.Sql(@"
                DECLARE @RoleCount INT;
                DECLARE @PermissionCount INT;
                DECLARE @RolePermissionCount INT;

                SELECT @RoleCount = COUNT(*) FROM Roles;
                SELECT @PermissionCount = COUNT(*) FROM Permissions WHERE IsActive = 1;
                SELECT @RolePermissionCount = COUNT(*) FROM RolePermissions;

                PRINT '========================================';
                PRINT 'PHASE 0 CLEANUP COMPLETE';
                PRINT '========================================';
                PRINT CONCAT('Total Roles: ', @RoleCount);
                PRINT CONCAT('Total Active Permissions: ', @PermissionCount);
                PRINT CONCAT('Total Role-Permission Mappings: ', @RolePermissionCount);
                PRINT '========================================';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This migration is NOT reversible due to data loss (deleted permissions, merged roles)
            // Attempting to reverse would require:
            // 1. Recreating all deleted permissions
            // 2. Recreating Operator role
            // 3. Determining which users were originally Operator vs Receptionist
            // 4. Restoring old role-permission mappings
            //
            // In practice, this migration should not be reversed.
            // If necessary, restore from database backup before migration.

            migrationBuilder.Sql(@"
                RAISERROR('Phase 0 cleanup migration is not reversible. Restore from database backup if needed.', 16, 1);
            ");
        }
    }
}
