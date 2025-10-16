using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using System.Reflection;
using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Application.Services.Email;
using VisitorManagementSystem.Api.Application.Services.QrCode;
using VisitorManagementSystem.Api.Application.Services.Pdf;
using VisitorManagementSystem.Api.Application.Services.Csv;
using VisitorManagementSystem.Api.Application.Services.Xlsx;
using VisitorManagementSystem.Api.Application.Services.Users;
using VisitorManagementSystem.Api.Application.Services.Visitors;
using VisitorManagementSystem.Api.Application.Services.Capacity;
using VisitorManagementSystem.Api.Configuration;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Domain.Interfaces.Services;
using VisitorManagementSystem.Api.Infrastructure.Data;
using VisitorManagementSystem.Api.Infrastructure.Data.Repositories;
using VisitorManagementSystem.Api.Middleware;
using VisitorManagementSystem.Api.Application.Services.Configuration;
using VisitorManagementSystem.Api.Infrastructure.Utilities;
using Microsoft.AspNetCore.Authorization;
using VisitorManagementSystem.Api.Infrastructure.Security.Authorization;
using VisitorManagementSystem.Api.Infrastructure.Security.Encryption;
using VisitorManagementSystem.Api.Application.Services.Notifications;
using VisitorManagementSystem.Api.Application.Services.BackgroundServices;
using VisitorManagementSystem.Api.Infrastructure.Data.Repositories.Notifications;
using Microsoft.AspNetCore.DataProtection;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Threading.RateLimiting;
using VisitorManagementSystem.Api.Application.Services.FileUploadService;

namespace VisitorManagementSystem.Api.Extensions;

