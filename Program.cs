// Program.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using FluentValidation;
using VisitorManagementSystem.Api.Extensions;
using VisitorManagementSystem.Api.Infrastructure.Data;
using VisitorManagementSystem.Api.Middleware;
using System.Reflection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Infrastructure.Security.Authorization;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;
using VisitorManagementSystem.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using VisitorManagementSystem.Api.Infrastructure.Security.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;
using VisitorManagementSystem.Api.Infrastructure.Configuration;
using VisitorManagementSystem.Api.Application.Services.Licensing;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddVmsProductionConfiguration(builder.Environment);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure JSON serialization for camelCase
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
        
        // Handle DateTime formatting consistently
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        
        // Allow reading numbers from strings (helpful for form data)
        options.JsonSerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString;
    });

// Database Configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("VisitorManagementSystem.Api"));

    if (builder.Configuration.GetValue("Database:EnableDetailedErrors", builder.Environment.IsDevelopment()))
    {
        options.EnableDetailedErrors();
    }

    if (builder.Configuration.GetValue<bool>("Database:EnableSensitiveDataLogging"))
    {
        options.EnableSensitiveDataLogging();
    }

    // Suppress MARS warnings in production
    if (!builder.Environment.IsDevelopment())
    {
        options.ConfigureWarnings(warnings =>
            warnings.Ignore(SqlServerEventId.SavepointsDisabledBecauseOfMARS));
    }
});


// Authentication & Authorization
// Note: JWT configuration will be loaded from database after first setup
// For initial setup, we'll use hardcoded values that will be seeded into the database

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    
    // Configure token validation parameters
    // This will be dynamically loaded from database in production
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(
            builder.Configuration["JWT:SecretKey"] ?? 
            throw new InvalidOperationException("JWT:SecretKey configuration is required for production"))),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JWT:Issuer"] ?? "VisitorManagementSystem",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT:Audience"] ?? "VMS-Users",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        NameClaimType = ClaimTypes.NameIdentifier // IMPORTANT: This fixes GetCurrentUserId()
    };

    // ✅ ENHANCED: Improved cookie token extraction and validation
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

            // Try Authorization header first (standard approach)
            var authorization = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
            {
                context.Token = authorization.Substring("Bearer ".Length).Trim();
                logger.LogDebug("🔑 Token from Authorization header");
                return Task.CompletedTask;
            }

            // Try access_token cookie (for web app)
            var token = context.Request.Cookies["access_token"];
            if (!string.IsNullOrEmpty(token))
            {
                context.Token = token;
                logger.LogDebug("🔑 Token from access_token cookie");
            }
            else
            {
                logger.LogDebug("❌ No token found in headers or cookies");
            }

            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            logger.LogDebug("✅ Token validated for user: {UserId}", userId);
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("❌ JWT Authentication failed: {Error}", context.Exception.Message);
            
            // Don't include the exception details in the response for security
            context.NoResult();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            
            var response = ApiResponseDto<object>.ErrorResponse(
                "Authentication failed",
                "Unauthorized",
                context.HttpContext.TraceIdentifier
            );
            
            return context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogDebug("🚪 JWT Challenge triggered for path: {Path}", context.Request.Path);
            
            // Custom challenge response
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            
            var response = ApiResponseDto<object>.ErrorResponse(
                "Authentication required",
                "Unauthorized", 
                context.HttpContext.TraceIdentifier
            );
            
            return context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
        }
    };
})
.AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
    ApiKeyAuthenticationSchemeOptions.DefaultScheme, null);

// use of Api Key above as below 
// Uses JWT (default)
// [Authorize]
// public IActionResult GetVisitors() { }

// // Uses API Key only
// [Authorize(AuthenticationSchemes = ApiKeyAuthenticationSchemeOptions.DefaultScheme)]
// public IActionResult ExternalApiEndpoint() { }

// // Accepts either JWT or API Key
// [Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme},{ApiKeyAuthenticationSchemeOptions.DefaultScheme}")]
// public IActionResult FlexibleEndpoint() { }


// Add Authorization
// builder.Services.AddAuthorization(options =>
// {
//     // Register all permission constants as policies
//     var allPermissions = Permissions.GetAllPermissions();

