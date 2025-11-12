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
            
            // 2. Security Configuration
            await SeedSecurityConfigurationAsync(configurations, configuration, now, systemUserId);
            
            // 3. Database Configuration
            await SeedDatabaseConfigurationAsync(configurations, configuration, now, systemUserId);
            
            // 4. Logging Configuration
            await SeedLoggingConfigurationAsync(configurations, configuration, now, systemUserId);
            
            // 5. Email Configuration
            await SeedEmailConfigurationAsync(configurations, configuration, now, systemUserId);
            
            // 6. SMS Configuration
            await SeedSmsConfigurationAsync(configurations, configuration, now, systemUserId);
            
            // 7. File Storage Configuration
            await SeedFileStorageConfigurationAsync(configurations, configuration, now, systemUserId);
            
            // 8. System Settings Configuration
            await SeedSystemSettingsConfigurationAsync(configurations, configuration, now, systemUserId);
            
            // 9. FR System Configuration
            await SeedFrSystemConfigurationAsync(configurations, configuration, now, systemUserId);
            
            // 10. Application Configuration
            await SeedApplicationConfigurationAsync(configurations, configuration, now, systemUserId);

            // 11. LDAP Configuration
            await SeedLdapConfigurationAsync(configurations, configuration, now, systemUserId);

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

    private static async Task SeedJwtConfigurationAsync(List<SystemConfiguration> configurations, IConfiguration configuration, DateTime now, int? systemUserId)
    {
        var jwtSection = configuration.GetSection("JWT");
        
        configurations.AddRange(new[]
        {
            CreateConfiguration("JWT", "SecretKey", 
                jwtSection["SecretKey"] ?? "ThisIsAVeryLongSecretKeyForJWTTokenGenerationThatMustBeAtLeast256BitsLong",
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

        // Lockout Configuration
        var lockoutSection = securitySection.GetSection("Lockout");
        configurations.AddRange(new[]
        {
            CreateConfiguration("Security", "Lockout_DefaultLockoutTimeSpan", 
                lockoutSection["DefaultLockoutTimeSpan"] ?? "00:15:00",
                "timespan", "Default account lockout duration", false, false, false, now, systemUserId),
                
            CreateConfiguration("Security", "Lockout_MaxFailedAccessAttempts", 
                lockoutSection["MaxFailedAccessAttempts"] ?? "5",
                "int", "Maximum failed login attempts before lockout", false, false, false, now, systemUserId),
                
            CreateConfiguration("Security", "Lockout_AllowedForNewUsers", 
                lockoutSection["AllowedForNewUsers"] ?? "true",
                "bool", "Enable lockout for new users", false, false, false, now, systemUserId),
                
            CreateConfiguration("Security", "Lockout_LockoutWindow", 
                lockoutSection["LockoutWindow"] ?? "00:05:00",
                "timespan", "Time window for tracking failed attempts", false, false, false, now, systemUserId),
                
            CreateConfiguration("Security", "Lockout_AutoUnlockAfterLockoutPeriod", 
                lockoutSection["AutoUnlockAfterLockoutPeriod"] ?? "true",
                "bool", "Auto unlock accounts after lockout period", false, false, false, now, systemUserId),
                
            CreateConfiguration("Security", "Lockout_NotifyOnLockout", 
                lockoutSection["NotifyOnLockout"] ?? "true",
                "bool", "Send notification when account is locked", false, false, false, now, systemUserId),
                
            CreateConfiguration("Security", "Lockout_MaxLockoutAttempts", 
                lockoutSection["MaxLockoutAttempts"] ?? "10",
                "int", "Maximum lockout attempts before permanent lock", false, false, false, now, systemUserId),
                
            CreateConfiguration("Security", "Lockout_ExtendedLockoutDuration", 
                lockoutSection["ExtendedLockoutDuration"] ?? "1.00:00:00",
                "timespan", "Extended lockout duration for repeated violations", false, false, false, now, systemUserId)
        });

        // Encryption Keys
        var encryptionSection = securitySection.GetSection("EncryptionKeys");
        configurations.AddRange(new[]
        {
            CreateConfiguration("Security", "EncryptionKeys_DataProtectionKey", 
                encryptionSection["DataProtectionKey"] ?? "DataProtectionKey123456789012345678901234567890",
                "string", "Data protection encryption key", true, true, true, now, systemUserId),
                
            CreateConfiguration("Security", "EncryptionKeys_DatabaseEncryptionKey", 
                encryptionSection["DatabaseEncryptionKey"] ?? "DatabaseKey123456789012345678901234567890",
                "string", "Database encryption key", true, true, true, now, systemUserId),
                
            CreateConfiguration("Security", "EncryptionKeys_CookieEncryptionKey", 
                encryptionSection["CookieEncryptionKey"] ?? "CookieKey123456789012345678901234567890",
                "string", "Cookie encryption key", true, true, true, now, systemUserId),
                
            CreateConfiguration("Security", "EncryptionKeys_FileEncryptionKey", 
                encryptionSection["FileEncryptionKey"] ?? "FileKey123456789012345678901234567890",
                "string", "File encryption key", true, true, true, now, systemUserId),
                
            CreateConfiguration("Security", "EncryptionKeys_AutoRotateKeys", 
                encryptionSection["AutoRotateKeys"] ?? "true",
                "bool", "Automatically rotate encryption keys", false, false, false, now, systemUserId),
                
            CreateConfiguration("Security", "EncryptionKeys_KeyRotationDays", 
                encryptionSection["KeyRotationDays"] ?? "90",
                "int", "Key rotation interval in days", false, false, false, now, systemUserId)
        });

        // Session Security
        var sessionSection = securitySection.GetSection("SessionSecurity");
        configurations.AddRange(new[]
        {
            CreateConfiguration("Security", "SessionSecurity_SessionTimeout", 
                sessionSection["SessionTimeout"] ?? "00:30:00",
                "timespan", "Session timeout duration", false, false, false, now, systemUserId),
                
            CreateConfiguration("Security", "SessionSecurity_SlidingExpiration", 
                sessionSection["SlidingExpiration"] ?? "true",
                "bool", "Enable sliding session expiration", false, false, false, now, systemUserId),
                
            CreateConfiguration("Security", "SessionSecurity_RequireSecureCookies", 
                sessionSection["RequireSecureCookies"] ?? "true",
                "bool", "Require secure cookies (HTTPS only)", false, false, false, now, systemUserId),
                
            CreateConfiguration("Security", "SessionSecurity_RequireHttpOnlyCookies", 
                sessionSection["RequireHttpOnlyCookies"] ?? "true",
                "bool", "Require HTTP-only cookies", false, false, false, now, systemUserId),
                
            CreateConfiguration("Security", "SessionSecurity_SameSiteMode", 
                sessionSection["SameSiteMode"] ?? "Strict",
                "string", "Cookie SameSite mode", false, false, false, now, systemUserId),
                
            CreateConfiguration("Security", "SessionSecurity_EnableDeviceTracking", 
                sessionSection["EnableDeviceTracking"] ?? "true",
                "bool", "Enable device fingerprinting", false, false, false, now, systemUserId),
                
            CreateConfiguration("Security", "SessionSecurity_EnableGeoLocationTracking", 
                sessionSection["EnableGeoLocationTracking"] ?? "false",
                "bool", "Enable geographical location tracking", false, false, false, now, systemUserId)
        });

        // HTTPS Configuration
        var httpsSection = securitySection.GetSection("Https");
        configurations.AddRange(new[]
        {
            CreateConfiguration("Security", "Https_RequireHttps", 
                httpsSection["RequireHttps"] ?? "true",
                "bool", "Require HTTPS for all requests", true, false, false, now, systemUserId),
                
            CreateConfiguration("Security", "Https_RedirectHttpToHttps", 
                httpsSection["RedirectHttpToHttps"] ?? "true",
                "bool", "Redirect HTTP requests to HTTPS", false, false, false, now, systemUserId),
                
            CreateConfiguration("Security", "Https_HttpsPort", 
                httpsSection["HttpsPort"] ?? "443",
                "int", "HTTPS port number", false, false, false, now, systemUserId),
                
            CreateConfiguration("Security", "Https_EnableHsts", 
                httpsSection["EnableHsts"] ?? "true",
                "bool", "Enable HTTP Strict Transport Security", false, false, false, now, systemUserId),
                
            CreateConfiguration("Security", "Https_HstsMaxAge", 
                httpsSection["HstsMaxAge"] ?? "365.00:00:00",
                "timespan", "HSTS max age", false, false, false, now, systemUserId),
                
            CreateConfiguration("Security", "Https_HstsIncludeSubdomains", 
                httpsSection["HstsIncludeSubdomains"] ?? "true",
                "bool", "Include subdomains in HSTS", false, false, false, now, systemUserId)
        });

        await Task.CompletedTask;
    }
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
                
            CreateConfiguration("SystemSettings", "DateFormat", 
                systemSection["DateFormat"] ?? "yyyy-MM-dd",
                "string", "Default date format", false, false, false, now, systemUserId),
                
            CreateConfiguration("SystemSettings", "TimeFormat", 
                systemSection["TimeFormat"] ?? "HH:mm:ss",
                "string", "Default time format", false, false, false, now, systemUserId),
                
            CreateConfiguration("SystemSettings", "DefaultPageSize", 
                systemSection["DefaultPageSize"] ?? "20",
                "int", "Default page size for paginated results", false, false, false, now, systemUserId),
                
            CreateConfiguration("SystemSettings", "MaxPageSize", 
                systemSection["MaxPageSize"] ?? "100",
                "int", "Maximum page size for paginated results", false, false, false, now, systemUserId),
                
            CreateConfiguration("SystemSettings", "SessionTimeout", 
                systemSection["SessionTimeout"] ?? "00:30:00",
                "timespan", "User session timeout", false, false, false, now, systemUserId),
                
            CreateConfiguration("SystemSettings", "EnableAuditLogging", 
                systemSection["EnableAuditLogging"] ?? "true",
                "bool", "Enable audit logging", false, false, false, now, systemUserId),
                
            CreateConfiguration("SystemSettings", "EnableRealTimeNotifications", 
                systemSection["EnableRealTimeNotifications"] ?? "true",
                "bool", "Enable real-time notifications", false, false, false, now, systemUserId)
        });

        await Task.CompletedTask;
    }

    private static async Task SeedFrSystemConfigurationAsync(List<SystemConfiguration> configurations, IConfiguration configuration, DateTime now, int? systemUserId)
    {
        var frSystemSection = configuration.GetSection("FRSystem");
        
        configurations.AddRange(new[]
        {
            CreateConfiguration("FRSystem", "GraphQLEndpoint", 
                frSystemSection["GraphQLEndpoint"] ?? "https://your-fr-system.com/graphql",
                "string", "Face Recognition system GraphQL endpoint", false, false, false, now, systemUserId),
                
            CreateConfiguration("FRSystem", "ApiKey", 
                frSystemSection["ApiKey"] ?? "",
                "string", "Face Recognition system API key", false, true, true, now, systemUserId),
                
            CreateConfiguration("FRSystem", "WebhookSecret", 
                frSystemSection["WebhookSecret"] ?? "",
                "string", "Face Recognition system webhook secret", false, true, true, now, systemUserId),
                
            CreateConfiguration("FRSystem", "Timeout", 
                frSystemSection["Timeout"] ?? "00:00:30",
                "timespan", "Request timeout for Face Recognition system", false, false, false, now, systemUserId),
                
            CreateConfiguration("FRSystem", "RetryCount", 
                frSystemSection["RetryCount"] ?? "3",
                "int", "Retry count for Face Recognition system requests", false, false, false, now, systemUserId),
                
            CreateConfiguration("FRSystem", "HealthCheckInterval", 
                frSystemSection["HealthCheckInterval"] ?? "00:01:00",
                "timespan", "Health check interval for Face Recognition system", false, false, false, now, systemUserId)
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
                
            CreateConfiguration("Application", "ApplicationVersion", 
                "1.0.0",
                "string", "Application version", false, false, false, now, systemUserId),
                
            CreateConfiguration("Application", "Environment", 
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                "string", "Application environment", false, false, false, now, systemUserId),
                
            CreateConfiguration("Application", "MaintenanceMode", 
                "false",
                "bool", "Enable maintenance mode", false, false, false, now, systemUserId),
                
            CreateConfiguration("Application", "MaintenanceMessage", 
                "The system is currently under maintenance. Please try again later.",
                "string", "Maintenance mode message", false, false, false, now, systemUserId),
                
            CreateConfiguration("Application", "EnableDebugMode", 
                "false",
                "bool", "Enable debug mode", false, false, false, now, systemUserId),
                
            CreateConfiguration("Application", "EnableSwagger", 
                "true",
                "bool", "Enable Swagger documentation", false, false, false, now, systemUserId),
                
            CreateConfiguration("Application", "MaxConcurrentUsers", 
                "1000",
                "int", "Maximum concurrent users", false, false, false, now, systemUserId),
                
            CreateConfiguration("Application", "EnableFeatureFlags", 
                "true",
                "bool", "Enable feature flag system", false, false, false, now, systemUserId),
                
            CreateConfiguration("Application", "CacheExpirationMinutes", 
                "30",
                "int", "Default cache expiration in minutes", false, false, false, now, systemUserId)
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
            CreateConfiguration("JWT", "SecretKey", "ThisIsAVeryLongSecretKeyForJWTTokenGenerationThatMustBeAtLeast256BitsLong", "string", "Secret key for JWT signing", true, true, true, now, systemUserId),
            CreateConfiguration("JWT", "Issuer", "VisitorManagementSystem", "string", "JWT token issuer", false, false, false, now, systemUserId),
            CreateConfiguration("JWT", "Audience", "VMS-Users", "string", "JWT token audience", false, false, false, now, systemUserId),
            CreateConfiguration("JWT", "ExpiryInMinutes", "15", "int", "Token expiry time in minutes", false, false, false, now, systemUserId),
            CreateConfiguration("Security", "Https_RequireHttps", "true", "bool", "Require HTTPS for all requests", true, false, false, now, systemUserId),
            CreateConfiguration("Database", "CommandTimeout", "30", "int", "Database command timeout", false, false, false, now, systemUserId),
            CreateConfiguration("Logging", "LogLevel", "Information", "string", "Global log level", false, false, false, now, systemUserId),
            CreateConfiguration("Application", "ApplicationName", "Visitor Management System", "string", "Application name", false, false, false, now, systemUserId)
        });
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
            DefaultValue = value,
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
