namespace VisitorManagementSystem.Api.Application.DTOs.Auth
{
    /// <summary>
    /// Token validation response DTO
    /// </summary>
    public class TokenValidationDto
    {
        public bool IsValid { get; set; }
        public int? UserId { get; set; }
        public string? UserEmail { get; set; }
        public DateTime? Expiry { get; set; }
        public string? Reason { get; set; }
        public bool IsExpired { get; set; }
        public bool IsNearExpiry { get; set; }
    }
}
