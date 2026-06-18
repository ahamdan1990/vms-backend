using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Infrastructure.Data;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Seeds;

/// <summary>
/// Comprehensive configuration seeder that migrates ALL settings to database
/// </summary>
public static class ComprehensiveConfigurationSeeder
{
    /// <summary>
    /// Seeds all system configurations into the database
    /// </summary>
    public static async Task SeedAllConfigurationsAsync(ApplicationDbContext context, IServiceProvider serviceProvider, int? systemUserId = null)
    {
        if (context.SystemConfigurations.Any())
        {
            return; // Already seeded
        }

        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger("ComprehensiveConfigurationSeeder");
        
        logger?.LogInformation("Seeding comprehensive system configurations...");

        // Find an existing admin user or use null for system seeding
        if (systemUserId == null)
        {
            var adminUser = await context.Users
                .Where(u => u.Role == Domain.Enums.UserRole.Administrator)
                .FirstOrDefaultAsync();
            
            if (adminUser != null)
            {
                systemUserId = adminUser.Id;
                logger?.LogInformation("Using admin user ID {UserId} for system configuration seeding", systemUserId);
            }
            else
            {
                logger?.LogWarning("No admin user found, system configurations will be created without CreatedBy reference");
                systemUserId = null; // Explicitly set to null for system configurations
            }
        }

        var configurations = new List<SystemConfiguration>();
        var now = DateTime.UtcNow;

        // Read from appsettings.json for initial migration, then use defaults
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        try
        {
            // 1. JWT Configuration
            await SeedJwtConfigurationAsync(configurations, configuration, now, systemUserId);
            
            // 2. Security Configuration (password policy only)
            await SeedSecurityConfigurationAsync(configurations, configuration, now, systemUserId);

            // 3. Lockout Configuration
            await SeedLockoutConfigurationAsync(configurations, now, systemUserId);

            // 4. Email Configuration
            await SeedEmailConfigurationAsync(configurations, configuration, now, systemUserId);

            // 5. SMS Configuration (provision — not yet wired)
            await SeedSmsConfigurationAsync(configurations, configuration, now, systemUserId);

            // 6. System Settings Configuration
            await SeedSystemSettingsConfigurationAsync(configurations, configuration, now, systemUserId);

            // 7. FR System Configuration
            await SeedFrSystemConfigurationAsync(configurations, configuration, now, systemUserId);

            // 8. Application Configuration
            await SeedApplicationConfigurationAsync(configurations, configuration, now, systemUserId);

            // 11. LDAP Configuration
            await SeedLdapConfigurationAsync(configurations, configuration, now, systemUserId);

            // 12. Invitations Configuration
            await SeedInvitationsConfigurationAsync(configurations, now, systemUserId);

            // 13. Backup Configuration
            await SeedBackupConfigurationAsync(configurations, now, systemUserId);

            // 14. Storage Alert Configuration
            await SeedStorageAlertConfigurationAsync(configurations, now, systemUserId);

            // 15. Speech Recognition Configuration
            await SpeechConfigurationSeeder.SeedAsync(configurations, now, systemUserId);

            logger?.LogInformation("Seeded {Count} configuration entries", configurations.Count);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Could not read some configurations from appsettings.json, using defaults");
            
            // Add default configurations if reading from appsettings.json fails
            AddDefaultConfigurations(configurations, now, systemUserId);
        }

        // Save to database
        await context.SystemConfigurations.AddRangeAsync(configurations);
        await context.SaveChangesAsync();

        logger?.LogInformation("Successfully seeded {Count} comprehensive configurations to database", configurations.Count);
    }

