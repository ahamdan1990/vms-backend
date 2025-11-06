-- ============================================================================
-- Migration: Add Visit Purpose and Location Granular Permissions
-- Date: 2025-01-06
-- Description: Adds new granular permissions for Visit Purpose and Location
--              management, and assigns them to Staff and Receptionist roles
-- ============================================================================

BEGIN TRANSACTION;

-- ============================================================================
-- Step 1: Add new Visit Purpose permissions
-- ============================================================================

INSERT INTO Permissions (Name, Description, Category, DisplayName, DisplayOrder, IsActive, IsSystemPermission, CreatedAt, RiskLevel)
VALUES
    ('VisitPurpose.Read', 'Allows user to read visit purposes in the system', 'VisitPurpose', 'Read - VisitPurpose', 1000, 1, 1, GETUTCDATE(), 1),
    ('VisitPurpose.Read.All', 'Allows user to read all visit purposes in the system', 'VisitPurpose', 'Read All - VisitPurpose', 1001, 1, 1, GETUTCDATE(), 1),
    ('VisitPurpose.Create', 'Allows user to create visit purposes in the system', 'VisitPurpose', 'Create - VisitPurpose', 1002, 1, 1, GETUTCDATE(), 3),
    ('VisitPurpose.Update', 'Allows user to update visit purposes in the system', 'VisitPurpose', 'Update - VisitPurpose', 1003, 1, 1, GETUTCDATE(), 3),
    ('VisitPurpose.Delete', 'Allows user to delete visit purposes in the system', 'VisitPurpose', 'Delete - VisitPurpose', 1004, 1, 1, GETUTCDATE(), 3);

-- ============================================================================
-- Step 2: Add new Location permissions
-- ============================================================================

INSERT INTO Permissions (Name, Description, Category, DisplayName, DisplayOrder, IsActive, IsSystemPermission, CreatedAt, RiskLevel)
VALUES
    ('Location.Read', 'Allows user to read locations in the system', 'Location', 'Read - Location', 1005, 1, 1, GETUTCDATE(), 1),
    ('Location.Read.All', 'Allows user to read all locations in the system', 'Location', 'Read All - Location', 1006, 1, 1, GETUTCDATE(), 1),
    ('Location.Create', 'Allows user to create locations in the system', 'Location', 'Create - Location', 1007, 1, 1, GETUTCDATE(), 3),
    ('Location.Update', 'Allows user to update locations in the system', 'Location', 'Update - Location', 1008, 1, 1, GETUTCDATE(), 3),
    ('Location.Delete', 'Allows user to delete locations in the system', 'Location', 'Delete - Location', 1009, 1, 1, GETUTCDATE(), 3);

-- ============================================================================
-- Step 3: Add new permissions to Staff role
-- ============================================================================

DECLARE @StaffRoleId INT = (SELECT Id FROM Roles WHERE Name = 'Staff' AND IsDeleted = 0);

