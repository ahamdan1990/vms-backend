using VisitorManagementSystem.Api.Application.DTOs.Users;

namespace VisitorManagementSystem.Api.Application.Services.Users;

/// <summary>
/// Interface for user management operations
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets user by ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User details</returns>
    Task<UserDetailDto?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user
    /// </summary>
    /// <param name="createUserDto">User creation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user ID</returns>
    Task<int> CreateUserAsync(CreateUserDto createUserDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates user information
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="updateUserDto">Update data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task UpdateUserAsync(int userId, UpdateUserDto updateUserDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task DeleteUserAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users with pagination
    /// </summary>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="searchTerm">Search term</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated users</returns>
    Task<List<UserDto>> GetUsersAsync(int pageNumber = 1, int pageSize = 20, string? searchTerm = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a user account
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task ActivateUserAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a user account
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task DeactivateUserAsync(int userId, CancellationToken cancellationToken = default);
}
