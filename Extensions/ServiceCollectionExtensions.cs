// Extensions/ServiceCollectionExtensions.cs
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Configuration;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Infrastructure.Data;
using VisitorManagementSystem.Api.Infrastructure.Data.Repositories;
using VisitorManagementSystem.Api.Infrastructure.Security.Authorization;
using VisitorManagementSystem.Api.Infrastructure.Security.Authentication;
using VisitorManagementSystem.Api.Infrastructure.Security.Encryption;
using VisitorManagementSystem.Api.Infrastructure.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Authentication;
using StackExchange.Redis;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to register application services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all application services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection RegisterApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register configuration sections
        services.RegisterConfiguration(configuration);

        // Register core services
        services.RegisterRepositories();
        services.RegisterBusinessServices();
        services.RegisterSecurityServices();
        services.RegisterInfrastructureServices();
        services.RegisterValidators();

        // Register external service interfaces
        services.RegisterExternalServices();

        // Register background services
        services.RegisterBackgroundServices();

        // Configure API behavior
        services.ConfigureApiOptions();

        // Configure security
        services.ConfigureSecurity(configuration);

        // Configure rate limiting
        services.ConfigureRateLimiting(configuration);

        return services;
    }

    /// <summary>
    /// Registers configuration sections
    /// </summary>
    private static IServiceCollection RegisterConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtConfiguration>(configuration.GetSection(JwtConfiguration.SectionName));
        services.Configure<SecurityConfiguration>(configuration.GetSection(SecurityConfiguration.SectionName));
        services.Configure<DatabaseConfiguration>(configuration.GetSection(DatabaseConfiguration.SectionName));
        services.Configure<LoggingConfiguration>(configuration.GetSection(LoggingConfiguration.SectionName));

        // Validate critical configurations on startup
        services.AddOptions<JwtConfiguration>()
            .Bind(configuration.GetSection(JwtConfiguration.SectionName))
            .ValidateDataAnnotations()
            .Validate(config => !string.IsNullOrWhiteSpace(config.SecretKey), "JWT SecretKey is required")
            .Validate(config => config.SecretKey.Length >= 32, "JWT SecretKey must be at least 256 bits (32 characters)")
            .Validate(config => !string.IsNullOrWhiteSpace(config.Issuer), "JWT Issuer is required")
            .Validate(config => !string.IsNullOrWhiteSpace(config.Audience), "JWT Audience is required")
            .ValidateOnStart();

        services.AddOptions<SecurityConfiguration>()
            .Bind(configuration.GetSection(SecurityConfiguration.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    /// <summary>
    /// Registers repository services
    /// </summary>
    private static IServiceCollection RegisterRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(BaseRepository<>));

        return services;
    }

    /// <summary>
    /// Registers business logic services
    /// </summary>
    private static IServiceCollection RegisterBusinessServices(this IServiceCollection services)
    {
        // Authentication services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IUserLockoutService, UserLockoutService>();

        // Utility services
        services.AddSingleton<DateTimeProvider>();
        services.AddSingleton<GuidGenerator>();

        return services;
    }

    /// <summary>
    /// Registers security services
    /// </summary>
    private static IServiceCollection RegisterSecurityServices(this IServiceCollection services)
    {
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
        services.AddDistributedMemoryCache();
        // Data protection
        services.AddDataProtection()
            .SetApplicationName("VisitorManagementSystem")
            .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

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
    /// Registers external service interfaces (will be implemented in later chunks)
    /// </summary>
    private static IServiceCollection RegisterExternalServices(this IServiceCollection services)
    {
        // Email service placeholder
        services.AddScoped<IEmailService>(provider =>
            throw new NotImplementedException("Email service will be implemented in later chunks"));

        // SMS service placeholder
        services.AddScoped<ISMSService>(provider =>
            throw new NotImplementedException("SMS service will be implemented in later chunks"));

        // File storage service placeholder
        services.AddScoped<IFileStorageService>(provider =>
            throw new NotImplementedException("File storage service will be implemented in later chunks"));

        // Notification service placeholder
        services.AddScoped<INotificationService>(provider =>
            throw new NotImplementedException("Notification service will be implemented in later chunks"));

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
    /// Configures security settings
    /// </summary>
    private static IServiceCollection ConfigureSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        var securityConfig = configuration.GetSection(SecurityConfiguration.SectionName).Get<SecurityConfiguration>()
            ?? new SecurityConfiguration();

        // Configure password hasher
        services.Configure<PasswordHasherOptions>(options =>
        {
            options.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3;
            options.IterationCount = 10000;
        });

        // Configure anti-forgery
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "__RequestVerificationToken";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });

        // Configure HSTS
        if (securityConfig.Https.EnableHsts)
        {
            services.AddHsts(options =>
            {
                options.MaxAge = securityConfig.Https.HstsMaxAge;
                options.IncludeSubDomains = securityConfig.Https.HstsIncludeSubdomains;
                options.Preload = securityConfig.Https.HstsPreload;
            });
        }

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
    /// Configures health checks
    /// </summary>
    public static IServiceCollection ConfigureHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("database_status", failureStatus: HealthStatus.Unhealthy)
            .AddCheck<ApplicationHealthCheck>("application", failureStatus: HealthStatus.Unhealthy)
            .AddCheck<SecurityHealthCheck>("security", failureStatus: HealthStatus.Unhealthy);
        return services;
    }


    /// <summary>
    /// Configures Swagger/OpenAPI documentation
    /// </summary>
    public static IServiceCollection ConfigureSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
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
                    Email = "support@vms.com",
                    Url = new Uri("https://vms.com/support")
                },
                License = new OpenApiLicense
                {
                    Name = "Proprietary",
                    Url = new Uri("https://vms.com/license")
                },
                TermsOfService = new Uri("https://vms.com/terms")
            });

            // Add JWT authentication
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Note: Tokens are managed via secure HTTP-only cookies.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT"
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

            // Include XML comments if available
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }
}

// Background Services (placeholders for now)
public class TokenCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TokenCleanupBackgroundService> _logger;

    public TokenCleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<TokenCleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var refreshTokenService = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();

                await refreshTokenService.CleanupExpiredTokensAsync(TimeSpan.FromDays(30), stoppingToken);
                _logger.LogInformation("Token cleanup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token cleanup");
            }

            await Task.Delay(TimeSpan.FromHours(6), stoppingToken); // Run every 6 hours
        }
    }
}

public class AuditCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditCleanupBackgroundService> _logger;

    public AuditCleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AuditCleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var cutoffDate = DateTime.UtcNow.AddDays(-365); // Keep 1 year of audit logs
                var deletedCount = await unitOfWork.ExecuteSqlAsync(
                    "DELETE FROM AuditLogs WHERE CreatedOn < @p0",
                    new object[] { cutoffDate },
                    stoppingToken);

                _logger.LogInformation("Audit cleanup completed. Deleted {Count} old records", deletedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during audit cleanup");
            }

            await Task.Delay(TimeSpan.FromDays(1), stoppingToken); // Run daily
        }
    }
}

// Health Check Classes (placeholders)
public class ApplicationHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // Implement application-specific health checks
        return Task.FromResult(HealthCheckResult.Healthy("Application is running"));
    }
}

public class SecurityHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // Implement security-specific health checks
        return Task.FromResult(HealthCheckResult.Healthy("Security systems operational"));
    }
}

// Service interfaces (placeholders that will be implemented in later chunks)
public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}

public interface ISMSService
{
    Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
}

public interface INotificationService
{
    Task SendNotificationAsync(string message, CancellationToken cancellationToken = default);
}