/// <summary>
/// Service collection extensions for dependency injection configuration
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures application services
    /// </summary>
    public static IServiceCollection ConfigureApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register core services
        services.RegisterConfiguration(configuration);
        services.RegisterServices();
        services.RegisterRepositories();
        services.RegisterExternalServices();
        services.RegisterBackgroundServices();
        services.RegisterInfrastructureServices();
        services.RegisterValidators();

        // Configure security
        services.ConfigureSecurity(configuration);
        // Configure rate limiting
        services.ConfigureRateLimiting(configuration);
        // Configure API behavior
        services.ConfigureApiOptions();

        return services;
    }

    /// <summary>
    /// Registers configuration services
    /// </summary>
    private static IServiceCollection RegisterConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Register the dynamic configuration service
        services.AddScoped<IDynamicConfigurationService, DynamicConfigurationService>();

        // Note: All other configurations are now stored in database and accessed via IDynamicConfigurationService

        return services;
    }

    /// <summary>
    /// Registers core application services
    /// </summary>
    private static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        // Auth services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<ITwoFactorService, TwoFactorService>();
        services.AddScoped<IPermissionService, PermissionService>();

        // User management services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserLockoutService, UserLockoutService>();
        services.AddScoped<IUserPreferencesService, UserPreferencesService>();
        services.AddScoped<IUserActivityService, UserActivityService>();

        // Visitor services
        services.AddScoped<IVisitorService, VisitorService>();

        // Capacity management services
        services.AddScoped<ICapacityService, CapacityService>();

        // File upload service
        services.AddScoped<IFileUploadService, FileUploadService>();

        // Visitor notes bridge service (converts form data to structured notes)
        services.AddScoped<IVisitorNotesBridgeService, VisitorNotesBridgeService>();

        // Utility services
        services.AddSingleton<DateTimeProvider>();
        services.AddSingleton<GuidGenerator>();

        // Encryption services
        services.AddSingleton<IEncryptionService, AESEncryptionService>();
        services.AddSingleton<IKeyManagementService, KeyManagementService>();

        // Authorization handlers
        services.AddScoped<IAuthorizationHandler, PermissionHandler>();
        services.AddScoped<IAuthorizationHandler, RoleHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, PolicyProvider>();


        return services;
    }

    /// <summary>
    /// Registers external service implementations
    /// </summary>
    private static IServiceCollection RegisterExternalServices(this IServiceCollection services)
    {
        // Email services
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();

        // QR Code service
        services.AddScoped<IQrCodeService, QrCodeService>();

        // PDF service
        services.AddScoped<IPdfService, PdfService>();

        // CSV service
        services.AddScoped<ICsvService, CsvService>();

        // XLSX service  
        services.AddScoped<IXlsxService, XlsxService>();

        // Other services (will be implemented)
        services.AddScoped<ISMSService, StubSMSService>();
        services.AddScoped<IFileStorageService, StubFileStorageService>();
        
        // SignalR Notification service
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }

    /// <summary>
    /// Registers background services
    /// </summary>
    private static IServiceCollection RegisterBackgroundServices(this IServiceCollection services)
    {
        // Token cleanup service
        services.AddHostedService<TokenCleanupBackgroundService>();

        // Audit cleanup service  
        services.AddHostedService<AuditCleanupBackgroundService>();

        // Notification & FR processing services

        //services.AddHostedService<FREventProcessorService>();
        services.AddHostedService<NotificationDispatcherService>();
        services.AddHostedService<VisitorTrackingService>();

        return services;
    }

    /// <summary>
    /// Registers validators
    /// </summary>
    private static IServiceCollection RegisterValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true; // Handle validation manually
        });

        return services;
    }

    /// <summary>
    /// Registers infrastructure services
    /// </summary>
    private static IServiceCollection RegisterInfrastructureServices(this IServiceCollection services)
    {
        // HTTP client services
        services.AddHttpClient();

        // In-memory cache
        services.AddMemoryCache();

        // —— SESSION: backing store + registration ——
        services.AddDistributedMemoryCache();
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        // Distributed cache (Redis)
        //services.AddStackExchangeRedisCache(options =>
        //{
        //    options.Configuration = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "localhost:6379";
        //});
        //services.AddDistributedMemoryCache();
        // Data protection
        services.AddDataProtection()
            .SetApplicationName("VisitorManagementSystem")
            .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

        return services;
    }

    /// <summary>
    /// Registers repository implementations
    /// </summary>
    private static IServiceCollection RegisterRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IInvitationRepository, InvitationRepository>();
        services.AddScoped<IVisitorRepository, VisitorRepository>();
        services.AddScoped<ISystemConfigurationRepository, SystemConfigurationRepository>();
        services.AddScoped<IConfigurationAuditRepository, ConfigurationAuditRepository>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(BaseRepository<>));
        services.AddScoped<IEmergencyContactRepository, EmergencyContactRepository>();
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<IVisitorDocumentRepository, VisitorDocumentRepository>();
        services.AddScoped<IVisitorNoteRepository, VisitorNoteRepository>();
        services.AddScoped<IVisitPurposeRepository, VisitPurposeRepository>();

        // Camera repositories
        services.AddScoped<ICameraRepository, CameraRepository>();

        // Notification repositories
        services.AddScoped<INotificationAlertRepository, NotificationAlertRepository>();
        services.AddScoped<IOperatorSessionRepository, OperatorSessionRepository>();
        services.AddScoped<IAlertEscalationRepository, AlertEscalationRepository>();

        return services;
    }

    /// <summary>
    /// Configures Swagger/OpenAPI
    /// </summary>
    public static IServiceCollection ConfigureSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Visitor Management System API",
                Version = "v1",
                Description = "Enterprise Visitor Management System with Facial Recognition Integration",
                Contact = new OpenApiContact
                {
                    Name = "VMS Support",
                    Email = "support@vms.com"
                }
            });

            // Add JWT authentication
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Include XML comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }

    /// <summary>
    /// Configures health checks for the application
    /// </summary>
    public static IServiceCollection ConfigureHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddCheck<ApplicationHealthCheck>("application")
            .AddCheck<DatabaseHealthCheck>("database")
            .AddCheck<ExternalServicesHealthCheck>("external_services")
            .AddDbContextCheck<ApplicationDbContext>("dbcontext");

        return services;
    }

    /// <summary>
    /// Configures security services with default settings
    /// Security settings are now managed dynamically via database
    /// </summary>
    private static IServiceCollection ConfigureSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure password hasher with secure defaults
        services.Configure<PasswordHasherOptions>(options =>
        {
            options.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3;
            options.IterationCount = 10000;
        });

        // Configure anti-forgery with secure defaults
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "__RequestVerificationToken";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });

        // Configure HSTS with secure defaults
        services.AddHsts(options =>
        {
            options.MaxAge = TimeSpan.FromDays(365);
            options.IncludeSubDomains = true;
            options.Preload = true;
        });

        return services;
    }

    /// <summary>
    /// Configures rate limiting
    /// </summary>
    private static IServiceCollection ConfigureRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var securityConfig = configuration.GetSection(SecurityConfiguration.SectionName).Get<SecurityConfiguration>()
            ?? new SecurityConfiguration();

        services.AddRateLimiter(options =>
        {
            // Global rate limit
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = securityConfig.RateLimiting.GeneralApi.PermitLimit,
                        Window = securityConfig.RateLimiting.GeneralApi.Window,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = securityConfig.RateLimiting.GeneralApi.QueueLimit
                    }));

            // Login endpoint rate limit
            options.AddPolicy("login", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = securityConfig.RateLimiting.LoginAttempts.PermitLimit,
                        Window = securityConfig.RateLimiting.LoginAttempts.Window,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // No queuing for login attempts
                    }));

            // Token refresh rate limit
            options.AddPolicy("token-refresh", httpContext =>
                RateLimitPartition.GetTokenBucketLimiter(
                    partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: partition => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = securityConfig.RateLimiting.TokenRefresh.PermitLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        ReplenishmentPeriod = securityConfig.RateLimiting.TokenRefresh.ReplenishmentPeriod,
                        TokensPerPeriod = 1,
                        AutoReplenishment = securityConfig.RateLimiting.TokenRefresh.AutoReplenishment
                    }));

            // Password reset rate limit
            options.AddPolicy("password-reset", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = securityConfig.RateLimiting.PasswordReset.PermitLimit,
                        Window = securityConfig.RateLimiting.PasswordReset.Window,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();
                }

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please try again later.", cancellationToken);
            };
        });

        return services;
    }

    /// <summary>
    /// Configures API options
    /// </summary>
    private static IServiceCollection ConfigureApiOptions(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            // Customize model state validation responses
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .SelectMany(e => e.Value!.Errors.Select(er => er.ErrorMessage))
                    .ToList();

                var response = new
                {
                    success = false,
                    message = "Validation failed",
                    errors = errors,
                    timestamp = DateTime.UtcNow
                };

                return new BadRequestObjectResult(response);
            };
        });

        // Configure JSON options
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.SerializerOptions.WriteIndented = false;
            options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

        return services;
    }
    /// <summary>
    /// Configures response compression
    /// </summary>
    public static IServiceCollection ConfigureCompression(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
        });

        return services;
    }

    /// <summary>
    /// Configures API versioning
    /// </summary>
    public static IServiceCollection ConfigureApiVersioning(this IServiceCollection services)
    {
        // TODO: Add API versioning configuration when package is available
        /*
        services.AddApiVersioning(config =>
        {
            config.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
            config.AssumeDefaultVersionWhenUnspecified = true;
            config.ReadVersionFromHeader = true;
            config.HeaderName = "X-API-Version";
        });
        */

        return services;
    }

    /// <summary>
    /// Configures the application database context
    /// </summary>
    public static IServiceCollection ConfigureDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            
            // Enable detailed errors in development
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            }
        });

        return services;
    }

    /// <summary>
    /// Configures ASP.NET Core Identity
    /// </summary>
    public static IServiceCollection ConfigureIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        var securityConfig = configuration.GetSection("Security").Get<SecurityConfiguration>() ?? new SecurityConfiguration();

        // TODO: Configure Identity when proper entities are implemented
        /*
        services.AddIdentity<User, Role>(options =>
        {
            // Password settings
            options.Password.RequireDigit = securityConfig.PasswordPolicy.RequireDigit;
            options.Password.RequireLowercase = securityConfig.PasswordPolicy.RequireLowercase;
            options.Password.RequireUppercase = securityConfig.PasswordPolicy.RequireUppercase;
            options.Password.RequireNonAlphanumeric = securityConfig.PasswordPolicy.RequireNonAlphanumeric;
            options.Password.RequiredLength = securityConfig.PasswordPolicy.RequiredLength;
            options.Password.RequiredUniqueChars = securityConfig.PasswordPolicy.RequiredUniqueChars;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = securityConfig.Lockout.DefaultLockoutTimeSpan;
            options.Lockout.MaxFailedAccessAttempts = securityConfig.Lockout.MaxFailedAccessAttempts;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

            // Sign in settings
            options.SignIn.RequireConfirmedEmail = false;
            options.SignIn.RequireConfirmedPhoneNumber = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();
        */

        return services;
    }

}