IF @StaffRoleId IS NOT NULL
BEGIN
    -- Add VisitPurpose.Read permission to Staff
    IF NOT EXISTS (
        SELECT 1 FROM RolePermissions rp
        INNER JOIN Permissions p ON rp.PermissionId = p.Id
        WHERE rp.RoleId = @StaffRoleId AND p.Name = 'VisitPurpose.Read'
    )
    BEGIN
        INSERT INTO RolePermissions (RoleId, PermissionId, GrantedAt)
        SELECT @StaffRoleId, Id, GETUTCDATE()
        FROM Permissions WHERE Name = 'VisitPurpose.Read';
    END

    -- Add Location.Read permission to Staff
    IF NOT EXISTS (
        SELECT 1 FROM RolePermissions rp
        INNER JOIN Permissions p ON rp.PermissionId = p.Id
        WHERE rp.RoleId = @StaffRoleId AND p.Name = 'Location.Read'
    )
    BEGIN
        INSERT INTO RolePermissions (RoleId, PermissionId, GrantedAt)
        SELECT @StaffRoleId, Id, GETUTCDATE()
        FROM Permissions WHERE Name = 'Location.Read';
    END

    -- Add Profile.UploadPhoto permission to Staff (if not exists)
    IF NOT EXISTS (
        SELECT 1 FROM RolePermissions rp
        INNER JOIN Permissions p ON rp.PermissionId = p.Id
        WHERE rp.RoleId = @StaffRoleId AND p.Name = 'Profile.UploadPhoto'
    )
    BEGIN
        IF EXISTS (SELECT 1 FROM Permissions WHERE Name = 'Profile.UploadPhoto')
        BEGIN
            INSERT INTO RolePermissions (RoleId, PermissionId, GrantedAt)
            SELECT @StaffRoleId, Id, GETUTCDATE()
            FROM Permissions WHERE Name = 'Profile.UploadPhoto';
        END
    END

    -- Add Visitor.ReadOwn permission to Staff (if not exists)
    IF NOT EXISTS (
        SELECT 1 FROM RolePermissions rp
        INNER JOIN Permissions p ON rp.PermissionId = p.Id
        WHERE rp.RoleId = @StaffRoleId AND p.Name = 'Visitor.ReadOwn'
    )
    BEGIN
        IF EXISTS (SELECT 1 FROM Permissions WHERE Name = 'Visitor.ReadOwn')
        BEGIN
            INSERT INTO RolePermissions (RoleId, PermissionId, GrantedAt)
            SELECT @StaffRoleId, Id, GETUTCDATE()
            FROM Permissions WHERE Name = 'Visitor.ReadOwn';
        END
    END

    -- Add Visitor.Search permission to Staff (if not exists)
    IF NOT EXISTS (
        SELECT 1 FROM RolePermissions rp
        INNER JOIN Permissions p ON rp.PermissionId = p.Id
        WHERE rp.RoleId = @StaffRoleId AND p.Name = 'Visitor.Search'
    )
    BEGIN
        IF EXISTS (SELECT 1 FROM Permissions WHERE Name = 'Visitor.Search')
        BEGIN
            INSERT INTO RolePermissions (RoleId, PermissionId, GrantedAt)
            SELECT @StaffRoleId, Id, GETUTCDATE()
            FROM Permissions WHERE Name = 'Visitor.Search';
        END
    END

    -- Add Visitor.ManagePhotos permission to Staff (if not exists)
    IF NOT EXISTS (
        SELECT 1 FROM RolePermissions rp
        INNER JOIN Permissions p ON rp.PermissionId = p.Id
        WHERE rp.RoleId = @StaffRoleId AND p.Name = 'Visitor.ManagePhotos'
    )
    BEGIN
        IF EXISTS (SELECT 1 FROM Permissions WHERE Name = 'Visitor.ManagePhotos')
        BEGIN
            INSERT INTO RolePermissions (RoleId, PermissionId, GrantedAt)
            SELECT @StaffRoleId, Id, GETUTCDATE()
            FROM Permissions WHERE Name = 'Visitor.ManagePhotos';
        END
    END

    -- Add document permissions for Staff
    DECLARE @StaffDocumentPermissions TABLE (PermissionName NVARCHAR(255));
    INSERT INTO @StaffDocumentPermissions VALUES
        ('VisitorDocument.Create'),
        ('VisitorDocument.Read'),
        ('VisitorDocument.Update'),
        ('VisitorDocument.Upload'),
        ('VisitorDocument.Download'),
        ('VisitorNote.Create'),
        ('VisitorNote.Read'),
        ('VisitorNote.Update'),
        ('EmergencyContact.Create'),
        ('EmergencyContact.Read'),
        ('EmergencyContact.Update'),
        ('Notification.Receive'),
        ('Notification.Acknowledge'),
        ('Template.Download.Single'),
        ('User.ViewActivity'),
        ('Invitation.Read'),
        ('Invitation.ViewHistory');

    INSERT INTO RolePermissions (RoleId, PermissionId, GrantedAt)
    SELECT @StaffRoleId, p.Id, GETUTCDATE()
    FROM Permissions p
    INNER JOIN @StaffDocumentPermissions sdp ON p.Name = sdp.PermissionName
    WHERE NOT EXISTS (
        SELECT 1 FROM RolePermissions rp
        WHERE rp.RoleId = @StaffRoleId AND rp.PermissionId = p.Id
    );
END

