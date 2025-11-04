-- Grant all Role.* permissions to Administrator role

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

-- Show results
SELECT 'Granted Role permissions to Administrator role';
SELECT COUNT(*) AS 'New Permissions Granted' 
FROM RolePermissions rp
INNER JOIN Permissions p ON rp.PermissionId = p.Id
WHERE rp.RoleId = @AdminRoleId AND p.Name LIKE 'Role.%';