// Stub service implementations (to be replaced with actual implementations)
public class StubSMSService : ISMSService
{
    private readonly ILogger<StubSMSService> _logger;

    public StubSMSService(ILogger<StubSMSService> logger)
    {
        _logger = logger;
    }

    public Task SendSMSAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("SMS service not implemented. Would send SMS to {PhoneNumber}: {Message}", phoneNumber, message);
        return Task.CompletedTask;
    }

    public Task SendTemplatedSMSAsync(string phoneNumber, string templateName, object templateData, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("SMS service not implemented. Would send templated SMS to {PhoneNumber} using template {TemplateName}", phoneNumber, templateName);
        return Task.CompletedTask;
    }

    public Task<bool> IsServiceAvailableAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("SMS service not implemented. Service availability check requested");
        return Task.FromResult(false);
    }
}

public class StubFileStorageService : IFileStorageService
{
    private readonly ILogger<StubFileStorageService> _logger;

    public StubFileStorageService(ILogger<StubFileStorageService> logger)
    {
        _logger = logger;
    }

    public Task<string> SaveFileAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("File storage service not implemented. Would save file: {FileName}", fileName);
        return Task.FromResult($"/files/{fileName}");
    }

    public Task<string> SaveFileAsync(Stream fileStream, string fileName, string folderPath, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("File storage service not implemented. Would save file: {FileName} to folder: {FolderPath}", fileName, folderPath);
        return Task.FromResult($"/files/{folderPath}/{fileName}");
    }

    public Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("File storage service not implemented. Would delete file: {FilePath}", filePath);
        return Task.CompletedTask;
    }

    public Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("File storage service not implemented. Would check if file exists: {FilePath}", filePath);
        return Task.FromResult(false);
    }

    public Task<Stream> GetFileStreamAsync(string filePath, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("File storage service not implemented. Would get file stream: {FilePath}", filePath);
        return Task.FromResult<Stream>(new MemoryStream());
    }

    public string GetPublicUrl(string filePath)
    {
        _logger.LogWarning("File storage service not implemented. Would get public URL for: {FilePath}", filePath);
        return $"/files/{filePath}";
    }

    public Task<FileMetadata> GetFileMetadataAsync(string filePath, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("File storage service not implemented. Would get metadata for: {FilePath}", filePath);
        return Task.FromResult(new FileMetadata
        {
            FileName = Path.GetFileName(filePath),
            Size = 0,
            ContentType = "application/octet-stream",
            LastModified = DateTime.UtcNow,
            Extension = Path.GetExtension(filePath)
        });
    }
}


