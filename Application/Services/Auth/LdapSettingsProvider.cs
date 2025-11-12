using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using VisitorManagementSystem.Api.Application.Services.Configuration;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.Services.Auth;

/// <summary>
/// Resolves LDAP settings from the dynamic configuration store with lightweight caching.
/// </summary>
public class LdapSettingsProvider : ILdapSettingsProvider
{
    private readonly IDynamicConfigurationService _configService;
    private readonly IConfiguration _applicationConfiguration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LdapSettingsProvider> _logger;

    private static readonly string CacheKey = "ldap_settings_provider_cache";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public LdapSettingsProvider(
        IDynamicConfigurationService configService,
        IConfiguration applicationConfiguration,
        IMemoryCache cache,
        ILogger<LdapSettingsProvider> logger)
    {
        _configService = configService;
        _applicationConfiguration = applicationConfiguration;
        _cache = cache;
        _logger = logger;
    }

    public async Task<LdapConfiguration> GetSettingsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        if (!forceRefresh &&
            _cache.TryGetValue(CacheKey, out var cachedObj) &&
            cachedObj is LdapConfiguration cachedSettings)
        {
            return cachedSettings;
        }

        var settings = await BuildSettingsAsync(cancellationToken);
        _cache.Set(CacheKey, settings, CacheDuration);
        return settings;
    }

    public LdapConfiguration? TryGetCachedSettings()
    {
        if (_cache.TryGetValue(CacheKey, out var cachedObj) &&
            cachedObj is LdapConfiguration cachedSettings)
        {
            return cachedSettings;
        }

        return null;
    }

    public void InvalidateCache()
    {
        _cache.Remove(CacheKey);
    }

    private async Task<LdapConfiguration> BuildSettingsAsync(CancellationToken cancellationToken)
    {
        var defaults = _applicationConfiguration.GetSection("LdapConfiguration").Get<LdapConfiguration>() ?? new LdapConfiguration();
        var settings = new LdapConfiguration();

        settings.Enabled = await GetBoolAsync("Enabled", defaults.Enabled, cancellationToken);
        settings.Server = await GetStringAsync("Server", defaults.Server, cancellationToken);
        settings.Port = await GetIntAsync("Port", defaults.Port, cancellationToken);
        settings.Domain = await GetStringAsync("Domain", defaults.Domain, cancellationToken);
        settings.UserName = await GetStringAsync("UserName", defaults.UserName, cancellationToken);
        settings.Password = await GetStringAsync("Password", defaults.Password, cancellationToken);
        settings.BaseDn = await GetStringAsync("BaseDn", defaults.BaseDn, cancellationToken);
        settings.DefaultDepartmentId = await GetNullableIntAsync("DefaultDepartmentId", defaults.DefaultDepartmentId, cancellationToken);
        settings.AutoCreateUsers = await GetBoolAsync("AutoCreateUsers", defaults.AutoCreateUsers, cancellationToken);
        settings.SyncProfileOnLogin = await GetBoolAsync("SyncProfileOnLogin", defaults.SyncProfileOnLogin, cancellationToken);
        settings.IncludeDirectoryUsersInHostSearch = await GetBoolAsync("IncludeDirectoryUsersInHostSearch", defaults.IncludeDirectoryUsersInHostSearch, cancellationToken);
        settings.AllowRoleSelectionOnImport = await GetBoolAsync("AllowRoleSelectionOnImport", defaults.AllowRoleSelectionOnImport, cancellationToken);

        var defaultRoleName = await GetStringAsync("DefaultImportRole", defaults.DefaultImportRole, cancellationToken);
        if (!Enum.TryParse<UserRole>(defaultRoleName, true, out var defaultRole))
        {
            defaultRole = UserRole.Staff;
        }
        settings.DefaultImportRole = defaultRole.ToString();

        return settings;
    }

    private async Task<bool> GetBoolAsync(string key, bool fallback, CancellationToken cancellationToken)
    {
        var value = await _configService.GetConfigurationAsync<bool?>("LDAP", key, fallback, cancellationToken);
        return value.HasValue ? value.Value : fallback;
    }

    private async Task<int> GetIntAsync(string key, int fallback, CancellationToken cancellationToken)
    {
        var value = await _configService.GetConfigurationAsync<int?>("LDAP", key, fallback, cancellationToken);
        return value.HasValue ? value.Value : fallback;
    }

    private async Task<int?> GetNullableIntAsync(string key, int? fallback, CancellationToken cancellationToken)
    {
        return await _configService.GetConfigurationAsync("LDAP", key, fallback, cancellationToken) ?? fallback;
    }

    private async Task<string> GetStringAsync(string key, string? fallback, CancellationToken cancellationToken)
    {
        var safeFallback = fallback ?? string.Empty;
        return await _configService.GetConfigurationValueAsync("LDAP", key, safeFallback, cancellationToken) ?? safeFallback;
    }
}
