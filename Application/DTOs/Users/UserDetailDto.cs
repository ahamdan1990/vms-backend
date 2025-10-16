using VisitorManagementSystem.Api.Application.DTOs.Auth;

namespace VisitorManagementSystem.Api.Application.DTOs.Users
{
    public class UserDetailDto : UserListDto
    {
        // Enhanced phone fields
        public new string? PhoneNumber { get; set; }
        public new string? PhoneCountryCode { get; set; }
        public new string? PhoneType { get; set; }
        
        public string? ProfilePhotoPath { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public bool MustChangePassword { get; set; }
        public DateTime? PasswordChangedDate { get; set; }
        
        // User preferences
        public string TimeZone { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Theme { get; set; } = string.Empty;
        
        // Enhanced address fields
        public string? AddressType { get; set; }
        public string? Street1 { get; set; }
        public string? Street2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public UserActivityDto? ActivitySummary { get; set; }
        public List<UserSessionDto> ActiveSessions { get; set; } = new();
    }
}