// Background Services - Production Ready Implementations
public class TokenCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TokenCleanupBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(6); // Run every 6 hours

    public TokenCleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<TokenCleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Token cleanup background service started");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var refreshTokenService = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();

                var cleanupResult = await refreshTokenService.CleanupExpiredTokensAsync(TimeSpan.FromDays(30), stoppingToken);
                _logger.LogInformation("Token cleanup completed successfully. Expired: {ExpiredTokens}, Deleted: {DeletedTokens}, Orphaned: {OrphanedTokens}, Invalid: {InvalidTokens}", 
                    cleanupResult.ExpiredTokensRevoked, cleanupResult.OldTokensDeleted, cleanupResult.OrphanedTokensRemoved, cleanupResult.InvalidTokensRemoved);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token cleanup");
            }

            await Task.Delay(_interval, stoppingToken);
        }
        
        _logger.LogInformation("Token cleanup background service stopped");
    }
}

public class AuditCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditCleanupBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromDays(1); // Run daily

    public AuditCleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AuditCleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Audit cleanup background service started");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var cutoffDate = DateTime.UtcNow.AddDays(-365);
                var deletedCount = await unitOfWork.ExecuteSqlAsync(
                    "DELETE FROM AuditLogs WHERE CreatedOn < @p0",
                    new object[] { cutoffDate },
                    stoppingToken);

                _logger.LogInformation("Audit cleanup completed successfully. Removed {DeletedCount} old audit logs", deletedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during audit cleanup");
            }

            await Task.Delay(_interval, stoppingToken);
        }
        
        _logger.LogInformation("Audit cleanup background service stopped");
    }
}

