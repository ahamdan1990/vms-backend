using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Auth;

namespace VisitorManagementSystem.Api.Application.Commands.Auth
{
    /// <summary>
    /// Command for LDAP/Active Directory user authentication
    /// Authenticates user against company domain instead of local password
    /// </summary>
    public class LdapLoginCommand : IRequest<AuthenticationResult>
    {
        /// <summary>
        /// Username (can be with or without domain, e.g., "john.doe" or "john.doe@company.com")
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Password
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// User's IP address (for audit and security tracking)
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// User's browser/client information
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// Device fingerprint for device tracking
        /// </summary>
        public string? DeviceFingerprint { get; set; }

        /// <summary>
        /// Remember this device
        /// </summary>
        public bool RememberMe { get; set; }
    }
}
