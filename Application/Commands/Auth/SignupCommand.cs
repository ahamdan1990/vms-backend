using MediatR;

namespace VisitorManagementSystem.Api.Application.Commands.Auth
{
    /// <summary>
    /// Command for user registration/signup
    /// </summary>
    public class SignupCommand : IRequest<SignupResult>
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
        public string? PhoneNumber { get; set; }
        public int? DepartmentId { get; set; }
        public string? JobTitle { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }

    public class SignupResult
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        public int? UserId { get; set; }
        public List<string> Errors { get; set; } = [];

        public static SignupResult Success(int userId, string message = "Account created successfully. Please verify your email.")
        {
            return new SignupResult
            {
                IsSuccess = true,
                UserId = userId,
                Message = message
            };
        }

        public static SignupResult Failure(string error)
        {
            return new SignupResult
            {
                IsSuccess = false,
                Errors = [error]
            };
        }

        public static SignupResult Failure(List<string> errors)
        {
            return new SignupResult
            {
                IsSuccess = false,
                Errors = errors
            };
        }
    }
}
