using System.ComponentModel.DataAnnotations;

namespace VisitorManagementSystem.Api.Application.DTOs.Users
{

    /// <summary>
    /// User deactivation request DTO
    /// </summary>
    public class UserDeactivationDto
    {
        [Required(ErrorMessage = "Reason is required for deactivation")]
        public string Reason { get; set; } = string.Empty;
        public bool RevokeAllSessions { get; set; } = true;
    }
}