    /// <summary>
    /// Seeds only categories that are missing from the database.
    /// Safe to call on existing databases — never overwrites existing rows.
    /// </summary>
    public static async Task SeedMissingCategoriesAsync(ApplicationDbContext context, IServiceProvider serviceProvider)
    {
        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger("ComprehensiveConfigurationSeeder");

        var adminUser = await context.Users
            .Where(u => u.Role == Domain.Enums.UserRole.Administrator)
            .FirstOrDefaultAsync();
        int? systemUserId = adminUser?.Id;

        var now = DateTime.UtcNow;
        var toAdd = new List<SystemConfiguration>();

        // SpeechRecognition — added after initial deployment
        if (!await context.SystemConfigurations.AnyAsync(c => c.Category == "SpeechRecognition"))
        {
            logger?.LogInformation("Seeding missing category: SpeechRecognition");
            await SpeechConfigurationSeeder.SeedAsync(toAdd, now, systemUserId);
        }
        else
        {
            // Add individual keys that were added to the seeder after the category was first seeded
            if (!await context.SystemConfigurations.AnyAsync(c => c.Category == "SpeechRecognition" && c.Key == "TimeoutSeconds"))
            {
                toAdd.Add(new SystemConfiguration
                {
                    Category = "SpeechRecognition", Key = "TimeoutSeconds", Value = "30", DataType = "int",
                    Description = "HTTP request timeout when calling the Whisper service, in seconds.",
                    DefaultValue = "30", RequiresRestart = false, IsEncrypted = false, IsSensitive = false,
                    IsReadOnly = false, Group = "Speech Recognition", DisplayOrder = 0, Environment = "All",
                    CreatedBy = systemUserId, CreatedOn = now, IsActive = true,
                });
            }
            if (!await context.SystemConfigurations.AnyAsync(c => c.Category == "SpeechRecognition" && c.Key == "ConcurrencyLimit"))
            {
                toAdd.Add(new SystemConfiguration
                {
                    Category = "SpeechRecognition", Key = "ConcurrencyLimit", Value = "2", DataType = "int",
                    Description = "Maximum number of parallel transcription requests.",
                    DefaultValue = "2", RequiresRestart = false, IsEncrypted = false, IsSensitive = false,
                    IsReadOnly = false, Group = "Speech Recognition", DisplayOrder = 0, Environment = "All",
                    CreatedBy = systemUserId, CreatedOn = now, IsActive = true,
                });
            }
        }

        // Primary and backup face engine settings — added to FRSystem so all FR config is in one place
        if (!await context.SystemConfigurations.AnyAsync(c => c.Category == "FRSystem" && c.Key == "PrimaryEnabled"))
        {
            logger?.LogInformation("Seeding FRSystem: primary and backup engine settings");
            toAdd.AddRange(new[]
            {
                // Primary engine (local SDK)
                CreateConfiguration("FRSystem", "PrimaryEnabled", "false", "bool", "Enable primary face detection engine", true, false, false, now, systemUserId),
                CreateConfiguration("FRSystem", "PrimaryDetectionThreshold", "3", "int", "Detection sensitivity 1–10 (lower = more detections, requires restart)", true, false, false, now, systemUserId),
                CreateConfiguration("FRSystem", "PrimaryInternalResizeWidth", "640", "int", "Image width for detection in pixels — 640–1280 recommended (requires restart)", true, false, false, now, systemUserId),
                CreateConfiguration("FRSystem", "PrimaryMatchThreshold", "0.80", "decimal", "Minimum similarity score to confirm identity (0–1)", false, false, false, now, systemUserId),
                CreateConfiguration("FRSystem", "PrimaryCropMarginPercent", "20", "int", "Face crop padding percentage (0–50)", false, false, false, now, systemUserId),
                CreateConfiguration("FRSystem", "PrimaryMaxAdditionalTemplates", "5", "int", "Extra templates stored per person (1–20)", false, false, false, now, systemUserId),
                CreateConfiguration("FRSystem", "PrimaryArbitraryRotationsEnabled", "true", "bool", "Detect faces at any rotation — more accurate but slower (requires restart)", true, false, false, now, systemUserId),
                CreateConfiguration("FRSystem", "PrimaryDetermineRotationAngle", "true", "bool", "Compute roll angle per face — required for roll-limit filtering (requires restart)", true, false, false, now, systemUserId),
                CreateConfiguration("FRSystem", "PrimaryDebugFrameDumpEnabled", "false", "bool", "Write each frame to disk as JPEG — for diagnostics only", false, false, false, now, systemUserId),
                CreateConfiguration("FRSystem", "PrimaryDebugFrameDumpPath", "debug_frames", "string", "Folder path for debug frame dump files", false, false, false, now, systemUserId),

                // Backup engine (remote API)
                CreateConfiguration("FRSystem", "BackupEnabled", "false", "bool", "Enable backup face detection engine", true, false, false, now, systemUserId),
                CreateConfiguration("FRSystem", "BackupBaseUrl", "", "string", "Backup engine API base URL (e.g. http://localhost:8000)", true, false, false, now, systemUserId),
                CreateConfiguration("FRSystem", "BackupDetectionApiKey", "", "string", "API key for backup engine face detection endpoint", true, false, true, now, systemUserId),
                CreateConfiguration("FRSystem", "BackupRecognitionApiKey", "", "string", "API key for backup engine face recognition endpoint", true, false, true, now, systemUserId),
                CreateConfiguration("FRSystem", "BackupVerificationApiKey", "", "string", "API key for backup engine face verification endpoint", true, false, true, now, systemUserId),
                CreateConfiguration("FRSystem", "BackupDefaultMarginPercent", "40", "int", "Face crop margin percentage for backup engine (0–100)", false, false, false, now, systemUserId),
                CreateConfiguration("FRSystem", "BackupMinimumConfidence", "0.80", "decimal", "Minimum face detection confidence score (0–1)", false, false, false, now, systemUserId),
                CreateConfiguration("FRSystem", "BackupMinimumSimilarity", "0.85", "decimal", "Minimum similarity score for identity match (0–1)", false, false, false, now, systemUserId),
                CreateConfiguration("FRSystem", "BackupMaxFacesDetect", "1", "int", "Maximum faces to detect per request", false, false, false, now, systemUserId),
                CreateConfiguration("FRSystem", "BackupTimeoutSeconds", "2", "int", "HTTP request timeout in seconds (1–30)", false, false, false, now, systemUserId),
                CreateConfiguration("FRSystem", "BackupMaxRetries", "0", "int", "Number of retries on transient failure (0–5)", false, false, false, now, systemUserId),
                CreateConfiguration("FRSystem", "BackupCircuitBreakerThreshold", "1", "int", "Consecutive failures before circuit opens (1–10)", false, false, false, now, systemUserId),
                CreateConfiguration("FRSystem", "BackupCircuitBreakerRecoverySeconds", "60", "int", "Seconds before circuit breaker attempts recovery (10–3600)", false, false, false, now, systemUserId),
            });
        }

        // Lockout — added after initial deployment; fixes bug where Security seeder used wrong category/key names
        if (!await context.SystemConfigurations.AnyAsync(c => c.Category == "Lockout"))
        {
            logger?.LogInformation("Seeding missing category: Lockout");
            await SeedLockoutConfigurationAsync(toAdd, now, systemUserId);
        }

        if (toAdd.Count > 0)
        {
            await context.SystemConfigurations.AddRangeAsync(toAdd);
            await context.SaveChangesAsync();
            logger?.LogInformation("Seeded {Count} missing configuration rows", toAdd.Count);
        }
    }

    /// <summary>
    /// Deletes obsolete configuration rows that were seeded by earlier versions.
    /// Call this before SeedMissingCategoriesAsync so stale rows are gone before new ones are added.
    /// </summary>
    public static async Task CleanupObsoleteSettingsAsync(ApplicationDbContext context, ILogger? logger = null)
    {
        var toDelete = new (string Category, string Key)[]
        {
            // FRSystem — dead integration stubs (GraphQL FR system never built)
            ("FRSystem", "GraphQLEndpoint"), ("FRSystem", "ApiKey"),
            ("FRSystem", "WebhookSecret"),   ("FRSystem", "Timeout"),
            ("FRSystem", "RetryCount"),      ("FRSystem", "HealthCheckInterval"),

            // Security — Lockout (wrong category/key names; now in "Lockout" category)
            ("Security", "Lockout_DefaultLockoutTimeSpan"), ("Security", "Lockout_MaxFailedAccessAttempts"),
            ("Security", "Lockout_AllowedForNewUsers"),     ("Security", "Lockout_LockoutWindow"),
            ("Security", "Lockout_AutoUnlockAfterLockoutPeriod"), ("Security", "Lockout_NotifyOnLockout"),
            ("Security", "Lockout_MaxLockoutAttempts"),     ("Security", "Lockout_ExtendedLockoutDuration"),

            // Security — EncryptionKeys (Data Protection is configured at startup, not runtime)
            ("Security", "EncryptionKeys_DataProtectionKey"), ("Security", "EncryptionKeys_DatabaseEncryptionKey"),
            ("Security", "EncryptionKeys_CookieEncryptionKey"), ("Security", "EncryptionKeys_FileEncryptionKey"),
            ("Security", "EncryptionKeys_AutoRotateKeys"), ("Security", "EncryptionKeys_KeyRotationDays"),

            // Security — SessionSecurity (cookie/session middleware config, not runtime)
            ("Security", "SessionSecurity_SessionTimeout"), ("Security", "SessionSecurity_SlidingExpiration"),
            ("Security", "SessionSecurity_RequireSecureCookies"), ("Security", "SessionSecurity_RequireHttpOnlyCookies"),
            ("Security", "SessionSecurity_SameSiteMode"), ("Security", "SessionSecurity_EnableDeviceTracking"),
            ("Security", "SessionSecurity_EnableGeoLocationTracking"),

            // Security — Https (HSTS/HTTPS middleware, not runtime)
            ("Security", "Https_RequireHttps"), ("Security", "Https_RedirectHttpToHttps"),
            ("Security", "Https_HttpsPort"),    ("Security", "Https_EnableHsts"),
            ("Security", "Https_HstsMaxAge"),   ("Security", "Https_HstsIncludeSubdomains"),

            // Application — removed non-functional settings
            ("Application", "ApplicationVersion"), ("Application", "Environment"),
            ("Application", "EnableDebugMode"),    ("Application", "MaxConcurrentUsers"),
            ("Application", "EnableFeatureFlags"), ("Application", "CacheExpirationMinutes"),

            // SystemSettings — removed non-functional settings
            ("SystemSettings", "SessionTimeout"),           ("SystemSettings", "EnableAuditLogging"),
            ("SystemSettings", "EnableRealTimeNotifications"), ("SystemSettings", "DateFormat"),
            ("SystemSettings", "TimeFormat"),
        };

        var affectedCategories = toDelete.Select(x => x.Category).Distinct().ToList();
        var existing = await context.SystemConfigurations
            .Where(c => affectedCategories.Contains(c.Category))
            .ToListAsync();

        var specificRows = existing
            .Where(c => toDelete.Contains((c.Category, c.Key)))
            .ToList();

        if (specificRows.Count > 0)
            context.SystemConfigurations.RemoveRange(specificRows);

        // Remove entire dead categories
        var deadCategories = new[] { "Database", "Logging", "FileStorage", "Primary" };
        var deadRows = await context.SystemConfigurations
            .Where(c => deadCategories.Contains(c.Category))
            .ToListAsync();

        if (deadRows.Count > 0)
            context.SystemConfigurations.RemoveRange(deadRows);

        int total = specificRows.Count + deadRows.Count;
        if (total > 0)
        {
            await context.SaveChangesAsync();
            logger?.LogInformation("CleanupObsoleteSettings: removed {Count} obsolete configuration rows", total);
        }
    }

