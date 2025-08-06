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

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/vms-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();

// Database Configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("VisitorManagementSystem.Api"));

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
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    
    // Configure token validation parameters
    // This will be dynamically loaded from database in production
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(
            builder.Configuration["JWT:SecretKey"] ?? "your-super-secret-jwt-key-that-must-be-at-least-32-characters-long-for-security")),
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
});

// Add Authorization
builder.Services.AddAuthorization(options =>
{
    // Register all permission constants as policies
    var allPermissions = Permissions.GetAllPermissions();

    foreach (var permission in allPermissions)
    {
        options.AddPolicy(permission, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.AddRequirements(new PermissionRequirement(permission));
        });
    }

    // Set default policy
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

// Add AutoMapper
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

// Add Health Checks
builder.Services.ConfigureHealthChecks(builder.Configuration);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        // ✅ FIXED: Environment-based CORS configuration
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000", "https://localhost:3000" }; // Fallback for development

        var allowedMethods = builder.Configuration.GetSection("Cors:AllowedMethods").Get<string[]>()
            ?? new[] { "GET", "POST", "PUT", "DELETE", "OPTIONS", "PATCH"};

        var allowedHeaders = builder.Configuration.GetSection("Cors:AllowedHeaders").Get<string[]>()
            ?? new[] { "Content-Type", "Authorization", "X-Request-ID", "X-VMS-Client", "X-VMS-Version"};

        policy.WithOrigins(allowedOrigins)
              .WithMethods(allowedMethods)
              .AllowAnyHeader()
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromSeconds(86400)); // 24 hours cache for preflight
    });
});

// Configure Swagger/OpenAPI
builder.Services.ConfigureSwagger();

// Register application services
builder.Services.RegisterApplicationServices(builder.Configuration);


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "VMS API V1");
        c.RoutePrefix = string.Empty;
    });
}

// CORS middleware
app.UseCors("AllowFrontend");

app.UseSession();

// Global exception handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Security headers
app.UseMiddleware<SecurityHeadersMiddleware>();

// Request logging
app.UseMiddleware<RequestLoggingMiddleware>();

// Rate limiting
app.UseMiddleware<RateLimitingMiddleware>();

// Authentication & Authorization (removed duplicate custom middleware)
app.UseAuthentication();
app.UseAuthorization();

// Audit logging (after authentication so we have user context)
app.UseMiddleware<AuditLoggingMiddleware>();

app.MapControllers()
    .RequireRateLimiting("login") // Apply login rate limiting to auth endpoints
    .WithOpenApi();

app.MapHealthChecks("/health");

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        await context.Database.MigrateAsync();
        await DbInitializer.InitializeAsync(context, scope.ServiceProvider);
        logger.LogInformation("Database initialized successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database");
        throw;
    }
}

app.Run();