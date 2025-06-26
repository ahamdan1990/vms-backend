using VisitorManagementSystem.Api.Application.DTOs.Auth;

namespace VisitorManagementSystem.Api.Application.DTOs.Users
{
    public class UserDetailDto : UserListDto
    {
        public string? PhoneNumber { get; set; }
        public string? ProfilePhotoPath { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public bool MustChangePassword { get; set; }
        public DateTime? PasswordChangedDate { get; set; }
        public string TimeZone { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Theme { get; set; } = string.Empty;
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public UserActivityDto? ActivitySummary { get; set; }
        public List<UserSessionDto> ActiveSessions { get; set; } = new();
    }
}