    private static async Task SeedLockoutConfigurationAsync(List<SystemConfiguration> configurations, DateTime now, int? systemUserId)
    {
        configurations.AddRange(new[]
        {
            CreateConfiguration("Lockout", "MaxFailedAttempts", "5",
                "int", "Failed login attempts before account lockout", false, false, false, now, systemUserId),

            CreateConfiguration("Lockout", "LockoutDuration", "15",
                "int", "Account lockout duration in minutes", false, false, false, now, systemUserId),

            CreateConfiguration("Lockout", "EnableProgressiveLockout", "false",
                "bool", "Double lockout duration on each successive lockout", false, false, false, now, systemUserId),

            CreateConfiguration("Lockout", "LockoutProgression", "15,30,60,1440",
                "string", "Comma-separated lockout durations (minutes) for progressive lockout", false, false, false, now, systemUserId),

            CreateConfiguration("Lockout", "FailedAttemptWindow", "10",
                "int", "Time window (minutes) in which failed attempts are counted", false, false, false, now, systemUserId),

            CreateConfiguration("Lockout", "ResetAttemptsOnSuccess", "true",
                "bool", "Reset failed attempt counter after successful login", false, false, false, now, systemUserId),

            CreateConfiguration("Lockout", "EnableIpBlocking", "false",
                "bool", "Block IP addresses after too many failed attempts", false, false, false, now, systemUserId),

            CreateConfiguration("Lockout", "MaxFailedAttemptsPerIp", "20",
                "int", "Failed attempts per IP before IP block is applied", false, false, false, now, systemUserId),

            CreateConfiguration("Lockout", "IpBlockDuration", "60",
                "int", "IP block duration in minutes", false, false, false, now, systemUserId),

            CreateConfiguration("Lockout", "NotifyOnLockout", "false",
                "bool", "Send notification to user when account is locked", false, false, false, now, systemUserId),

            CreateConfiguration("Lockout", "NotifyAdminOnLockout", "false",
                "bool", "Send notification to administrators when an account is locked", false, false, false, now, systemUserId),
        });

        await Task.CompletedTask;
    }

    private static async Task SeedJwtConfigurationAsync(List<SystemConfiguration> configurations, IConfiguration configuration, DateTime now, int? systemUserId)
    {
        var jwtSection = configuration.GetSection("JWT");
        
        configurations.AddRange(new[]
        {
            CreateConfiguration("JWT", "SecretKey",
                jwtSection["SecretKey"] ?? string.Empty,
                "string", "Secret key used for JWT token signing and validation", true, true, true, now, systemUserId),
                
            CreateConfiguration("JWT", "Issuer", 
                jwtSection["Issuer"] ?? "VisitorManagementSystem",
                "string", "JWT token issuer", false, false, false, now, systemUserId),
                
            CreateConfiguration("JWT", "Audience", 
                jwtSection["Audience"] ?? "VMS-Users", 
                "string", "JWT token audience", false, false, false, now, systemUserId),
                
            CreateConfiguration("JWT", "ExpiryInMinutes", 
                jwtSection["ExpiryInMinutes"] ?? "15",
                "int", "JWT access token expiry time in minutes", false, false, false, now, systemUserId),
                
            CreateConfiguration("JWT", "RefreshTokenExpiryInDays", 
                jwtSection["RefreshTokenExpiryInDays"] ?? "7",
                "int", "Refresh token expiry time in days", false, false, false, now, systemUserId),
                
            CreateConfiguration("JWT", "Algorithm", 
                jwtSection["Algorithm"] ?? "HS256",
                "string", "JWT signing algorithm", true, false, false, now, systemUserId),
                
            CreateConfiguration("JWT", "ValidateIssuerSigningKey", 
                jwtSection["ValidateIssuerSigningKey"] ?? "true",
                "bool", "Validate JWT issuer signing key", false, false, false, now, systemUserId),
                
            CreateConfiguration("JWT", "ValidateIssuer", 
                jwtSection["ValidateIssuer"] ?? "true",
                "bool", "Validate JWT issuer", false, false, false, now, systemUserId),
                
            CreateConfiguration("JWT", "ValidateAudience", 
                jwtSection["ValidateAudience"] ?? "true",
                "bool", "Validate JWT audience", false, false, false, now, systemUserId),
                
            CreateConfiguration("JWT", "ValidateLifetime", 
                jwtSection["ValidateLifetime"] ?? "true",
                "bool", "Validate JWT token lifetime", false, false, false, now, systemUserId),
                
            CreateConfiguration("JWT", "RequireExpirationTime", 
                jwtSection["RequireExpirationTime"] ?? "true",
                "bool", "Require expiration time in JWT tokens", false, false, false, now, systemUserId),
                
            CreateConfiguration("JWT", "ClockSkewMinutes", 
                jwtSection["ClockSkewMinutes"] ?? "0",
                "int", "Clock skew tolerance in minutes", false, false, false, now, systemUserId),
                
            CreateConfiguration("JWT", "PasswordResetTokenExpiryMinutes", 
                jwtSection["PasswordResetTokenExpiryMinutes"] ?? "30",
                "int", "Password reset token expiry time in minutes", false, false, false, now, systemUserId),
                
            CreateConfiguration("JWT", "EmailConfirmationTokenExpiryHours", 
                jwtSection["EmailConfirmationTokenExpiryHours"] ?? "24",
                "int", "Email confirmation token expiry time in hours", false, false, false, now, systemUserId),
                
            CreateConfiguration("JWT", "TwoFactorTokenExpiryMinutes", 
                jwtSection["TwoFactorTokenExpiryMinutes"] ?? "5",
                "int", "Two-factor authentication token expiry time in minutes", false, false, false, now, systemUserId),
                
            CreateConfiguration("JWT", "AllowConcurrentSessions", 
                jwtSection["AllowConcurrentSessions"] ?? "true",
                "bool", "Allow multiple concurrent sessions per user", false, false, false, now, systemUserId),
                
            CreateConfiguration("JWT", "MaxConcurrentSessions", 
                jwtSection["MaxConcurrentSessions"] ?? "5",
                "int", "Maximum number of concurrent sessions per user", false, false, false, now, systemUserId),
                
            CreateConfiguration("JWT", "RotateRefreshTokens", 
                jwtSection["RotateRefreshTokens"] ?? "true",
                "bool", "Rotate refresh tokens on usage", false, false, false, now, systemUserId),
                
            CreateConfiguration("JWT", "RevokeFamilyOnSuspiciousActivity", 
                jwtSection["RevokeFamilyOnSuspiciousActivity"] ?? "true",
                "bool", "Revoke refresh token families on suspicious activity", false, false, false, now, systemUserId)
        });

        await Task.CompletedTask;
    }

