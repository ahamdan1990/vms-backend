namespace VisitorManagementSystem.Api.Application.DTOs.Users
{
    /// <summary>
    /// User activation request DTO
    /// </summary>
    public class UserActivationDto
    {
        public string? Reason { get; set; }
        public bool ResetFailedAttempts { get; set; } = true;
    }

}
