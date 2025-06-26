using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Domain.ValueObjects;

namespace VisitorManagementSystem.Api.Application.Commands.Users
{
    /// <summary>
    /// Handler for create user command
    /// </summary>
    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordService _passwordService;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateUserCommandHandler> _logger;

        public CreateUserCommandHandler(
            IUnitOfWork unitOfWork,
            IPasswordService passwordService,
            IMapper mapper,
            ILogger<CreateUserCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _passwordService = passwordService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Processing create user command for email: {Email}", request.Email);

                // Validate email uniqueness
                if (await _unitOfWork.Users.EmailExistsAsync(request.Email, cancellationToken: cancellationToken))
                {
                    _logger.LogWarning("Attempt to create user with existing email: {Email}", request.Email);
                    throw new InvalidOperationException($"A user with email '{request.Email}' already exists.");
                }

                // Validate employee ID uniqueness if provided
                if (!string.IsNullOrEmpty(request.EmployeeId) &&
                    await _unitOfWork.Users.EmployeeIdExistsAsync(request.EmployeeId, cancellationToken: cancellationToken))
                {
                    _logger.LogWarning("Attempt to create user with existing employee ID: {EmployeeId}", request.EmployeeId);
                    throw new InvalidOperationException($"A user with employee ID '{request.EmployeeId}' already exists.");
                }

                // Generate password if not provided
                var password = !string.IsNullOrEmpty(request.TemporaryPassword)
                    ? request.TemporaryPassword
                    : _passwordService.GeneratePassword(12, true, true);

                // Validate password
                var passwordValidation = _passwordService.ValidatePassword(password);
                if (!passwordValidation.IsValid)
                {
                    _logger.LogWarning("Generated/provided password failed validation for user: {Email}", request.Email);
                    throw new InvalidOperationException($"Password validation failed: {string.Join(", ", passwordValidation.Errors)}");
                }

                // Hash password
                var hashedPassword = _passwordService.HashPassword(password);

                // Create user entity
                var user = new User
                {
                    FirstName = request.FirstName.Trim(),
                    LastName = request.LastName.Trim(),
                    Email = new Email(request.Email.Trim().ToLowerInvariant()),
                    NormalizedEmail = request.Email.Trim().ToUpperInvariant(),
                    PasswordHash = hashedPassword.Hash,
                    PasswordSalt = hashedPassword.Salt,
                    Role = request.Role,
                    Status = UserStatus.Active,
                    Department = request.Department?.Trim(),
                    JobTitle = request.JobTitle?.Trim(),
                    EmployeeId = request.EmployeeId?.Trim(),
                    TimeZone = request.TimeZone ?? "UTC",
                    Language = request.Language ?? "en-US",
                    MustChangePassword = request.MustChangePassword,
                    PasswordChangedDate = DateTime.UtcNow,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    IsActive = true
                };

                // Set phone number if provided
                if (!string.IsNullOrEmpty(request.PhoneNumber))
                {
                    user.PhoneNumber = new PhoneNumber(request.PhoneNumber);
                }

                // Set audit information
                user.SetCreatedBy(request.CreatedBy);

                // Add user to repository
                await _unitOfWork.Users.AddAsync(user, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Save password to history
                await _passwordService.SavePasswordHistoryAsync(
                    user.Id,
                    hashedPassword.Hash,
                    hashedPassword.Salt,
                    cancellationToken);

                _logger.LogInformation("User created successfully: {UserId} ({Email}) by {CreatedBy}",
                    user.Id, user.Email.Value, request.CreatedBy);

                // Map to DTO
                var userDto = _mapper.Map<UserDto>(user);

                // Add temporary password to response for admin notification
                // Note: In production, this should be sent via secure channel
                if (!string.IsNullOrEmpty(request.TemporaryPassword))
                {
                    userDto.TemporaryPassword = password; // This should be handled securely
                }

                return userDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user with email: {Email}", request.Email);
                throw;
            }
        }
    }
}
