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
using VisitorManagementSystem.Api.Configuration;
using System.Reflection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Infrastructure.Security.Authorization;

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
builder.Services.Configure<JwtConfiguration>(builder.Configuration.GetSection("JWT"));
builder.Services.Configure<SecurityConfiguration>(builder.Configuration.GetSection("Security"));

var jwtConfig = builder.Configuration.GetSection("JWT").Get<JwtConfiguration>();
var key = Encoding.ASCII.GetBytes(jwtConfig?.SecretKey ?? throw new InvalidOperationException("JWT SecretKey not configured"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtConfig.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtConfig.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        NameClaimType = ClaimTypes.NameIdentifier // IMPORTANT: This fixes GetCurrentUserId()
    };

    // FIXED: Enhanced cookie token extraction
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

            // Log all cookies for debugging
            var allCookies = string.Join(", ", context.Request.Cookies.Select(c => $"{c.Key}={c.Value?.Substring(0, Math.Min(10, c.Value.Length))}..."));
            logger.LogInformation("🍪 All cookies: {Cookies}", allCookies);

            // Try Authorization header first
            var authorization = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
            {
                context.Token = authorization.Substring("Bearer ".Length).Trim();
                logger.LogInformation("🔑 Token from Authorization header: {TokenStart}...", context.Token?.Substring(0, Math.Min(20, context.Token.Length)));
                return Task.CompletedTask;
            }

            // Try access_token cookie
            var token = context.Request.Cookies["access_token"];
            if (!string.IsNullOrEmpty(token))
            {
                context.Token = token;
                logger.LogInformation("🔑 Token from cookie: {TokenStart}...", token?.Substring(0, Math.Min(20, token.Length)));
            }
            else
            {
                logger.LogWarning("❌ No access_token cookie found!");
            }

            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            logger.LogInformation("✅ Token validated successfully for user: {UserId}", userId);
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError("❌ JWT Authentication failed: {Error}", context.Exception.Message);
            return Task.CompletedTask;
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
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
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

app.UseSession();

// Global exception handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Security headers
app.UseMiddleware<SecurityHeadersMiddleware>();

// Request logging
app.UseMiddleware<RequestLoggingMiddleware>();

// Rate limiting
app.UseRateLimiter();

// Authentication middleware
app.UseMiddleware<AuthenticationMiddleware>();

// Audit logging
app.UseMiddleware<AuditLoggingMiddleware>();

//app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

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