-- ============================================================================
-- Step 4: Add new permissions to Receptionist role
-- ============================================================================

DECLARE @ReceptionistRoleId INT = (SELECT Id FROM Roles WHERE Name = 'Receptionist' AND IsDeleted = 0);

IF @ReceptionistRoleId IS NOT NULL
BEGIN
    -- Add VisitPurpose.Read permission to Receptionist
    IF NOT EXISTS (
        SELECT 1 FROM RolePermissions rp
        INNER JOIN Permissions p ON rp.PermissionId = p.Id
        WHERE rp.RoleId = @ReceptionistRoleId AND p.Name = 'VisitPurpose.Read'
    )
    BEGIN
        INSERT INTO RolePermissions (RoleId, PermissionId, GrantedAt)
        SELECT @ReceptionistRoleId, Id, GETUTCDATE()
        FROM Permissions WHERE Name = 'VisitPurpose.Read';
    END

    -- Add Location.Read permission to Receptionist
    IF NOT EXISTS (
        SELECT 1 FROM RolePermissions rp
        INNER JOIN Permissions p ON rp.PermissionId = p.Id
        WHERE rp.RoleId = @ReceptionistRoleId AND p.Name = 'Location.Read'
    )
    BEGIN
        INSERT INTO RolePermissions (RoleId, PermissionId, GrantedAt)
        SELECT @ReceptionistRoleId, Id, GETUTCDATE()
        FROM Permissions WHERE Name = 'Location.Read';
    END

    -- Add document and additional permissions for Receptionist
    DECLARE @ReceptionistDocumentPermissions TABLE (PermissionName NVARCHAR(255));
    INSERT INTO @ReceptionistDocumentPermissions VALUES
        ('VisitorDocument.Create'),
        ('VisitorDocument.Read'),
        ('VisitorDocument.Update'),
        ('VisitorDocument.Upload'),
        ('VisitorDocument.Download'),
        ('VisitorNote.Create'),
        ('VisitorNote.Read'),
        ('VisitorNote.Update'),
        ('EmergencyContact.Create'),
        ('EmergencyContact.Read'),
        ('EmergencyContact.Update'),
        ('Visitor.Search'),
        ('Visitor.ManagePhotos'),
        ('CheckIn.ManualEntry'),
        ('CheckIn.PhotoCapture'),
        ('WalkIn.FRSync'),
        ('Invitation.ReadOwn'),
        ('Invitation.Read'),
        ('Emergency.GenerateRoster'),
        ('User.ViewActivity'),
        ('Audit.Read.All');

    INSERT INTO RolePermissions (RoleId, PermissionId, GrantedAt)
    SELECT @ReceptionistRoleId, p.Id, GETUTCDATE()
    FROM Permissions p
    INNER JOIN @ReceptionistDocumentPermissions rdp ON p.Name = rdp.PermissionName
    WHERE NOT EXISTS (
        SELECT 1 FROM RolePermissions rp
        WHERE rp.RoleId = @ReceptionistRoleId AND rp.PermissionId = p.Id
    );
END

-- ============================================================================
-- Step 5: Verification - Print summary of changes
-- ============================================================================

PRINT '============================================================================';
PRINT 'Migration Summary';
PRINT '============================================================================';

PRINT 'New Visit Purpose Permissions:';
SELECT Name, Category, RiskLevel FROM Permissions WHERE Name LIKE 'VisitPurpose.%';

PRINT 'New Location Permissions:';
SELECT Name, Category, RiskLevel FROM Permissions WHERE Name LIKE 'Location.%';

PRINT 'Staff Role Permissions Count:';
SELECT COUNT(*) AS PermissionCount FROM RolePermissions
WHERE RoleId = (SELECT Id FROM Roles WHERE Name = 'Staff' AND IsDeleted = 0);

PRINT 'Receptionist Role Permissions Count:';
SELECT COUNT(*) AS PermissionCount FROM RolePermissions
WHERE RoleId = (SELECT Id FROM Roles WHERE Name = 'Receptionist' AND IsDeleted = 0);

PRINT '============================================================================';

-- Commit the transaction
COMMIT TRANSACTION;

PRINT 'Migration completed successfully!';
GO
