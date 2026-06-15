using Microsoft.AspNetCore.DataProtection;

namespace VisitorManagementSystem.Api.Infrastructure.Configuration;

public sealed class SensitiveConfigurationProtection
{
    private const string ProtectedPrefix = "vms-protected:";
    private readonly IDataProtector _protector;

    public SensitiveConfigurationProtection(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("VMS.SystemConfiguration.v1");
    }

    public string Protect(string value) =>
        IsProtected(value) ? value : ProtectedPrefix + _protector.Protect(value);

    public string Unprotect(string value) =>
        IsProtected(value) ? _protector.Unprotect(value[ProtectedPrefix.Length..]) : value;

    public static bool IsProtected(string value) =>
        value.StartsWith(ProtectedPrefix, StringComparison.Ordinal);
}