    private static async Task SeedSecurityConfigurationAsync(List<SystemConfiguration> configurations, IConfiguration configuration, DateTime now, int? systemUserId)
    {
        var securitySection = configuration.GetSection("Security");
        
        // Password Policy
        var passwordSection = securitySection.GetSection("PasswordPolicy");
        configurations.AddRange(new[]
        {
            CreateConfiguration("Security", "PasswordPolicy_RequireDigit",
                passwordSection["RequireDigit"] ?? "true",
                "bool", "Require at least one digit in passwords", false, false, false, now, systemUserId),

            CreateConfiguration("Security", "PasswordPolicy_RequireLowercase",
                passwordSection["RequireLowercase"] ?? "true",
                "bool", "Require at least one lowercase letter in passwords", false, false, false, now, systemUserId),

            CreateConfiguration("Security", "PasswordPolicy_RequireUppercase",
                passwordSection["RequireUppercase"] ?? "true",
                "bool", "Require at least one uppercase letter in passwords", false, false, false, now, systemUserId),

            CreateConfiguration("Security", "PasswordPolicy_RequireNonAlphanumeric",
                passwordSection["RequireNonAlphanumeric"] ?? "true",
                "bool", "Require at least one special character in passwords", false, false, false, now, systemUserId),

            CreateConfiguration("Security", "PasswordPolicy_RequiredLength",
                passwordSection["RequiredLength"] ?? "8",
                "int", "Minimum password length", false, false, false, now, systemUserId),

            CreateConfiguration("Security", "PasswordPolicy_RequiredUniqueChars",
                passwordSection["RequiredUniqueChars"] ?? "3",
                "int", "Required unique characters in password", false, false, false, now, systemUserId),

            CreateConfiguration("Security", "PasswordPolicy_MaxLength",
                passwordSection["MaxLength"] ?? "128",
                "int", "Maximum password length", false, false, false, now, systemUserId),

            CreateConfiguration("Security", "PasswordPolicy_PasswordHistoryLimit",
                passwordSection["PasswordHistoryLimit"] ?? "5",
                "int", "Number of previous passwords to remember", false, false, false, now, systemUserId),

            CreateConfiguration("Security", "PasswordPolicy_PasswordExpiryDays",
                passwordSection["PasswordExpiryDays"] ?? "90",
                "int", "Password expiry in days", false, false, false, now, systemUserId),

            CreateConfiguration("Security", "PasswordPolicy_PreventPasswordReuse",
                passwordSection["PreventPasswordReuse"] ?? "true",
                "bool", "Prevent password reuse", false, false, false, now, systemUserId),

            CreateConfiguration("Security", "PasswordPolicy_RequirePeriodicChange",
                passwordSection["RequirePeriodicChange"] ?? "true",
                "bool", "Require periodic password change", false, false, false, now, systemUserId),

            CreateConfiguration("Security", "PasswordPolicy_MinimumAge",
                passwordSection["MinimumAge"] ?? "1",
                "int", "Minimum age of password in days", false, false, false, now, systemUserId),

            CreateConfiguration("Security", "PasswordPolicy_MaximumAge",
                passwordSection["MaximumAge"] ?? "90",
                "int", "Maximum age of password in days", false, false, false, now, systemUserId),

            CreateConfiguration("Security", "PasswordPolicy_PasswordExpiryWarningDays",
                passwordSection["PasswordExpiryWarningDays"] ?? "14",
                "int", "Days before expiry to warn user", false, false, false, now, systemUserId),
        });

        await Task.CompletedTask;
    }

