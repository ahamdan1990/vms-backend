using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Services.Users;

/// <summary>
/// User service implementation
/// </summary>
public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;

    public UserService(IUnitOfWork unitOfWork, ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UserDetailDto?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return null;
        }

        // TODO: Map to UserDetailDto
        return new UserDetailDto
        {
            Id = user.Id,
            Email = user.Email.Value,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            CreatedOn = user.CreatedOn,
            ModifiedOn = user.ModifiedOn ?? DateTime.UtcNow
        };
    }
    public async Task<int> CreateUserAsync(CreateUserDto createUserDto, CancellationToken cancellationToken = default)
    {
        // TODO: Implement user creation
        _logger.LogInformation("Creating user with email {Email}", createUserDto.Email);
        
        // Placeholder implementation
        throw new NotImplementedException("User creation not implemented");
    }

    public async Task UpdateUserAsync(int userId, UpdateUserDto updateUserDto, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        // TODO: Update user properties
        _logger.LogInformation("Updating user {UserId}", userId);
        
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        _unitOfWork.Users.Delete(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Deleted user {UserId}", userId);
    }

    public async Task<List<UserDto>> GetUsersAsync(int pageNumber = 1, int pageSize = 20, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        // TODO: Implement pagination and search
        var users = await _unitOfWork.Users.GetAllAsync(cancellationToken);
        
        return users.Select(u => new UserDto
        {
            Id = u.Id,
            Email = u.Email.Value,
            FirstName = u.FirstName,
            LastName = u.LastName,
            IsActive = u.IsActive,
            CreatedOn = u.CreatedOn
        }).ToList();
    }

    public async Task ActivateUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        user.Activate();
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Activated user {UserId}", userId);
    }

    public async Task DeactivateUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found");
        }

        user.Deactivate();
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Deactivated user {UserId}", userId);
    }
}
