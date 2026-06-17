using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.Versioning;
using Microsoft.Win32;
using VisitorManagementSystem.Api.Application.Models.Licensing;
using VisitorManagementSystem.Api.Application.Services.Licensing;

namespace VisitorManagementSystem.Api.Infrastructure.Services.Licensing;

[SupportedOSPlatform("windows")]
public class MachineFingerprintService : IMachineFingerprintService
{
    private static readonly object _lock = new();
    private MachineComponentInfo? _cached;

    public MachineComponentInfo CollectComponents()
    {
        lock (_lock)
        {
            if (_cached != null)
                return _cached;

            _cached = new MachineComponentInfo
            {
                BiosUuid = GetWmiValue("Win32_ComputerSystemProduct", "UUID"),
                MotherboardSerial = GetWmiValue("Win32_BaseBoard", "SerialNumber"),
                CpuProcessorId = GetWmiValue("Win32_Processor", "ProcessorId"),
                WindowsProductId = GetRegistryValue(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductId"),
                PrimaryMacAddress = GetPrimaryMacAddress()
            };

            return _cached;
        }
    }

    public MachineFingerprintData ComputeFingerprint()
        => CollectComponents().ToFingerprintData(requiredMatches: 3);

    public string GetDisplayFingerprint()
        => ComputeFingerprint().FullHash;

    public bool[] ScoreAgainstLicense(MachineFingerprintData licenseFingerprint)
    {
        var current = ComputeFingerprint();

        if (current.ComponentHashes.Length != licenseFingerprint.ComponentHashes.Length)
            return new bool[licenseFingerprint.ComponentHashes.Length];

        return current.ComponentHashes
            .Zip(licenseFingerprint.ComponentHashes, (a, b) =>
                string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private static string GetWmiValue(string wmiClass, string propertyName)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT {propertyName} FROM {wmiClass}");
            foreach (ManagementObject obj in searcher.Get())
            {
                var value = obj[propertyName]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(value) &&
                    !string.Equals(value, "None", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(value, "Default string", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(value, "To Be Filled By O.E.M.", StringComparison.OrdinalIgnoreCase))
                {
                    return value;
                }
            }
        }
        catch
        {
            // WMI may be restricted by policy or unavailable; return empty for tolerance
        }
        return string.Empty;
    }

    private static string GetRegistryValue(string subKey, string valueName)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(subKey);
            return key?.GetValue(valueName)?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string GetPrimaryMacAddress()
    {
        try
        {
            var mac = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n =>
                    n.OperationalStatus == OperationalStatus.Up &&
                    n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    n.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
                    !n.Description.Contains("Virtual", StringComparison.OrdinalIgnoreCase) &&
                    !n.Description.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase) &&
                    !n.Description.Contains("VMware", StringComparison.OrdinalIgnoreCase) &&
                    !n.Description.Contains("VirtualBox", StringComparison.OrdinalIgnoreCase))
                .OrderBy(n => n.Name)
                .Select(n => n.GetPhysicalAddress().ToString())
                .FirstOrDefault(m => !string.IsNullOrEmpty(m) && m != "000000000000");

            return mac ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