//     foreach (var permission in allPermissions)
//     {
//         options.AddPolicy(permission, policy =>
//         {
//             policy.RequireAuthenticatedUser();
//             policy.AddRequirements(new PermissionRequirement(permission));
//         });
//     }

//     // Set default policy
//     options.DefaultPolicy = new AuthorizationPolicyBuilder()
//         .RequireAuthenticatedUser()
//         .Build();
// });

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

// Add AutoMapper
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddMaps(Assembly.GetExecutingAssembly());

    var autoMapperLicenseKey = builder.Configuration["AutoMapper:LicenseKey"];
    if (!string.IsNullOrWhiteSpace(autoMapperLicenseKey))
    {
        cfg.LicenseKey = autoMapperLicenseKey;
    }
});
builder.Services.AddSingleton<PermissionHubFilter>();
// Add SignalR
builder.Services.AddSignalR(options =>
{
    // Configure SignalR options
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(30);
}).AddHubOptions<OperatorHub>(o =>
{
    o.AddFilter<PermissionHubFilter>();
});

// Add Health Checks
builder.Services.ConfigureHealthChecks(builder.Configuration);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        // Environment-based CORS configuration supporting LAN access
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000", "https://localhost:3000" };

        var allowedMethods = builder.Configuration.GetSection("Cors:AllowedMethods").Get<string[]>()
            ?? new[] { "GET", "POST", "PUT", "DELETE", "OPTIONS", "PATCH" };

        var allowedHeaders = builder.Configuration.GetSection("Cors:AllowedHeaders").Get<string[]>()
            ?? new[] { "Content-Type", "Authorization", "X-Correlation-ID", "X-Request-ID", "X-VMS-Client", "X-VMS-Version" };

        // For development, allow any origin from 192.168.0.* subnet
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrEmpty(origin))
                    return false;

                // Allow configured origins
                if (allowedOrigins.Contains(origin))
                    return true;

                // Allow any origin from local network (192.168.0.*)
                if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                {
                    var host = uri.Host;
                    // Allow localhost and 192.168.0.* subnet
                    return host == "localhost" ||
                           host == "127.0.0.1" ||
                           host.StartsWith("192.168.0.") ||
                           host.StartsWith("192.168.1.") ||  // Common subnet
                           host.StartsWith("10.0.0.");       // Another common subnet
                }

                return false;
            })
            .WithMethods(allowedMethods)
            .AllowAnyHeader()
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromSeconds(86400)); // 24 hours cache for preflight
        }
        else
        {
            // Production: Strict origin checking
            policy.WithOrigins(allowedOrigins)
                  .WithMethods(allowedMethods)
                  .AllowAnyHeader()
                  .AllowCredentials()
                  .SetPreflightMaxAge(TimeSpan.FromSeconds(86400));
        }
    });
});

// Configure Swagger/OpenAPI
builder.Services.ConfigureSwagger();

// Register application services
// Services are already registered through the main registration method
builder.Services.ConfigureApplicationServices(builder.Configuration);

// Configure forwarded headers — must be registered before builder.Build().
// Allows UrlResolverService and scheme detection to work correctly when IIS
// or any reverse proxy terminates TLS and forwards X-Forwarded-Proto/Host.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    // Accept forwarded headers from any source (IIS loopback is not in KnownNetworks by default)
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();
var enforceCanonicalHost = builder.Configuration.GetValue<bool>("Security:EnforceCanonicalHost");

// Configure the HTTP request pipeline.

// Must be FIRST: reads X-Forwarded-Proto/Host/For from IIS/proxy so that
// request.Scheme and request.Host reflect the real public values.
app.UseForwardedHeaders();

// Establishes one request correlation ID for logs, response headers, response bodies, and audit metadata.
app.UseMiddleware<RequestLoggingMiddleware>();

// Redirect HTTP to HTTPS in production. Canonical-host enforcement is optional
// because some deployments intentionally support localhost, machine-name, and IP access.
if (!app.Environment.IsDevelopment())
{
    if (enforceCanonicalHost)
    {
        app.UseMiddleware<CanonicalUrlRedirectMiddleware>();
    }

    app.UseHttpsRedirection();
}

