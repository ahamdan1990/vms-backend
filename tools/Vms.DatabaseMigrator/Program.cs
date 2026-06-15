using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using VisitorManagementSystem.Api.Infrastructure.Configuration;
using VisitorManagementSystem.Api.Infrastructure.Data;

static string? GetArgument(string[] values, string name)
{
    var index = Array.FindIndex(values, value => value.Equals(name, StringComparison.OrdinalIgnoreCase));
    return index >= 0 && index + 1 < values.Length ? values[index + 1] : null;
}

Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", Environments.Production);

var configPath = GetArgument(args, "--config");
var secretsPath = GetArgument(args, "--secrets");
if (!string.IsNullOrWhiteSpace(configPath))
    Environment.SetEnvironmentVariable(VmsRuntimePaths.ConfigPathEnvironmentVariable, configPath);
if (!string.IsNullOrWhiteSpace(secretsPath))
    Environment.SetEnvironmentVariable(VmsRuntimePaths.SecretsPathEnvironmentVariable, secretsPath);

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddVmsProductionConfiguration(builder.Environment);

// The migrator must run under a privileged (installer-time) connection. It must never
// fall back to the least-privilege vms_app runtime credential: that login is deliberately
// stripped of dbcreator and DDL rights by 03-setup-sql.ps1, so a silent fallback would
// surface as "CREATE DATABASE permission denied in database 'master'" on fresh targets.
var connectionString = Environment.GetEnvironmentVariable("VMS_MIGRATION_CONNECTION_STRING")
    ?? GetArgument(args, "--connection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "A privileged migration connection is required. Set the VMS_MIGRATION_CONNECTION_STRING " +
        "environment variable or pass --connection \"<connection string>\". The migrator refuses to " +
        "use ConnectionStrings:DefaultConnection from runtime configuration because the runtime SQL " +
        "login is intentionally not permitted to create databases or modify schema.");

var keyDirectory = Path.Combine(VmsRuntimePaths.ProgramDataRoot, "secrets", "DataProtectionKeys");
Directory.CreateDirectory(keyDirectory);

builder.Services.AddLogging();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDataProtection()
    .SetApplicationName("VisitorManagementSystem")
    .PersistKeysToFileSystem(new DirectoryInfo(keyDirectory))
    .ProtectKeysWithDpapi(protectToLocalMachine: true);
builder.Services.AddSingleton<SensitiveConfigurationProtection>();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly("VisitorManagementSystem.Api")));

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

Console.WriteLine("Applying VMS database migrations...");
await context.Database.MigrateAsync();
Console.WriteLine("Applying idempotent VMS seed data...");
await DbInitializer.InitializeAsync(context, scope.ServiceProvider);
Console.WriteLine("VMS database migration and initialization completed.");
