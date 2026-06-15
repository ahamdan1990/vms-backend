using System.Security.Cryptography;

namespace VisitorManagementSystem.Api.Infrastructure.Configuration;

public static class VmsProductionConfigurationExtensions
{
    public static ConfigurationManager AddVmsProductionConfiguration(
        this ConfigurationManager configuration,
        IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
            return configuration;

        var configPath = VmsRuntimePaths.GetConfigPath();
        configuration.AddJsonFile(configPath, optional: false, reloadOnChange: true);

        var secretsPath = VmsRuntimePaths.GetSecretsPath();
        if (!File.Exists(secretsPath))
            throw new InvalidOperationException($"Protected production secrets file was not found: {secretsPath}");

        var protectedBytes = File.ReadAllBytes(secretsPath);
        var jsonBytes = ProtectedData.Unprotect(protectedBytes, optionalEntropy: null, DataProtectionScope.LocalMachine);
        configuration.AddJsonStream(new MemoryStream(jsonBytes, writable: false));

        return configuration;
    }
}
