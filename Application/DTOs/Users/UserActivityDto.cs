namespace VisitorManagementSystem.Api.Application.DTOs.Users
{

    public class UserActivityDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public int LoginCount { get; set; }
        public DateTime? LastLogin { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LastFailedLogin { get; set; }
        public int InvitationsCreated { get; set; }
        public int PasswordChanges { get; set; }
        public Dictionary<string, int> ActivityByType { get; set; } = new();
        public List<string> RecentActions { get; set; } = new();
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public bool IsSuccess { get; set; }
    }
}
