using VisitorManagementSystem.Api.Application.Models.Licensing;

namespace VisitorManagementSystem.Api.Application.Services.Licensing;

public interface IMachineFingerprintService
{
    MachineComponentInfo CollectComponents();
    MachineFingerprintData ComputeFingerprint();
    string GetDisplayFingerprint();
    bool[] ScoreAgainstLicense(MachineFingerprintData licenseFingerprint);
}
