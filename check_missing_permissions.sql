-- Check which permissions Administrator role is missing
DECLARE @AdminRoleId INT;
SELECT @AdminRoleId = Id FROM Roles WHERE Name = 'Administrator';

-- Show permissions that exist but are NOT assigned to Administrator
SELECT
    p.Id,
    p.Name,
    p.Category
FROM Permissions p
WHERE p.IsActive = 1
AND NOT EXISTS (
    SELECT 1
    FROM RolePermissions rp
    WHERE rp.RoleId = @AdminRoleId
    AND rp.PermissionId = p.Id
)
ORDER BY p.Category, p.Name;

-- Count total permissions
SELECT
    'Total Permissions' AS [Type],
    COUNT(*) AS [Count]
FROM Permissions
WHERE IsActive = 1

UNION ALL

SELECT
    'Admin Permissions' AS [Type],
    COUNT(*) AS [Count]
FROM RolePermissions rp
INNER JOIN Permissions p ON rp.PermissionId = p.Id
WHERE rp.RoleId = @AdminRoleId AND p.IsActive = 1

UNION ALL

SELECT
    'Missing Permissions' AS [Type],
    COUNT(*) AS [Count]
FROM Permissions p
WHERE p.IsActive = 1
AND NOT EXISTS (
    SELECT 1
    FROM RolePermissions rp
    WHERE rp.RoleId = @AdminRoleId
    AND rp.PermissionId = p.Id
);

-- Check if Role.* permissions exist
SELECT
    p.Id,
    p.Name,
    CASE
        WHEN EXISTS (
            SELECT 1 FROM RolePermissions rp
            WHERE rp.RoleId = @AdminRoleId AND rp.PermissionId = p.Id
        ) THEN 'Assigned'
        ELSE 'Missing'
    END AS Status
FROM Permissions p
WHERE p.Name LIKE 'Role.%'
ORDER BY p.Name;
