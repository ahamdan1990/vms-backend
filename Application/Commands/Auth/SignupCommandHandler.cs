using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Domain.ValueObjects;
using VisitorManagementSystem.Api.Infrastructure.Utilities;

namespace VisitorManagementSystem.Api.Application.Commands.Auth
{
    /// <summary>
    /// Handler for user signup command
    /// Validates input, creates user account, and sends email verification
    /// </summary>
    public class SignupCommandHandler : IRequestHandler<SignupCommand, SignupResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailVerificationService _emailVerificationService;
        private readonly ILogger<SignupCommandHandler> _logger;
        private readonly string _baseUrl;

        public SignupCommandHandler(
            IUnitOfWork unitOfWork,
            IEmailVerificationService emailVerificationService,
            ILogger<SignupCommandHandler> logger,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _emailVerificationService = emailVerificationService ?? throw new ArgumentNullException(nameof(emailVerificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _baseUrl = configuration?["BaseUrl"] ?? "https://localhost:3000";
        }

        public async Task<SignupResult> Handle(SignupCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Validate input
                var validationErrors = ValidateSignupInput(request);
                if (validationErrors.Count > 0)
                {
                    _logger.LogWarning("Signup validation failed for email: {Email}", request.Email);
                    return SignupResult.Failure(validationErrors);
                }

                // Null safety checks (these should be caught by validation, but adding for compiler)
                if (string.IsNullOrWhiteSpace(request.Email))
                    return SignupResult.Failure("Email is required");

                if (string.IsNullOrWhiteSpace(request.Password))
                    return SignupResult.Failure("Password is required");

                if (string.IsNullOrWhiteSpace(request.FirstName))
                    return SignupResult.Failure("First name is required");

                if (string.IsNullOrWhiteSpace(request.LastName))
                    return SignupResult.Failure("Last name is required");

                // Check if email already exists
                var existingUser = await _unitOfWork.Users.GetByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("Signup attempt with existing email: {Email}", request.Email);
                    return SignupResult.Failure($"The email address {request.Email} is already registered");
                }

                // Create new user
                var salt = CryptoHelper.GenerateSalt();
                var passwordHash = CryptoHelper.HashPassword(request.Password, salt);

                var newUser = new User
                {
                    Email = new Email(request.Email),
                    FirstName = request.FirstName.Trim(),
                    LastName = request.LastName.Trim(),
                    PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
                        ? null
                        : new PhoneNumber(request.PhoneNumber),
                    JobTitle = request.JobTitle?.Trim(),
                    DepartmentId = request.DepartmentId,
                    Status = UserStatus.Pending, // Pending email verification
                    Role = UserRole.Staff, // Default role for new signups
                    IsActive = true,
                    IsEmailVerified = false,
                    IsLdapUser = false,
                    PasswordHash = passwordHash,
                    PasswordSalt = salt,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    CreatedOn = DateTime.UtcNow,
                    IsDeleted = false,
                    NormalizedEmail = request.Email.ToUpperInvariant(),
                    TimeZone = "UTC",
                    Language = "en-US",
                    Theme = "light"
                };

                await _unitOfWork.Users.AddAsync(newUser, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("New user created: {Email}, UserId: {UserId}", newUser.Email.Value, newUser.Id);

                // Generate email verification token
                var verificationToken = await _emailVerificationService.GenerateVerificationTokenAsync(newUser.Id);

                if (string.IsNullOrWhiteSpace(verificationToken))
                {
                    _logger.LogError("Failed to generate verification token for user: {UserId}", newUser.Id);
                    return SignupResult.Failure("System error: could not generate verification token");
                }

                // Send verification email
                var verificationLink = $"{_baseUrl}/verify-email?userId={newUser.Id}&token={verificationToken}";

                try
                {
                    await _emailVerificationService.SendVerificationEmailAsync(
                        newUser.Email.Value,
                        newUser.FirstName,
                        verificationLink);

                    _logger.LogInformation("Verification email sent to: {Email}", newUser.Email.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending verification email to: {Email}", newUser.Email.Value);
                    // Don't fail the signup if email sending fails - user can request resend
                }

                return SignupResult.Success(newUser.Id, "Signup successful. Please check your email to verify your account.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing signup for email: {Email}", request.Email);
                return SignupResult.Failure("An error occurred during signup");
            }
        }

        /// <summary>
        /// Validates signup input against business rules
        /// </summary>
        private List<string> ValidateSignupInput(SignupCommand request)
        {
            var errors = new List<string>();

            // Validate first name
            if (string.IsNullOrWhiteSpace(request.FirstName))
                errors.Add("First name is required");
            else if (request.FirstName.Trim().Length < 2)
                errors.Add("First name must be at least 2 characters");
            else if (request.FirstName.Length > 50)
                errors.Add("First name cannot exceed 50 characters");

            // Validate last name
            if (string.IsNullOrWhiteSpace(request.LastName))
                errors.Add("Last name is required");
            else if (request.LastName.Trim().Length < 2)
                errors.Add("Last name must be at least 2 characters");
            else if (request.LastName.Length > 50)
                errors.Add("Last name cannot exceed 50 characters");

            // Validate email
            if (string.IsNullOrWhiteSpace(request.Email))
                errors.Add("Email is required");
            else if (!IsValidEmail(request.Email))
                errors.Add("Email format is invalid");

            // Validate password
            if (string.IsNullOrWhiteSpace(request.Password))
                errors.Add("Password is required");
            else
            {
                var passwordErrors = ValidatePasswordStrength(request.Password);
                errors.AddRange(passwordErrors);
            }

            // Validate password confirmation
            if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
                errors.Add("Password confirmation is required");
            else if (request.Password != request.ConfirmPassword)
                errors.Add("Passwords do not match");

            // Validate phone number (optional but if provided, must be valid)
            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                if (request.PhoneNumber.Length < 7)
                    errors.Add("Phone number must be at least 7 digits");
                if (request.PhoneNumber.Length > 20)
                    errors.Add("Phone number cannot exceed 20 characters");
            }

            // Validate job title (optional)
            if (!string.IsNullOrWhiteSpace(request.JobTitle) && request.JobTitle.Length > 100)
                errors.Add("Job title cannot exceed 100 characters");

            return errors;
        }

        /// <summary>
        /// Validates password strength
        /// </summary>
        private List<string> ValidatePasswordStrength(string password)
        {
            var errors = new List<string>();

            if (password.Length < 8)
                errors.Add("Password must be at least 8 characters");
            else if (password.Length > 128)
                errors.Add("Password cannot exceed 128 characters");

            if (!Regex.IsMatch(password, @"[A-Z]"))
                errors.Add("Password must contain at least one uppercase letter");

            if (!Regex.IsMatch(password, @"[a-z]"))
                errors.Add("Password must contain at least one lowercase letter");

            if (!Regex.IsMatch(password, @"[0-9]"))
                errors.Add("Password must contain at least one digit");

            if (!Regex.IsMatch(password, @"[!@#$%^&*()_\-+=\[\]{};':"",.<>/?\\|`~]"))
                errors.Add("Password must contain at least one special character");

            return errors;
        }

        /// <summary>
        /// Validates email format
        /// </summary>
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