    // Dead method — no longer called from SeedAllConfigurationsAsync.
    // CleanupObsoleteSettingsAsync will remove any rows previously seeded by these.
    private static async Task SeedDatabaseConfigurationAsync(List<SystemConfiguration> configurations, IConfiguration configuration, DateTime now, int? systemUserId)
    {
        var databaseSection = configuration.GetSection("Database");
        
        configurations.AddRange(new[]
        {
            CreateConfiguration("Database", "CommandTimeout", 
                databaseSection["CommandTimeout"] ?? "30",
                "int", "Database command timeout in seconds", false, false, false, now, systemUserId),
                
            CreateConfiguration("Database", "MaxRetryCount", 
                databaseSection["MaxRetryCount"] ?? "3",
                "int", "Maximum retry count for database operations", false, false, false, now, systemUserId),
                
            CreateConfiguration("Database", "EnableSensitiveDataLogging", 
                databaseSection["EnableSensitiveDataLogging"] ?? "false",
                "bool", "Enable sensitive data logging (development only)", false, false, false, now, systemUserId),
                
            CreateConfiguration("Database", "EnableDetailedErrors", 
                databaseSection["EnableDetailedErrors"] ?? "false",
                "bool", "Enable detailed error messages", false, false, false, now, systemUserId)
        });

        // Connection Pool
        var poolSection = databaseSection.GetSection("ConnectionPool");
        configurations.AddRange(new[]
        {
            CreateConfiguration("Database", "ConnectionPool_MinPoolSize", 
                poolSection["MinPoolSize"] ?? "5",
                "int", "Minimum connection pool size", false, false, false, now, systemUserId),
                
            CreateConfiguration("Database", "ConnectionPool_MaxPoolSize", 
                poolSection["MaxPoolSize"] ?? "100",
                "int", "Maximum connection pool size", false, false, false, now, systemUserId),
                
            CreateConfiguration("Database", "ConnectionPool_ConnectionTimeout", 
                poolSection["ConnectionTimeout"] ?? "00:00:30",
                "timespan", "Database connection timeout", false, false, false, now, systemUserId),
                
            CreateConfiguration("Database", "ConnectionPool_ConnectionLifetime", 
                poolSection["ConnectionLifetime"] ?? "00:30:00",
                "timespan", "Maximum connection lifetime", false, false, false, now, systemUserId),
                
            CreateConfiguration("Database", "ConnectionPool_Pooling", 
                poolSection["Pooling"] ?? "true",
                "bool", "Enable connection pooling", false, false, false, now, systemUserId),
                
            CreateConfiguration("Database", "ConnectionPool_ConnectionIdleTimeout", 
                poolSection["ConnectionIdleTimeout"] ?? "300",
                "int", "Connection idle timeout in seconds", false, false, false, now, systemUserId)
        });

        // Migration
        var migrationSection = databaseSection.GetSection("Migration");
        configurations.AddRange(new[]
        {
            CreateConfiguration("Database", "Migration_AutoMigrate", 
                migrationSection["AutoMigrate"] ?? "false",
                "bool", "Automatically run migrations on startup", false, false, false, now, systemUserId),
                
            CreateConfiguration("Database", "Migration_ValidateOnStartup", 
                migrationSection["ValidateOnStartup"] ?? "true",
                "bool", "Validate database schema on startup", false, false, false, now, systemUserId),
                
            CreateConfiguration("Database", "Migration_MigrationHistoryTable", 
                migrationSection["MigrationHistoryTable"] ?? "__EFMigrationsHistory",
                "string", "Migration history table name", false, false, false, now, systemUserId)
        });

        // Performance
        var performanceSection = databaseSection.GetSection("Performance");
        configurations.AddRange(new[]
        {
            CreateConfiguration("Database", "Performance_EnableQueryCache", 
                performanceSection["EnableQueryCache"] ?? "true",
                "bool", "Enable query result caching", false, false, false, now, systemUserId),
                
            CreateConfiguration("Database", "Performance_QueryCacheSize", 
                performanceSection["QueryCacheSize"] ?? "1000",
                "int", "Query cache size limit", false, false, false, now, systemUserId),
                
            CreateConfiguration("Database", "Performance_QueryCacheExpiration", 
                performanceSection["QueryCacheExpiration"] ?? "00:05:00",
                "timespan", "Query cache expiration time", false, false, false, now, systemUserId),
                
            CreateConfiguration("Database", "Performance_EnableConnectionResiliency", 
                performanceSection["EnableConnectionResiliency"] ?? "true",
                "bool", "Enable connection resiliency", false, false, false, now, systemUserId),
                
            CreateConfiguration("Database", "Performance_LogSlowQueries", 
                performanceSection["LogSlowQueries"] ?? "true",
                "bool", "Log slow running queries", false, false, false, now, systemUserId),
                
            CreateConfiguration("Database", "Performance_SlowQueryThreshold", 
                performanceSection["SlowQueryThreshold"] ?? "00:00:01",
                "timespan", "Threshold for slow query logging", false, false, false, now, systemUserId),
                
            CreateConfiguration("Database", "Performance_EnableStatistics", 
                performanceSection["EnableStatistics"] ?? "true",
                "bool", "Enable database statistics collection", false, false, false, now, systemUserId)
        });

        // Health Check
        var healthCheckSection = databaseSection.GetSection("HealthCheck");
        configurations.AddRange(new[]
        {
            CreateConfiguration("Database", "HealthCheck_Enabled", 
                healthCheckSection["Enabled"] ?? "true",
                "bool", "Enable database health checks", false, false, false, now, systemUserId),
                
            CreateConfiguration("Database", "HealthCheck_Timeout", 
                healthCheckSection["Timeout"] ?? "00:00:10",
                "timespan", "Health check timeout", false, false, false, now, systemUserId),
                
            CreateConfiguration("Database", "HealthCheck_Interval", 
                healthCheckSection["Interval"] ?? "00:01:00",
                "timespan", "Health check interval", false, false, false, now, systemUserId),
                
            CreateConfiguration("Database", "HealthCheck_TestQuery", 
                healthCheckSection["TestQuery"] ?? "SELECT 1",
                "string", "Health check test query", false, false, false, now, systemUserId),
                
            CreateConfiguration("Database", "HealthCheck_CheckWriteConnection", 
                healthCheckSection["CheckWriteConnection"] ?? "true",
                "bool", "Check write connection in health check", false, false, false, now, systemUserId)
        });

        await Task.CompletedTask;
    }

    private static async Task SeedLoggingConfigurationAsync(List<SystemConfiguration> configurations, IConfiguration configuration, DateTime now, int? systemUserId)
    {
        var loggingSection = configuration.GetSection("Logging");
        
        configurations.AddRange(new[]
        {
            CreateConfiguration("Logging", "LogLevel", 
                loggingSection["LogLevel:Default"] ?? "Information",
                "string", "Global log level", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "EnableStructuredLogging", 
                loggingSection["EnableStructuredLogging"] ?? "true",
                "bool", "Enable structured logging", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "EnableCorrelationId", 
                loggingSection["EnableCorrelationId"] ?? "true",
                "bool", "Enable correlation ID logging", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "LogRequestResponse", 
                loggingSection["LogRequestResponse"] ?? "true",
                "bool", "Log request/response details", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "LogPerformanceMetrics", 
                loggingSection["LogPerformanceMetrics"] ?? "true",
                "bool", "Log performance metrics", false, false, false, now, systemUserId)
        });

        // Console Logging
        var consoleSection = loggingSection.GetSection("Console");
        configurations.AddRange(new[]
        {
            CreateConfiguration("Logging", "Console_Enabled", 
                consoleSection["Enabled"] ?? "true",
                "bool", "Enable console logging", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "Console_LogLevel", 
                consoleSection["LogLevel"] ?? "Information",
                "string", "Console log level", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "Console_UseColors", 
                consoleSection["UseColors"] ?? "true",
                "bool", "Use colors in console output", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "Console_IncludeScopes", 
                consoleSection["IncludeScopes"] ?? "true",
                "bool", "Include scopes in console logging", false, false, false, now, systemUserId)
        });

        // File Logging
        var fileSection = loggingSection.GetSection("File");
        configurations.AddRange(new[]
        {
            CreateConfiguration("Logging", "File_Enabled", 
                fileSection["Enabled"] ?? "true",
                "bool", "Enable file logging", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "File_LogLevel", 
                fileSection["LogLevel"] ?? "Information",
                "string", "File log level", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "File_Path", 
                fileSection["Path"] ?? "logs/vms-.txt",
                "string", "Log file path template", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "File_RollingInterval", 
                fileSection["RollingInterval"] ?? "Day",
                "string", "Log file rolling interval", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "File_RetainedFileCountLimit", 
                fileSection["RetainedFileCountLimit"] ?? "30",
                "int", "Number of log files to retain", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "File_FileSizeLimitBytes", 
                fileSection["FileSizeLimitBytes"] ?? "104857600",
                "long", "Log file size limit in bytes", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "File_RollOnFileSizeLimit", 
                fileSection["RollOnFileSizeLimit"] ?? "true",
                "bool", "Roll log files when size limit reached", false, false, false, now, systemUserId)
        });

        // Audit Logging
        var auditSection = loggingSection.GetSection("Audit");
        configurations.AddRange(new[]
        {
            CreateConfiguration("Logging", "Audit_Enabled", 
                auditSection["Enabled"] ?? "true",
                "bool", "Enable audit logging", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "Audit_LogLevel", 
                auditSection["LogLevel"] ?? "Information",
                "string", "Audit log level", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "Audit_LogUserActions", 
                auditSection["LogUserActions"] ?? "true",
                "bool", "Log user actions", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "Audit_LogDataChanges", 
                auditSection["LogDataChanges"] ?? "true",
                "bool", "Log data changes", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "Audit_LogSystemEvents", 
                auditSection["LogSystemEvents"] ?? "true",
                "bool", "Log system events", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "Audit_LogSecurityEvents", 
                auditSection["LogSecurityEvents"] ?? "true",
                "bool", "Log security events", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "Audit_LogLoginAttempts", 
                auditSection["LogLoginAttempts"] ?? "true",
                "bool", "Log login attempts", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "Audit_LogApiRequests", 
                auditSection["LogApiRequests"] ?? "true",
                "bool", "Log API requests", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "Audit_RetentionDays", 
                auditSection["RetentionDays"] ?? "365",
                "int", "Audit log retention period in days", false, false, false, now, systemUserId)
        });

        // Security Logging
        var securitySection = loggingSection.GetSection("Security");
        configurations.AddRange(new[]
        {
            CreateConfiguration("Logging", "Security_Enabled", 
                securitySection["Enabled"] ?? "true",
                "bool", "Enable security logging", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "Security_LogLevel", 
                securitySection["LogLevel"] ?? "Warning",
                "string", "Security log level", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "Security_LogFailedAuthentication", 
                securitySection["LogFailedAuthentication"] ?? "true",
                "bool", "Log failed authentication attempts", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "Security_LogUnauthorizedAccess", 
                securitySection["LogUnauthorizedAccess"] ?? "true",
                "bool", "Log unauthorized access attempts", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "Security_LogSuspiciousActivity", 
                securitySection["LogSuspiciousActivity"] ?? "true",
                "bool", "Log suspicious activities", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "Security_LogPasswordChanges", 
                securitySection["LogPasswordChanges"] ?? "true",
                "bool", "Log password changes", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "Security_LogAccountLockouts", 
                securitySection["LogAccountLockouts"] ?? "true",
                "bool", "Log account lockouts", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "Security_LogTokenEvents", 
                securitySection["LogTokenEvents"] ?? "true",
                "bool", "Log token-related events", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "Security_LogPermissionChanges", 
                securitySection["LogPermissionChanges"] ?? "true",
                "bool", "Log permission changes", false, false, false, now, systemUserId),
                
            CreateConfiguration("Logging", "Security_AlertOnSecurityEvents", 
                securitySection["AlertOnSecurityEvents"] ?? "true",
                "bool", "Send alerts on security events", false, false, false, now, systemUserId)
        });

        await Task.CompletedTask;
    }

