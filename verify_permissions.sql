-- Check if Administrator role has Role.* permissions
SELECT 
    r.Name AS RoleName,
    p.Name AS PermissionName,
    rp.GrantedAt
FROM RolePermissions rp
INNER JOIN Roles r ON rp.RoleId = r.Id
INNER JOIN Permissions p ON rp.PermissionId = p.Id
WHERE r.Name = 'Administrator' 
AND p.Name LIKE 'Role.%'
ORDER BY p.Name;

-- Count total permissions for Administrator
SELECT 
    r.Name AS RoleName,
    COUNT(*) AS TotalPermissions
FROM RolePermissions rp
INNER JOIN Roles r ON rp.RoleId = r.Id
WHERE r.Name = 'Administrator'
GROUP BY r.Name;
