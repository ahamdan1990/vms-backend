namespace VisitorManagementSystem.Api.Application.DTOs.Users
{
    public class AdminPasswordResetResponseDto
    {
        public string? NewPassword { get; set; } // If null, system generates one
        public bool MustChangePassword { get; set; } = true;
        public bool NotifyUser { get; set; } = true;
        public string? Reason { get; set; }
    }
}