    private static async Task SeedEmailConfigurationAsync(List<SystemConfiguration> configurations, IConfiguration configuration, DateTime now, int? systemUserId)
    {
        var emailSection = configuration.GetSection("Email");

        configurations.AddRange(new[]
        {
        CreateConfiguration("Email", "SmtpHost",
            emailSection["SmtpHost"] ?? "localhost",
            "string", "SMTP server hostname", false, false, false, now, systemUserId),

        CreateConfiguration("Email", "SmtpPort",
            emailSection["SmtpPort"] ?? "587",
            "int", "SMTP server port", false, false, false, now, systemUserId),

        CreateConfiguration("Email", "EnableSsl",
            emailSection["EnableSsl"] ?? "true",
            "bool", "Enable SSL for SMTP connection", false, false, false, now, systemUserId),

        CreateConfiguration("Email", "Username",
            emailSection["Username"] ?? "",
            "string", "SMTP username", false, true, false, now, systemUserId),

        CreateConfiguration("Email", "Password",
            emailSection["Password"] ?? "",
            "string", "SMTP password", false, true, true, now, systemUserId),

        CreateConfiguration("Email", "FromEmail",
            emailSection["FromEmail"] ?? "noreply@vms.com",
            "string", "Default sender email address", false, false, false, now, systemUserId),

        CreateConfiguration("Email", "FromName",
            emailSection["FromName"] ?? "Visitor Management System",
            "string", "Default sender name", false, false, false, now, systemUserId),

        CreateConfiguration("Email", "TimeoutSeconds",
            emailSection["TimeoutSeconds"] ?? "30",
            "int", "Connection timeout in seconds", false, false, false, now, systemUserId),

        CreateConfiguration("Email", "MaxAttachmentSizeMB",
            emailSection["MaxAttachmentSizeMB"] ?? "25",
            "int", "Maximum attachment size in MB", false, false, false, now, systemUserId),

        CreateConfiguration("Email", "EnableSending",
            emailSection["EnableSending"] ?? "true",
            "bool", "Enable email sending (for testing/staging environments)", false, false, false, now, systemUserId),

        CreateConfiguration("Email", "TestEmail",
            emailSection["TestEmail"] ?? "",
            "string", "Fallback email for testing (when EnableSending is false)", false, false, false, now, systemUserId),

        CreateConfiguration("Email", "TemplateDirectory",
            emailSection["TemplateDirectory"] ?? "EmailTemplates",
            "string", "Email template directory path", false, false, false, now, systemUserId),

        CreateConfiguration("Email", "CompanyLogoUrl",
            emailSection["CompanyLogoUrl"] ?? "",
            "string", "Company logo URL for email templates", false, false, false, now, systemUserId),

        CreateConfiguration("Email", "CompanyWebsiteUrl",
            emailSection["CompanyWebsiteUrl"] ?? "",
            "string", "Company website URL", false, false, false, now, systemUserId),

        CreateConfiguration("Email", "SupportEmail",
            emailSection["SupportEmail"] ?? "",
            "string", "Support email address", false, false, false, now, systemUserId)
    });

        await Task.CompletedTask;
    }

    private static async Task SeedSmsConfigurationAsync(List<SystemConfiguration> configurations, IConfiguration configuration, DateTime now, int? systemUserId)
    {
        var smsSection = configuration.GetSection("SMS");
        
        configurations.AddRange(new[]
        {
            CreateConfiguration("SMS", "Provider", 
                smsSection["Provider"] ?? "Twilio",
                "string", "SMS service provider", false, false, false, now, systemUserId),
                
            CreateConfiguration("SMS", "AccountSid", 
                smsSection["AccountSid"] ?? "",
                "string", "SMS provider account SID", false, true, false, now, systemUserId),
                
            CreateConfiguration("SMS", "AuthToken", 
                smsSection["AuthToken"] ?? "",
                "string", "SMS provider authentication token", false, true, true, now, systemUserId),
                
            CreateConfiguration("SMS", "FromNumber", 
                smsSection["FromNumber"] ?? "",
                "string", "SMS sender phone number", false, false, false, now, systemUserId)
        });

        await Task.CompletedTask;
    }

    private static async Task SeedFileStorageConfigurationAsync(List<SystemConfiguration> configurations, IConfiguration configuration, DateTime now, int? systemUserId)
    {
        var fileStorageSection = configuration.GetSection("FileStorage");
        
        configurations.AddRange(new[]
        {
            CreateConfiguration("FileStorage", "Provider", 
                fileStorageSection["Provider"] ?? "Local",
                "string", "File storage provider (Local, Azure, AWS)", false, false, false, now, systemUserId),
                
            CreateConfiguration("FileStorage", "BasePath", 
                fileStorageSection["BasePath"] ?? "uploads",
                "string", "Base path for file storage", false, false, false, now, systemUserId),
                
            CreateConfiguration("FileStorage", "MaxFileSize", 
                fileStorageSection["MaxFileSize"] ?? "10485760",
                "long", "Maximum file size in bytes (10MB)", false, false, false, now, systemUserId),
                
            CreateConfiguration("FileStorage", "AllowedExtensions", 
                System.Text.Json.JsonSerializer.Serialize(fileStorageSection.GetSection("AllowedExtensions").Get<string[]>() ?? new[] { ".jpg", ".jpeg", ".png", ".pdf", ".xlsx", ".csv" }),
                "json", "Allowed file extensions", false, false, false, now, systemUserId)
        });

        await Task.CompletedTask;
    }

