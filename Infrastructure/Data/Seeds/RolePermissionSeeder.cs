using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Seeds;

/// <summary>
/// Seeder for role-permission mappings
/// </summary>
public static class RolePermissionSeeder
{
    /// <summary>
    /// Seeds role-permission mappings to the database (FIRST-TIME ONLY to preserve custom configurations)
    /// </summary>
    /// <remarks>
    /// ⚠️ IMPORTANT: This seeder will ONLY run on first-time setup when NO role permissions exist.
    /// Once role permissions have been seeded, this will skip to preserve any custom configurations
    /// made by administrators. To force re-seed (dev/test only), manually delete all RolePermissions.
    /// </remarks>
    public static async Task SeedRolePermissionsAsync(ApplicationDbContext context)
    {
        Console.WriteLine("Checking role-permission mappings...");

        // ✅ FIX #4: Check if ANY role permissions exist (not just Administrator)
        // If they exist, skip seeding to preserve custom configurations
        var existingRolePermissionsCount = await context.RolePermissions.CountAsync();

        if (existingRolePermissionsCount > 0)
        {
            Console.WriteLine($"✅ Role permissions already seeded ({existingRolePermissionsCount} mappings exist).");
            Console.WriteLine("   Skipping seed to preserve custom configurations.");
            Console.WriteLine("   Applying additive sync for new system permissions only.");
            await EnsureBuiltInRoleCompanyPermissionsAsync(context);
            await EnsureNewSystemPermissionsAsync(context);
            await EnsureCivilDefenseRolePermissionsAsync(context);
            return;
        }

        Console.WriteLine("🆕 First-time seeding: Creating initial role-permission mappings...");
        Console.WriteLine("   (Subsequent runs will skip to preserve custom configurations)");

        // Get system roles from database
        var staffRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Staff");
        var receptionistRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Receptionist");
        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Administrator");
        var cdReceptionistRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "CivilDefense_Receptionist");
        var cdManagerRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "CivilDefense_Manager");

        if (staffRole == null || receptionistRole == null || adminRole == null)
        {
            throw new InvalidOperationException("System roles must be seeded before role permissions.");
        }

        // Get all permissions from database
        var allPermissions = await context.Permissions.ToDictionaryAsync(p => p.Name, p => p.Id);

        var rolePermissions = new List<RolePermission>();

        // Staff (Host) Permissions - Own invitations and mutual visitors only
        var staffPermissions = new[]
        {
            // Profile management
            Permissions.Profile.ViewOwn,
            Permissions.Profile.UpdateOwn,
            Permissions.Profile.ChangePassword,
            Permissions.Profile.ManagePreferences,
            Permissions.Profile.ViewActivity,
            Permissions.Profile.UploadPhoto,

            // Own invitation management
            Permissions.Invitation.CreateOwn,
            Permissions.Invitation.ReadOwn,
            Permissions.Invitation.UpdateOwn,
            Permissions.Invitation.CancelOwn,
            Permissions.Invitation.Read,
            Permissions.Invitation.ViewHistory,

            // Visitor management (own and mutual visitors)
            Permissions.Visitor.Create,
            Permissions.Visitor.Read,
            Permissions.Visitor.ReadOwn,
            Permissions.Visitor.Update,
            Permissions.Visitor.ViewStatistics,
            Permissions.Visitor.Search,
            Permissions.Visitor.ManagePhotos,

            // Company lookup
            Permissions.Company.Read,

            // Visitor documents (for invitation-related documents)
            Permissions.VisitorDocument.Create,
            Permissions.VisitorDocument.Read,
            Permissions.VisitorDocument.Update,
            Permissions.VisitorDocument.Upload,
            Permissions.VisitorDocument.Download,

            // Visitor notes (for tracking visitor information)
            Permissions.VisitorNote.Create,
            Permissions.VisitorNote.Read,
            Permissions.VisitorNote.Update,

            // Emergency contacts (for visitor safety)
            Permissions.EmergencyContact.Create,
            Permissions.EmergencyContact.Read,
            Permissions.EmergencyContact.Update,

            // Visit purposes (read-only for creating invitations)
            Permissions.VisitPurpose.Read,

            // Locations (read-only for creating invitations)
            Permissions.Location.Read,

            // Calendar viewing
            Permissions.Calendar.ViewOwn,
            Permissions.Calendar.ViewAvailability,

            // Notifications
            Permissions.Notification.ReadOwn,
            Permissions.Notification.Receive,
            Permissions.Notification.Acknowledge,

            // Dashboard
            Permissions.Dashboard.ViewBasic,

            // Template downloads
            Permissions.Template.DownloadSingle,

            // User activity
            Permissions.User.ViewActivity
        };

        // Receptionist Permissions - Check-in/out, walk-ins, view all invitations (including pending)
        var receptionistPermissions = new[]
        {
            // Profile management
            Permissions.Profile.ViewOwn,
            Permissions.Profile.UpdateOwn,
            Permissions.Profile.ChangePassword,

            // Check-in/Check-out operations
            Permissions.CheckIn.Process,
            Permissions.CheckIn.ProcessOut,
            Permissions.CheckIn.ViewQueue,
            Permissions.CheckIn.ViewHistory,
            Permissions.CheckIn.PrintBadge,
            Permissions.CheckIn.QRScan,
            Permissions.CheckIn.Override,
            Permissions.CheckIn.ManualVerification,
            Permissions.CheckIn.ManualEntry,
            Permissions.CheckIn.PhotoCapture,

            // Walk-in registration
            Permissions.WalkIn.Register,
            Permissions.WalkIn.QuickRegister,
            Permissions.WalkIn.CheckIn,
            Permissions.WalkIn.ViewList,
            Permissions.WalkIn.ViewHistory,
            Permissions.WalkIn.FRSync,

            // View all invitations (read-only, including PENDING)
            Permissions.Invitation.ReadAll,
            Permissions.Invitation.ViewPending,
            Permissions.Invitation.ViewHistory,
            Permissions.Invitation.ReadOwn,
            Permissions.Invitation.Read,

            // View all visitors
            Permissions.Visitor.ReadAll,
            Permissions.Visitor.ReadToday,
            Permissions.Visitor.ViewHistory,
            Permissions.Visitor.Create,
            Permissions.Visitor.Update,
            Permissions.Visitor.Read,
            Permissions.Visitor.Search,
            Permissions.Visitor.ViewStatistics,
            Permissions.Visitor.ManagePhotos,

            // Company lookup and quick creation during walk-in registration
            Permissions.Company.Read,
            Permissions.Company.Create,

            // Visitor documents (for walk-in registration and document verification)
            Permissions.VisitorDocument.Create,
            Permissions.VisitorDocument.Read,
            Permissions.VisitorDocument.Update,
            Permissions.VisitorDocument.Upload,
            Permissions.VisitorDocument.Download,

            // Visitor notes
            Permissions.VisitorNote.Create,
            Permissions.VisitorNote.Read,
            Permissions.VisitorNote.Update,

            // Emergency contacts
            Permissions.EmergencyContact.Create,
            Permissions.EmergencyContact.Read,
            Permissions.EmergencyContact.Update,

            // Visit purposes (read-only for viewing invitation details)
            Permissions.VisitPurpose.Read,

            // Locations (read-only for check-in operations)
            Permissions.Location.Read,

            // QR Code operations
            Permissions.QRCode.Scan,
            Permissions.QRCode.Validate,
            Permissions.QRCode.ViewHistory,

            // Badge printing
            Permissions.Badge.Print,
            Permissions.Badge.ReprintLost,

            // Notifications & Alerts (consolidated)
            Permissions.Notification.ReadOwn,
            Permissions.Notification.ReadAll,
            Permissions.Notification.Receive,
            Permissions.Notification.Acknowledge,

            // Dashboard
            Permissions.Dashboard.ViewBasic,
            Permissions.Dashboard.ViewOperations,

            // Calendar
            Permissions.Calendar.ViewAll,
            Permissions.Calendar.ViewAvailability,

            // Emergency operations
            Permissions.Emergency.Export,
            Permissions.Emergency.GenerateRoster,
            Permissions.Emergency.ViewRoster,
            Permissions.Emergency.PrintRoster,

            // System config read (for general system settings)
            Permissions.SystemConfig.Read,

            // User activity
            Permissions.User.ViewActivity,

            // Limited audit access
            Permissions.Audit.ReadAll
        };

        // Administrator Permissions - ALL permissions
        var adminPermissions = allPermissions.Keys.ToArray();

        // Create role permission mappings for Staff
        foreach (var permissionName in staffPermissions)
        {
            if (allPermissions.TryGetValue(permissionName, out var permissionId))
            {
                rolePermissions.Add(new RolePermission
                {
                    RoleId = staffRole.Id,
                    PermissionId = permissionId,
                    GrantedAt = DateTime.UtcNow,
                    GrantedBy = null // System seeded, no specific user
                });
            }
            else
            {
                Console.WriteLine($"Warning: Permission '{permissionName}' not found for Staff role.");
            }
        }

        // Create role permission mappings for Receptionist
        foreach (var permissionName in receptionistPermissions)
        {
            if (allPermissions.TryGetValue(permissionName, out var permissionId))
            {
                rolePermissions.Add(new RolePermission
                {
                    RoleId = receptionistRole.Id,
                    PermissionId = permissionId,
                    GrantedAt = DateTime.UtcNow,
                    GrantedBy = null // System seeded, no specific user
                });
            }
            else
            {
                Console.WriteLine($"Warning: Permission '{permissionName}' not found for Receptionist role.");
            }
        }

        // ✅ FIX #4: Create role permission mappings for Administrator (ALL permissions on first-time seed)
        // NOTE: This grants ALL permissions ONLY on first-time setup. Administrators can later customize
        // permissions via the Role edit form, and those changes will NOT be overwritten on subsequent runs.
        foreach (var permissionName in adminPermissions)
        {
            if (allPermissions.TryGetValue(permissionName, out var permissionId))
            {
                rolePermissions.Add(new RolePermission
                {
                    RoleId = adminRole.Id,
                    PermissionId = permissionId,
                    GrantedAt = DateTime.UtcNow,
                    GrantedBy = null // System seeded, no specific user
                });
            }
        }

        // Civil Defense Receptionist permissions
        var cdReceptionistPermissions = new[]
        {
            Permissions.CivilDefense.ViewPresence,
            Permissions.CivilDefense.CheckInStaff,
            Permissions.CivilDefense.CheckOutStaff,
            Permissions.CivilDefense.RegisterVisitor,
            Permissions.CivilDefense.CheckOutVisitor,
            Permissions.Profile.ViewOwn,
            Permissions.Profile.UpdateOwn,
            Permissions.Profile.ChangePassword,
            Permissions.Notification.ReadOwn,
            Permissions.Notification.Receive,
            Permissions.Notification.Acknowledge
        };

        // Civil Defense Manager permissions
        var cdManagerPermissions = new[]
        {
            Permissions.CivilDefense.ViewPresence,
            Permissions.CivilDefense.ViewReports,
            Permissions.CivilDefense.ExportReports,
            Permissions.Profile.ViewOwn,
            Permissions.Profile.UpdateOwn,
            Permissions.Profile.ChangePassword,
            Permissions.Notification.ReadOwn,
            Permissions.Notification.Receive,
            Permissions.Notification.Acknowledge
        };

        if (cdReceptionistRole != null)
        {
            foreach (var permissionName in cdReceptionistPermissions)
            {
                if (allPermissions.TryGetValue(permissionName, out var permissionId))
                {
                    rolePermissions.Add(new RolePermission
                    {
                        RoleId = cdReceptionistRole.Id,
                        PermissionId = permissionId,
                        GrantedAt = DateTime.UtcNow,
                        GrantedBy = null
                    });
                }
            }
        }

        if (cdManagerRole != null)
        {
            foreach (var permissionName in cdManagerPermissions)
            {
                if (allPermissions.TryGetValue(permissionName, out var permissionId))
                {
                    rolePermissions.Add(new RolePermission
                    {
                        RoleId = cdManagerRole.Id,
                        PermissionId = permissionId,
                        GrantedAt = DateTime.UtcNow,
                        GrantedBy = null
                    });
                }
            }
        }

        await context.RolePermissions.AddRangeAsync(rolePermissions);
        await context.SaveChangesAsync();

        Console.WriteLine($"✅ Successfully seeded {rolePermissions.Count} role-permission mappings.");
        Console.WriteLine($"   📋 Staff: {staffPermissions.Length} permissions");
        Console.WriteLine($"   📋 Receptionist: {receptionistPermissions.Length} permissions");
        Console.WriteLine($"   📋 Administrator: {adminPermissions.Length} permissions (ALL - initial setup)");
        Console.WriteLine($"   💡 TIP: Use the Role management UI to customize permissions. Changes persist across restarts.");

        // Verify Administrator has all permissions
        var verifyAdminCount = await context.RolePermissions.CountAsync(rp => rp.RoleId == adminRole.Id);
        var verifyTotalCount = await context.Permissions.CountAsync(p => p.IsActive);

        if (verifyAdminCount == verifyTotalCount)
        {
            Console.WriteLine($"✅ Verification passed: Administrator has all {verifyTotalCount} active permissions (initial setup).");
        }
        else
        {
            Console.WriteLine($"⚠️ Verification warning: Administrator has {verifyAdminCount}/{verifyTotalCount} permissions.");
        }
    }

    /// <summary>
    /// Gets recommended permissions for a specific role
    /// </summary>
    public static List<string> GetRecommendedPermissionsForRole(string roleName)
    {
        return roleName switch
        {
            "Staff" => GetStaffPermissions(),
            "Receptionist" => GetReceptionistPermissions(),
            "Administrator" => Permissions.GetAllPermissions(),
            _ => new List<string>()
        };
    }

    /// <summary>
    /// Gets Staff role permissions
    /// </summary>
    private static List<string> GetStaffPermissions()
    {
        return new List<string>
        {
            Permissions.Profile.ViewOwn,
            Permissions.Profile.UpdateOwn,
            Permissions.Invitation.CreateOwn,
            Permissions.Invitation.ReadOwn,
            Permissions.Invitation.UpdateOwn,
            Permissions.Invitation.CancelOwn,
            Permissions.Visitor.Create,
            Permissions.Visitor.Read,
            Permissions.Visitor.Update,
            Permissions.Company.Read,
            Permissions.Calendar.ViewOwn,
            Permissions.Calendar.ViewAvailability,
            Permissions.Notification.ReadOwn,
            Permissions.Dashboard.ViewBasic,
            Permissions.Report.GenerateOwn,
            Permissions.Report.ViewHistory
        };
    }

    /// <summary>
    /// Gets Receptionist role permissions
    /// </summary>
    private static List<string> GetReceptionistPermissions()
    {
        return new List<string>
        {
            Permissions.Profile.ViewOwn,
            Permissions.Profile.UpdateOwn,
            Permissions.Profile.ChangePassword,
            Permissions.CheckIn.Process,
            Permissions.CheckIn.ProcessOut,
            Permissions.CheckIn.ViewQueue,
            Permissions.CheckIn.ViewHistory,
            Permissions.CheckIn.PrintBadge,
            Permissions.CheckIn.QRScan,
            Permissions.CheckIn.Override,
            Permissions.CheckIn.ManualVerification,
            Permissions.WalkIn.Register,
            Permissions.WalkIn.QuickRegister,
            Permissions.WalkIn.CheckIn,
            Permissions.WalkIn.ViewList,
            Permissions.WalkIn.ViewHistory,
            Permissions.Invitation.ReadAll,
            Permissions.Invitation.ViewPending, // Key: Can see pending invitations
            Permissions.Invitation.ViewHistory,
            Permissions.Visitor.ReadAll,
            Permissions.Visitor.ReadToday,
            Permissions.Visitor.ViewHistory,
            Permissions.Visitor.Create, // For walk-in creation
            Permissions.Visitor.Update,
            Permissions.Visitor.Read,
            Permissions.Visitor.Search,
            Permissions.Visitor.ManagePhotos,
            Permissions.Company.Read,
            Permissions.Company.Create,
            Permissions.VisitorDocument.Create,
            Permissions.VisitorDocument.Read,
            Permissions.VisitorDocument.Update,
            Permissions.VisitorDocument.Upload,
            Permissions.VisitorDocument.Download,
            Permissions.QRCode.Scan,
            Permissions.QRCode.Validate,
            Permissions.QRCode.ViewHistory,
            Permissions.Badge.Print,
            Permissions.Badge.ReprintLost,
            Permissions.Notification.ReadOwn,
            Permissions.Notification.ReadAll,
            Permissions.Notification.Receive, // Alerts consolidated into Notification
            Permissions.Notification.Acknowledge,
            Permissions.Dashboard.ViewBasic,
            Permissions.Dashboard.ViewOperations,
            Permissions.Calendar.ViewAll,
            Permissions.Calendar.ViewAvailability,
            Permissions.Emergency.Export,
            Permissions.Emergency.ViewRoster,
            Permissions.Emergency.PrintRoster,
            Permissions.Report.GenerateAll,
            Permissions.Report.ViewHistory,
            Permissions.Report.Export
        };
    }

    private static async Task EnsureBuiltInRoleCompanyPermissionsAsync(ApplicationDbContext context)
    {
        var roleNames = new[] { "Staff", "Receptionist", "Administrator" };
        var roles = await context.Roles
            .Where(r => roleNames.Contains(r.Name))
            .ToDictionaryAsync(r => r.Name, r => r.Id);

        if (!roles.Any())
        {
            Console.WriteLine("Warning: Built-in roles not found while syncing company permissions.");
            return;
        }

        var companyPermissionsByRole = new Dictionary<string, List<string>>
        {
            ["Staff"] = new() { Permissions.Company.Read },
            ["Receptionist"] = new() { Permissions.Company.Read, Permissions.Company.Create },
            ["Administrator"] = new() { Permissions.Company.Create, Permissions.Company.Read, Permissions.Company.Update, Permissions.Company.Delete }
        };

        var requiredPermissionNames = companyPermissionsByRole
            .SelectMany(kvp => kvp.Value)
            .Distinct()
            .ToList();

        var permissions = await context.Permissions
            .Where(p => requiredPermissionNames.Contains(p.Name))
            .ToDictionaryAsync(p => p.Name, p => p.Id);

        if (!permissions.Any())
        {
            Console.WriteLine("Warning: Company permissions were not found in the permissions table.");
            return;
        }

        var targetRoleIds = roles.Values.ToList();
        var targetPermissionIds = permissions.Values.ToList();

        var existingMappings = await context.RolePermissions
            .Where(rp => targetRoleIds.Contains(rp.RoleId) && targetPermissionIds.Contains(rp.PermissionId))
            .Select(rp => new { rp.RoleId, rp.PermissionId })
            .ToListAsync();

        var existingMappingKeys = existingMappings
            .Select(rp => $"{rp.RoleId}:{rp.PermissionId}")
            .ToHashSet();

        var newMappings = new List<RolePermission>();

        foreach (var (roleName, permissionNames) in companyPermissionsByRole)
        {
            if (!roles.TryGetValue(roleName, out var roleId))
            {
                continue;
            }

            foreach (var permissionName in permissionNames)
            {
                if (!permissions.TryGetValue(permissionName, out var permissionId))
                {
                    Console.WriteLine($"Warning: Permission '{permissionName}' not found while syncing role '{roleName}'.");
                    continue;
                }

                var mappingKey = $"{roleId}:{permissionId}";
                if (existingMappingKeys.Contains(mappingKey))
                {
                    continue;
                }

                newMappings.Add(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionId,
                    GrantedAt = DateTime.UtcNow,
                    GrantedBy = null
                });

                existingMappingKeys.Add(mappingKey);
            }
        }

        if (!newMappings.Any())
        {
            Console.WriteLine("Company permission sync complete. No new role mappings were required.");
            return;
        }

        await context.RolePermissions.AddRangeAsync(newMappings);
        await context.SaveChangesAsync();

        Console.WriteLine($"Added {newMappings.Count} company permission mappings for built-in roles.");
    }

    /// <summary>
    /// Additively syncs permissions that were introduced after the initial seed.
    /// Call this from SeedRolePermissionsAsync alongside EnsureBuiltInRoleCompanyPermissionsAsync.
    /// </summary>
    public static async Task EnsureNewSystemPermissionsAsync(ApplicationDbContext context)
    {
        // Permissions added after initial seed that must be patched into existing role rows
        var additiveMappings = new Dictionary<string, string[]>
        {
            ["Administrator"] = new[] { Permissions.Notification.ManageRecipients },
            ["Receptionist"]  = new[] { Permissions.Visitor.Update }
        };

        var roleNames = additiveMappings.Keys.ToArray();
        var roles = await context.Roles
            .Where(r => roleNames.Contains(r.Name))
            .ToDictionaryAsync(r => r.Name!, r => r.Id);

        var allPermissionNames = additiveMappings.Values.SelectMany(p => p).Distinct().ToList();
        var permissionLookup = await context.Permissions
            .Where(p => allPermissionNames.Contains(p.Name))
            .ToDictionaryAsync(p => p.Name, p => p.Id);

        var toAdd = new List<RolePermission>();

        foreach (var (roleName, permNames) in additiveMappings)
        {
            if (!roles.TryGetValue(roleName, out var roleId)) continue;

            var permIds = permNames
                .Where(n => permissionLookup.ContainsKey(n))
                .Select(n => permissionLookup[n])
                .ToList();

            var existing = (await context.RolePermissions
                .Where(rp => rp.RoleId == roleId && permIds.Contains(rp.PermissionId))
                .Select(rp => rp.PermissionId)
                .ToListAsync())
                .ToHashSet();

            toAdd.AddRange(permNames
                .Where(n => permissionLookup.TryGetValue(n, out var pid) && !existing.Contains(pid))
                .Select(n => new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionLookup[n],
                    GrantedAt = DateTime.UtcNow,
                    GrantedBy = null
                }));
        }

        if (!toAdd.Any())
        {
            Console.WriteLine("New system permission sync complete. No additions required.");
            return;
        }

        await context.RolePermissions.AddRangeAsync(toAdd);
        await context.SaveChangesAsync();
        Console.WriteLine($"Patched {toAdd.Count} new system permissions across roles.");
    }

    /// <summary>
    /// Additively ensures Civil Defense roles have their required permissions.
    /// Safe to run on every startup — skips already-assigned permissions.
    /// </summary>
    public static async Task EnsureCivilDefenseRolePermissionsAsync(ApplicationDbContext context)
    {
        var cdRoleMappings = new Dictionary<string, string[]>
        {
            ["CivilDefense_Receptionist"] = new[]
            {
                Permissions.CivilDefense.ViewPresence,
                Permissions.CivilDefense.CheckInStaff,
                Permissions.CivilDefense.CheckOutStaff,
                Permissions.CivilDefense.RegisterVisitor,
                Permissions.CivilDefense.CheckOutVisitor,
                Permissions.VisitPurpose.Read,
                Permissions.Location.Read,
                Permissions.Profile.ViewOwn,
                Permissions.Profile.UpdateOwn,
                Permissions.Profile.ChangePassword,
                Permissions.Notification.ReadOwn,
                Permissions.Notification.Receive,
                Permissions.Notification.Acknowledge
            },
            ["CivilDefense_Manager"] = new[]
            {
                Permissions.CivilDefense.ViewPresence,
                Permissions.CivilDefense.ViewReports,
                Permissions.CivilDefense.ExportReports,
                Permissions.Profile.ViewOwn,
                Permissions.Profile.UpdateOwn,
                Permissions.Profile.ChangePassword,
                Permissions.Notification.ReadOwn,
                Permissions.Notification.Receive,
                Permissions.Notification.Acknowledge
            }
        };

        var roleNames = cdRoleMappings.Keys.ToArray();
        var roles = await context.Roles
            .Where(r => roleNames.Contains(r.Name))
            .ToDictionaryAsync(r => r.Name!, r => r.Id);

        if (!roles.Any())
        {
            Console.WriteLine("Civil Defense roles not found — skipping CD permission sync.");
            return;
        }

        var allPermissionNames = cdRoleMappings.Values.SelectMany(p => p).Distinct().ToList();
        var permissionLookup = await context.Permissions
            .Where(p => allPermissionNames.Contains(p.Name))
            .ToDictionaryAsync(p => p.Name, p => p.Id);

        var toAdd = new List<RolePermission>();

        foreach (var (roleName, permNames) in cdRoleMappings)
        {
            if (!roles.TryGetValue(roleName, out var roleId)) continue;

            var permIds = permNames
                .Where(n => permissionLookup.ContainsKey(n))
                .Select(n => permissionLookup[n])
                .ToList();

            var existing = (await context.RolePermissions
                .Where(rp => rp.RoleId == roleId && permIds.Contains(rp.PermissionId))
                .Select(rp => rp.PermissionId)
                .ToListAsync())
                .ToHashSet();

            toAdd.AddRange(permNames
                .Where(n => permissionLookup.TryGetValue(n, out var pid) && !existing.Contains(pid))
                .Select(n => new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionLookup[n],
                    GrantedAt = DateTime.UtcNow,
                    GrantedBy = null
                }));
        }

        if (!toAdd.Any())
        {
            Console.WriteLine("Civil Defense permission sync complete. No additions required.");
            return;
        }

        await context.RolePermissions.AddRangeAsync(toAdd);
        await context.SaveChangesAsync();
        Console.WriteLine($"Assigned {toAdd.Count} permissions to Civil Defense roles.");
    }
}
