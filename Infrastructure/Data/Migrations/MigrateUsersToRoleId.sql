-- ============================================================================
-- Migration Script: Set RoleId for Existing Users
-- Purpose: Migrate users from enum-based Role to database RoleId
-- Run this script if users are missing RoleId and getting hardcoded permissions
-- ============================================================================

-- Check current status before migration
PRINT '=== BEFORE MIGRATION ==='
SELECT
    COUNT(*) AS TotalActiveUsers,
    COUNT(RoleId) AS UsersWithRoleId,
    COUNT(*) - COUNT(RoleId) AS UsersWithoutRoleId
FROM Users
WHERE IsDeleted = 0;

-- Show users without RoleId
PRINT ''
PRINT 'Users without RoleId (will be migrated):'
SELECT
    Id,
    Email,
    CASE Role
        WHEN 0 THEN 'Staff'
        WHEN 1 THEN 'Receptionist'
        WHEN 2 THEN 'Administrator'
        ELSE 'Unknown'
    END AS CurrentRole,
    RoleId
FROM Users
WHERE RoleId IS NULL AND IsDeleted = 0;

-- Perform migration
PRINT ''
PRINT '=== EXECUTING MIGRATION ==='

-- Update Staff users (Role = 0)
UPDATE Users
SET RoleId = (SELECT Id FROM Roles WHERE Name = 'Staff')
WHERE RoleId IS NULL
  AND Role = 0
  AND IsDeleted = 0;
PRINT 'Updated Staff users: ' + CAST(@@ROWCOUNT AS VARCHAR(10))

-- Update Receptionist users (Role = 1)
UPDATE Users
SET RoleId = (SELECT Id FROM Roles WHERE Name = 'Receptionist')
WHERE RoleId IS NULL
  AND Role = 1
  AND IsDeleted = 0;
PRINT 'Updated Receptionist users: ' + CAST(@@ROWCOUNT AS VARCHAR(10))

-- Update Administrator users (Role = 2)
UPDATE Users
SET RoleId = (SELECT Id FROM Roles WHERE Name = 'Administrator')
WHERE RoleId IS NULL
  AND Role = 2
  AND IsDeleted = 0;
PRINT 'Updated Administrator users: ' + CAST(@@ROWCOUNT AS VARCHAR(10))

-- Default any remaining users without RoleId to Staff role
UPDATE Users
SET RoleId = (SELECT Id FROM Roles WHERE Name = 'Staff')
WHERE RoleId IS NULL
  AND IsDeleted = 0;
PRINT 'Updated remaining users (defaulted to Staff): ' + CAST(@@ROWCOUNT AS VARCHAR(10))

-- Verify migration results
PRINT ''
PRINT '=== AFTER MIGRATION ==='
SELECT
    COUNT(*) AS TotalActiveUsers,
    COUNT(RoleId) AS UsersWithRoleId,
    COUNT(*) - COUNT(RoleId) AS UsersWithoutRoleId
FROM Users
WHERE IsDeleted = 0;

-- Show role distribution after migration
PRINT ''
PRINT 'User distribution by role:'
SELECT
    r.Name AS RoleName,
    COUNT(u.Id) AS UserCount
FROM Users u
INNER JOIN Roles r ON u.RoleId = r.Id
WHERE u.IsDeleted = 0
GROUP BY r.Name
ORDER BY r.HierarchyLevel;

PRINT ''
PRINT '=== MIGRATION COMPLETE ==='
PRINT 'All active users now have RoleId set and will use database permissions.'