    private static async Task SeedSystemSettingsConfigurationAsync(List<SystemConfiguration> configurations, IConfiguration configuration, DateTime now, int? systemUserId)
    {
        var systemSection = configuration.GetSection("SystemSettings");
        
        configurations.AddRange(new[]
        {
            CreateConfiguration("SystemSettings", "DefaultTimeZone",
                systemSection["DefaultTimeZone"] ?? "UTC",
                "string", "Default system timezone", false, false, false, now, systemUserId),

            CreateConfiguration("SystemSettings", "DefaultPageSize",
                systemSection["DefaultPageSize"] ?? "20",
                "int", "Default page size for paginated results", false, false, false, now, systemUserId),

            CreateConfiguration("SystemSettings", "MaxPageSize",
                systemSection["MaxPageSize"] ?? "100",
                "int", "Maximum page size for paginated results", false, false, false, now, systemUserId),
        });

        await Task.CompletedTask;
    }

    private static async Task SeedFrSystemConfigurationAsync(List<SystemConfiguration> configurations, IConfiguration configuration, DateTime now, int? systemUserId)
    {
        var frSystemSection = configuration.GetSection("FRSystem");
        
        configurations.AddRange(new[]
        {
            CreateConfiguration("FRSystem", "RecognitionThreshold",
                frSystemSection["RecognitionThreshold"] ?? "0.9",
                "decimal", "Minimum similarity score (0.0–1.0) for a face match to be accepted. Lower = more permissive, higher = fewer false positives. Default 0.9.", false, false, false, now, systemUserId),
        });

        await Task.CompletedTask;
    }

    private static async Task SeedApplicationConfigurationAsync(List<SystemConfiguration> configurations, IConfiguration configuration, DateTime now, int? systemUserId)
    {
        configurations.AddRange(new[]
        {
            CreateConfiguration("Application", "ApplicationName",
                "Visitor Management System",
                "string", "Application display name", false, false, false, now, systemUserId),

            CreateConfiguration("Application", "MaintenanceMode",
                "false",
                "bool", "Enable maintenance mode — returns 503 for all non-admin requests", false, false, false, now, systemUserId),

            CreateConfiguration("Application", "MaintenanceMessage",
                "The system is currently under maintenance. Please try again later.",
                "string", "Message returned to clients during maintenance mode", false, false, false, now, systemUserId),

            CreateConfiguration("Application", "EnableSwagger",
                "true",
                "bool", "Expose Swagger/OpenAPI documentation (requires restart)", false, false, false, now, systemUserId),
        });

        await Task.CompletedTask;
    }

    private static async Task SeedLdapConfigurationAsync(List<SystemConfiguration> configurations, IConfiguration configuration, DateTime now, int? systemUserId)
    {
        var ldapSection = configuration.GetSection("LdapConfiguration");

        configurations.AddRange(new[]
        {
            CreateConfiguration("LDAP", "Enabled",
                ldapSection["Enabled"] ?? "false",
                "bool", "Enable or disable LDAP/Active Directory integration", false, false, false, now, systemUserId),

            CreateConfiguration("LDAP", "Server",
                ldapSection["Server"] ?? string.Empty,
                "string", "LDAP server hostname or IP address", true, false, false, now, systemUserId),

            CreateConfiguration("LDAP", "Port",
                ldapSection["Port"] ?? "389",
                "int", "LDAP server port", false, false, false, now, systemUserId),

            CreateConfiguration("LDAP", "Domain",
                ldapSection["Domain"] ?? string.Empty,
                "string", "Default domain for user principal names", false, false, false, now, systemUserId),

            CreateConfiguration("LDAP", "UserName",
                ldapSection["UserName"] ?? string.Empty,
                "string", "Service account username for LDAP binding", true, false, false, now, systemUserId),

            CreateConfiguration("LDAP", "Password",
                ldapSection["Password"] ?? string.Empty,
                "string", "Service account password", true, true, true, now, systemUserId),

            CreateConfiguration("LDAP", "BaseDn",
                ldapSection["BaseDn"] ?? string.Empty,
                "string", "Directory base distinguished name", true, false, false, now, systemUserId),

            CreateConfiguration("LDAP", "AutoCreateUsers",
                ldapSection["AutoCreateUsers"] ?? "true",
                "bool", "Automatically create users on first LDAP login", false, false, false, now, systemUserId),

            CreateConfiguration("LDAP", "SyncProfileOnLogin",
                ldapSection["SyncProfileOnLogin"] ?? "true",
                "bool", "Synchronize profile information on each login", false, false, false, now, systemUserId),

            CreateConfiguration("LDAP", "IncludeDirectoryUsersInHostSearch",
                ldapSection["IncludeDirectoryUsersInHostSearch"] ?? "true",
                "bool", "Show directory users when searching for hosts", false, false, false, now, systemUserId),

            CreateConfiguration("LDAP", "DefaultImportRole",
                ldapSection["DefaultImportRole"] ?? Domain.Enums.UserRole.Staff.ToString(),
                "string", "Default role to assign when importing LDAP users", false, false, false, now, systemUserId),

            CreateConfiguration("LDAP", "AllowRoleSelectionOnImport",
                ldapSection["AllowRoleSelectionOnImport"] ?? "false",
                "bool", "Allow overriding user role during LDAP import", false, false, false, now, systemUserId)
        });

        await Task.CompletedTask;
    }

    private static void AddDefaultConfigurations(List<SystemConfiguration> configurations, DateTime now, int? systemUserId)
    {
        // Add essential defaults if appsettings.json reading fails
        configurations.AddRange(new[]
        {
            CreateConfiguration("JWT", "SecretKey", string.Empty, "string", "Secret key for JWT signing", true, true, true, now, systemUserId),
            CreateConfiguration("JWT", "Issuer", "VisitorManagementSystem", "string", "JWT token issuer", false, false, false, now, systemUserId),
            CreateConfiguration("JWT", "Audience", "VMS-Users", "string", "JWT token audience", false, false, false, now, systemUserId),
            CreateConfiguration("JWT", "ExpiryInMinutes", "15", "int", "Token expiry time in minutes", false, false, false, now, systemUserId),
            CreateConfiguration("Security", "Https_RequireHttps", "true", "bool", "Require HTTPS for all requests", true, false, false, now, systemUserId),
            CreateConfiguration("Database", "CommandTimeout", "30", "int", "Database command timeout", false, false, false, now, systemUserId),
            CreateConfiguration("Logging", "LogLevel", "Information", "string", "Global log level", false, false, false, now, systemUserId),
            CreateConfiguration("Application", "ApplicationName", "Visitor Management System", "string", "Application name", false, false, false, now, systemUserId)
        });
    }

