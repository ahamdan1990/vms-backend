using System.Threading;
using System.Threading.Tasks;

namespace VisitorManagementSystem.Api.Application.Services.Auth;

/// <summary>
/// Provides runtime access to LDAP configuration settings stored in dynamic configuration.
/// </summary>
public interface ILdapSettingsProvider
{
    /// <summary>
    /// Gets the current LDAP settings, optionally forcing a refresh from the configuration store.
    /// </summary>
    /// <param name="forceRefresh">Force a refresh and bypass the cache.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>LDAP configuration snapshot.</returns>
    Task<LdapConfiguration> GetSettingsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to get the cached LDAP settings without hitting the configuration store.
    /// </summary>
    /// <returns>Cached LDAP settings or null if cache is empty.</returns>
    LdapConfiguration? TryGetCachedSettings();

    /// <summary>
    /// Invalidates the cached LDAP settings to force a reload on next access.
    /// </summary>
    void InvalidateCache();
}