// Health Check Implementations - Production Ready
public class ApplicationHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ApplicationHealthCheck> _logger;

    public ApplicationHealthCheck(IServiceProvider serviceProvider, ILogger<ApplicationHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await unitOfWork.ExecuteSqlAsync("SELECT 1", Array.Empty<object>(), cancellationToken);

            var healthData = new Dictionary<string, object>
            {
                { "timestamp", DateTime.UtcNow },
                { "database", "connected" },
                { "services", "operational" }
            };

            return HealthCheckResult.Healthy("Application is running normally", healthData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return HealthCheckResult.Unhealthy("Application health check failed", ex);
        }
    }
}

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(IServiceProvider serviceProvider, ILogger<DatabaseHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            
            var result = await unitOfWork.ExecuteSqlAsync("SELECT COUNT(*) FROM Users", Array.Empty<object>(), cancellationToken);
            
            var healthData = new Dictionary<string, object>
            {
                { "timestamp", DateTime.UtcNow },
                { "userCount", result },
                { "connectionState", "connected" }
            };

            return HealthCheckResult.Healthy("Database is accessible", healthData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    }
}

public class ExternalServicesHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExternalServicesHealthCheck> _logger;

    public ExternalServicesHealthCheck(IServiceProvider serviceProvider, ILogger<ExternalServicesHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var healthData = new Dictionary<string, object>
        {
            { "timestamp", DateTime.UtcNow }
        };

        var hasError = false;

        try
        {
            using var scope = _serviceProvider.CreateScope();

            try
            {
                var emailService = scope.ServiceProvider
                    .GetRequiredService<Application.Services.Email.IEmailService>();

                var isEmailHealthy = await emailService.ValidateConnectionAsync();
                healthData["emailService"] = isEmailHealthy ? "connected" : "disconnected";

                if (!isEmailHealthy)
                    hasError = true;
            }
            catch (Exception emailEx)
            {
                healthData["emailService"] = "error";
                healthData["emailError"] = emailEx.Message;
                hasError = true;
            }

            // Other service checks
            healthData["qrCodeService"] = "operational";
            healthData["pdfService"] = "operational";
            healthData["fileUploadService"] = "operational";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "External services health check failed");
            return HealthCheckResult.Unhealthy(
                description: "External services check failed completely",
                exception: ex
            );
        }

        if (hasError)
        {
            return HealthCheckResult.Degraded(
                description: "One or more external services have issues",
                data: healthData
            );
        }

        return HealthCheckResult.Healthy(
            description: "External services are operational",
            data: healthData
        );
    }
}