    private static async Task SeedInvitationsConfigurationAsync(List<SystemConfiguration> configurations, DateTime now, int? systemUserId)
    {
        configurations.AddRange(new[]
        {
            CreateConfiguration("Invitations", "RequireApprovalByDefault",
                "true",
                "bool", "When true, all new invitations are created as Submitted and require admin approval before the QR code is issued. Set to false to auto-approve.", false, false, false, now, systemUserId),

            CreateConfiguration("Invitations", "MaxAdvanceBookingDays",
                "90",
                "int", "Maximum number of days in advance that an invitation can be scheduled.", false, false, false, now, systemUserId),

            CreateConfiguration("Invitations", "DefaultVisitDurationHours",
                "2",
                "int", "Default visit duration in hours used when no end time is specified.", false, false, false, now, systemUserId)
        });

        await Task.CompletedTask;
    }

    private static async Task SeedBackupConfigurationAsync(List<SystemConfiguration> configurations, DateTime now, int? systemUserId)
    {
        configurations.AddRange(new[]
        {
            CreateConfiguration("Backup", "Enabled",
                "false",
                "bool", "Enable or disable automatic scheduled backups. Must be explicitly enabled by an administrator after verifying the destination path and disk space.", false, false, false, now, systemUserId),

            CreateConfiguration("Backup", "ScheduleTime",
                "02:00",
                "string", "Daily backup time in HH:mm format (24-hour). The backup runs once per day at this local time.", false, false, false, now, systemUserId),

            CreateConfiguration("Backup", "DestinationPath",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "VMS", "backups"),
                "string", "Absolute path to the folder where backup .bak files will be written. Must be writable by the application process.", false, false, false, now, systemUserId),

            CreateConfiguration("Backup", "RetentionDays",
                "14",
                "int", "Number of days to retain backup files. Files older than this value are deleted after each successful backup.", false, false, false, now, systemUserId),

            CreateConfiguration("Backup", "NextRunAt",
                "",
                "string", "ISO 8601 UTC datetime of the next scheduled backup. Calculated automatically by the scheduler.", false, false, false, now, systemUserId),

            CreateConfiguration("Backup", "LastSuccessAt",
                "",
                "string", "ISO 8601 UTC datetime of the last successful backup.", false, false, false, now, systemUserId),

            CreateConfiguration("Backup", "LastFailureMessage",
                "",
                "string", "Error message from the most recent failed backup, if any.", false, false, false, now, systemUserId),

            CreateConfiguration("Backup", "AutoBackupOnAlert",
                "false",
                "bool", "Trigger an immediate backup when the database data file crosses the alert threshold (85% of Express limit). The backup provides a recovery point before the situation worsens but does NOT free database space.", false, false, false, now, systemUserId),

            CreateConfiguration("Backup", "AutoBackupOnCritical",
                "true",
                "bool", "Trigger an immediate backup when the database data file crosses the critical threshold (95% of Express limit). Strongly recommended to keep enabled.", false, false, false, now, systemUserId),

            CreateConfiguration("Backup", "LastEventBackupAt",
                "",
                "string", "ISO 8601 UTC datetime of the last event-triggered (storage alert) backup. Used to enforce 6-hour cooldown between event-triggered backups.", false, false, false, now, systemUserId),
        });

        await Task.CompletedTask;
    }

    private static async Task SeedStorageAlertConfigurationAsync(List<SystemConfiguration> configurations, DateTime now, int? systemUserId)
    {
        configurations.AddRange(new[]
        {
            CreateConfiguration("Storage", "AlertEnabled",
                "true",
                "bool", "Master switch for storage monitoring alerts. When false, no storage threshold notifications are sent.", false, false, false, now, systemUserId),

            CreateConfiguration("Storage", "DbWarnThresholdPercent",
                "70",
                "int", "Database data file usage percentage at which a warning notification is sent to administrators (SQL Server Express 10 GB limit).", false, false, false, now, systemUserId),

            CreateConfiguration("Storage", "DbAlertThresholdPercent",
                "85",
                "int", "Database data file usage percentage at which a high-priority alert is sent. Auto-backup is triggered if Backup.AutoBackupOnAlert is enabled.", false, false, false, now, systemUserId),

            CreateConfiguration("Storage", "DbCriticalThresholdPercent",
                "95",
                "int", "Database data file usage percentage at which a critical alert is sent and auto-backup is triggered regardless of settings.", false, false, false, now, systemUserId),

            CreateConfiguration("Storage", "DiskWarnFreePercent",
                "20",
                "int", "Disk free space percentage below which a warning notification is sent.", false, false, false, now, systemUserId),

            CreateConfiguration("Storage", "DiskCriticalFreePercent",
                "10",
                "int", "Disk free space percentage below which a critical alert is sent and the next scheduled backup is suppressed.", false, false, false, now, systemUserId),

            CreateConfiguration("Storage", "LastDbAlertLevel",
                "None",
                "string", "Deduplication state for database size alerts. Values: None, Warning, High, Critical. Prevents repeated alerts for the same threshold.", false, false, false, now, systemUserId),

            CreateConfiguration("Storage", "LastDiskAlertLevel",
                "None",
                "string", "Deduplication state for disk space alerts. Values: None, Warning, Critical.", false, false, false, now, systemUserId),

            CreateConfiguration("Storage", "LastAlertFiredAt",
                "",
                "string", "ISO 8601 UTC datetime of the last storage alert. Enforces minimum 4-hour cooldown between repeated alert firings.", false, false, false, now, systemUserId),
        });

        await Task.CompletedTask;
    }

    private static SystemConfiguration CreateConfiguration(
        string category,
        string key,
        string value,
        string dataType, 
        string description, 
        bool requiresRestart = false, 
        bool isEncrypted = false, 
        bool isSensitive = false, 
        DateTime? createdOn = null, 
        int? createdBy = null)
    {
        return new SystemConfiguration
        {
            Category = category,
            Key = key,
            Value = value,
            DataType = dataType,
            Description = description,
            RequiresRestart = requiresRestart,
            IsEncrypted = isEncrypted,
            IsSensitive = isSensitive,
            IsReadOnly = false,
            DefaultValue = isSensitive || isEncrypted ? null : value,
            Group = GetGroupForCategory(category),
            DisplayOrder = GetDisplayOrderForKey(key),
            Environment = "All",
            CreatedBy = createdBy,
            CreatedOn = createdOn ?? DateTime.UtcNow,
            IsActive = true
        };
    }

    private static string GetGroupForCategory(string category)
    {
        return category switch
        {
            "JWT" => "Authentication",
            "Security" => "Security",
            "Database" => "Database",
            "Logging" => "Logging",
            "Email" => "Communication",
            "SMS" => "Communication",
            "FileStorage" => "Storage",
            "SystemSettings" => "System",
            "FRSystem" => "Integration",
            "Application" => "Application",
            _ => "General"
        };
    }

    private static int GetDisplayOrderForKey(string key)
    {
        return key switch
        {
            "SecretKey" => 1,
            "Issuer" => 2,
            "Audience" => 3,
            "ApplicationName" => 1,
            "RequireHttps" => 1,
            "LogLevel" => 1,
            _ => 99
        };
    }
}