// Application:EnableSwagger in appsettings.Production.json controls production availability.
// Changing it via the admin UI requires an app restart to take effect.
var enableSwagger = builder.Configuration.GetValue<bool>("Application:EnableSwagger", true);
if (enableSwagger || app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "VMS API V1");
        c.RoutePrefix = "swagger";
    });
}

// Managed SPA assets remain under the application directory. Customer-generated
// uploads and face snapshots are served from the external runtime data root.
app.UseDefaultFiles();
app.UseStaticFiles();
var runtimeDataRoot = VmsRuntimePaths.GetDataRoot(app.Environment);
var uploadsRoot = Path.Combine(runtimeDataRoot, "uploads");
var faceSnapshotsRoot = Path.Combine(runtimeDataRoot, "face-snapshots");
Directory.CreateDirectory(uploadsRoot);
Directory.CreateDirectory(faceSnapshotsRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRoot),
    RequestPath = "/uploads"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(faceSnapshotsRoot),
    RequestPath = "/face-snapshots"
});

// CORS middleware
app.UseCors("AllowFrontend");

app.UseSession();

// Global exception handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Security headers
app.UseMiddleware<SecurityHeadersMiddleware>();

// Rate limiting
app.UseMiddleware<RateLimitingMiddleware>();

// Authentication & Authorization (removed duplicate custom middleware)
app.UseAuthentication();
app.UseMiddleware<PermissionClaimsMiddleware>();
app.UseLicenseEnforcement();

app.UseAuthorization();

// Maintenance mode — after auth so Administrator role check works; before audit/endpoints
app.UseMiddleware<MaintenanceModeMiddleware>();

// Audit logging (after authentication so we have user context)
app.UseMiddleware<AuditLoggingMiddleware>();

app.MapControllers()
    .RequireRateLimiting("login") // Apply login rate limiting to auth endpoints
    .WithOpenApi();

// Map SignalR Hubs
app.MapHub<OperatorHub>("/hubs/operator");
app.MapHub<HostHub>("/hubs/host");
app.MapHub<SecurityHub>("/hubs/security");
app.MapHub<AdminHub>("/hubs/admin");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                data = e.Value.Data
            }),
            duration = report.TotalDuration
        });
        await context.Response.WriteAsync(result);
    }
});

// SPA fallback — must be AFTER all API/hub/health routes
app.MapFallbackToFile("index.html");

// ── License startup validation ────────────────────────────────────────────
// In Production: validate license before accepting any traffic.
// NotActivated is allowed (system runs but all API calls return 402).
// Any other invalid state (Tampered, Expired, HardwareMismatch, etc.) = refuse to start.
if (!app.Environment.IsDevelopment())
{
    var licenseValidator = app.Services.GetRequiredService<ILicenseValidatorService>();
    var licenseResult = await licenseValidator.ValidateCurrentLicenseAsync();
    if (!licenseResult.IsValid && licenseResult.Status != VisitorManagementSystem.Api.Domain.Enums.LicenseStatus.NotActivated)
    {
        Log.Fatal("License validation failed on startup (Status: {Status}): {Reason}",
            licenseResult.Status, licenseResult.FailureReason);
        Log.CloseAndFlush();
        return;
    }
    if (licenseResult.Status == VisitorManagementSystem.Api.Domain.Enums.LicenseStatus.NotActivated)
        Log.Warning("VMS is running without a license. All API endpoints are blocked until activation.");
}

// Cache mode startup diagnostic
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
var resilientCache = app.Services.GetService<VisitorManagementSystem.Api.Infrastructure.Caching.ResilientDistributedCache>();
if (resilientCache == null)
{
    startupLogger.LogWarning(
        "Redis is not configured (ConnectionStrings:Redis is empty). " +
        "Running with in-memory cache only. " +
        "Multi-server / load-balanced deployments will have inconsistent cache state.");
}
else if (!resilientCache.IsUsingRedis)
{
    startupLogger.LogWarning(
        "Redis is configured but unreachable at startup. " +
        "Fallen back to in-memory cache. " +
        "Multi-server / load-balanced deployments will have inconsistent cache state until Redis recovers.");
}
else
{
    startupLogger.LogInformation("Cache mode: Redis (distributed)");
}

app.Run();
