using Microsoft.Extensions.Configuration;

namespace VisitorManagementSystem.Api.Infrastructure.Configuration;

public static class VmsRuntimePaths
{
    public const string ConfigPathEnvironmentVariable = "VMS_CONFIG_PATH";
    public const string SecretsPathEnvironmentVariable = "VMS_SECRETS_PATH";
    public const string DataRootEnvironmentVariable = "VMS_DATA_ROOT";
    public const string LogsRootEnvironmentVariable = "VMS_LOGS_ROOT";

    public static string ProgramDataRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "VMS");

    public static string GetDataRoot(IHostEnvironment environment)
    {
        var configured = Environment.GetEnvironmentVariable(DataRootEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configured))
            return Path.GetFullPath(configured);

        return environment.IsDevelopment()
            ? Path.Combine(environment.ContentRootPath, ".runtime", "data")
            : Path.Combine(ProgramDataRoot, "data");
    }

    public static string GetLogsRoot(IHostEnvironment environment)
    {
        var configured = Environment.GetEnvironmentVariable(LogsRootEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configured))
            return Path.GetFullPath(configured);

        return environment.IsDevelopment()
            ? Path.Combine(environment.ContentRootPath, ".runtime", "logs")
            : Path.Combine(ProgramDataRoot, "logs");
    }

    public static string GetConfigPath() =>
        Environment.GetEnvironmentVariable(ConfigPathEnvironmentVariable)
        ?? Path.Combine(ProgramDataRoot, "config", "appsettings.Production.json");

    public static string GetSecretsPath() =>
        Environment.GetEnvironmentVariable(SecretsPathEnvironmentVariable)
        ?? Path.Combine(ProgramDataRoot, "secrets", "appsettings.secrets.dpapi");

    public static string ResolveDataPath(IHostEnvironment environment, string relativePath)
        => ResolveDataPath(GetDataRoot(environment), relativePath);

    public static string ResolveDataPath(string dataRoot, string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        var root = Path.GetFullPath(dataRoot);
        var candidate = Path.GetFullPath(Path.Combine(root, normalized));
        var rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Runtime data path escapes the configured VMS data root.");

        return candidate;
    }
}
