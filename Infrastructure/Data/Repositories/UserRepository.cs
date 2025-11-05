using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Domain.Specifications;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for User entity operations
/// </summary>
public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var specification = new UserByEmailSpecification(email);
        return await GetSingleAsync(specification, cancellationToken);
    }

    public async Task<User?> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        var specification = new UserByEmployeeIdSpecification(employeeId);
        return await GetSingleAsync(specification, cancellationToken);
    }

    public async Task<List<User>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        var specification = new UsersByRoleSpecification(role);
        return await GetAsync(specification, cancellationToken);
    }

    public async Task<List<User>> GetByRoleIdAsync(int roleId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => u.RoleId == roleId && u.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<User>> GetByStatusAsync(UserStatus status, CancellationToken cancellationToken = default)
    {
        var specification = new UsersByStatusSpecification(status);
        return await GetAsync(specification, cancellationToken);
    }

    public async Task<List<User>> GetByDepartmentAsync(string department, CancellationToken cancellationToken = default)
    {
        var specification = new UsersByDepartmentSpecification(department);
        return await GetAsync(specification, cancellationToken);
    }

    public async Task<List<User>> SearchAsync(string searchTerm, UserRole? role = null, UserStatus? status = null,
        string? department = null, CancellationToken cancellationToken = default)
    {
        var specification = new UserSearchSpecification(searchTerm, role, status, department);
        return await GetAsync(specification, cancellationToken);
    }

    public async Task<(List<User> Users, int TotalCount)> GetPaginatedAsync(int pageIndex, int pageSize,
        string? sortBy = null, bool sortDescending = false, CancellationToken cancellationToken = default)
    {
        var specification = new PaginatedUsersSpecification(pageIndex, pageSize, sortBy, sortDescending);
        return await GetPagedAsync(specification, cancellationToken);
    }

    public async Task<List<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        var specification = new ActiveUsersSpecification();
        return await GetAsync(specification, cancellationToken);
    }

    public async Task<List<User>> GetLockedOutUsersAsync(CancellationToken cancellationToken = default)
    {
        var specification = new LockedOutUsersSpecification();
        return await GetAsync(specification, cancellationToken);
    }

    public async Task<List<User>> GetUsersWithExpiredPasswordsAsync(int passwordExpiryDays, CancellationToken cancellationToken = default)
    {
        var specification = new UsersWithExpiredPasswordsSpecification(passwordExpiryDays);
        return await GetAsync(specification, cancellationToken);
    }

    public async Task<List<User>> GetUsersMustChangePasswordAsync(CancellationToken cancellationToken = default)
    {
        var specification = new UsersMustChangePasswordSpecification();
        return await GetAsync(specification, cancellationToken);
    }

    public async Task<List<User>> GetUsersCreatedInPeriodAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var specification = new UsersCreatedInPeriodSpecification(startDate, endDate);
        return await GetAsync(specification, cancellationToken);
    }

    public async Task<List<User>> GetUsersWithRecentLoginAsync(int recentDays, CancellationToken cancellationToken = default)
    {
        var specification = new UsersWithRecentLoginSpecification(recentDays);
        return await GetAsync(specification, cancellationToken);
    }

    public async Task<List<User>> GetUsersWithFailedLoginAttemptsAsync(int minFailedAttempts, CancellationToken cancellationToken = default)
    {
        var specification = new UsersWithFailedLoginAttemptsSpecification(minFailedAttempts);
        return await GetAsync(specification, cancellationToken);
    }

    public async Task<List<User>> GetUsersRequiringSecurityAttentionAsync(CancellationToken cancellationToken = default)
    {
        var specification = new UsersRequiringSecurityAttentionSpecification();
        return await GetAsync(specification, cancellationToken);
    }

    public async Task<List<User>> GetUsersWithValidRefreshTokensAsync(CancellationToken cancellationToken = default)
    {
        var specification = new UsersWithValidRefreshTokensSpecification();
        return await GetAsync(specification, cancellationToken);
    }

    public async Task<List<User>> GetInactiveUsersForCleanupAsync(int inactiveDays, CancellationToken cancellationToken = default)
    {
        var specification = new InactiveUsersForCleanupSpecification(inactiveDays);
        return await GetAsync(specification, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToUpperInvariant();
        var query = _dbSet.Where(u => u.NormalizedEmail == normalizedEmail && !u.IsDeleted);

        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> EmployeeIdExistsAsync(string employeeId, int? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(u => u.EmployeeId == employeeId && !u.IsDeleted);

        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<UserStatistics> GetUserStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var users = await _dbSet.Where(u => !u.IsDeleted).ToListAsync(cancellationToken);

        var statistics = new UserStatistics
        {
            TotalUsers = users.Count,
            ActiveUsers = users.Count(u => u.Status == UserStatus.Active),
            InactiveUsers = users.Count(u => u.Status == UserStatus.Inactive),
            LockedUsers = users.Count(u => u.Status == UserStatus.Locked),
            SuspendedUsers = users.Count(u => u.Status == UserStatus.Suspended),
            PendingUsers = users.Count(u => u.Status == UserStatus.Pending),
            StaffUsers = users.Count(u => u.Role == UserRole.Staff),
            AdminUsers = users.Count(u => u.Role == UserRole.Administrator),
            ReceptionistUsers = users.Count(u => u.Role == UserRole.Receptionist),
            UsersWithFailedLogins = users.Count(u => u.FailedLoginAttempts > 0),
            LastUserCreated = users.Any() ? users.Max(u => u.CreatedOn) : null,
            LastSuccessfulLogin = users.Where(u => u.LastLoginDate.HasValue).Any() ?
                users.Where(u => u.LastLoginDate.HasValue).Max(u => u.LastLoginDate) : null
        };

        // Calculate users by department
        statistics.UsersByDepartment = users
            .Where(u => !string.IsNullOrEmpty(u.Department))
            .GroupBy(u => u.Department!)
            .ToDictionary(g => g.Key, g => g.Count());

        return statistics;
    }

    public async Task<UserActivitySummary> GetUserActivitySummaryAsync(int userId, int days = 30, CancellationToken cancellationToken = default)
    {
        var user = await GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new ArgumentException($"User with ID {userId} not found");
        }

        var startDate = DateTime.UtcNow.AddDays(-days);

        // Get audit logs for the user
        var auditLogs = await _context.Set<AuditLog>()
            .Where(al => al.UserId == userId && al.CreatedOn >= startDate)
            .ToListAsync(cancellationToken);

        var summary = new UserActivitySummary
        {
            UserId = userId,
            UserName = user.FullName,
            LastLogin = user.LastLoginDate,
            FailedLoginAttempts = user.FailedLoginAttempts
        };

        // Calculate activity metrics from audit logs
        summary.ActivityByType = auditLogs
            .GroupBy(al => al.Action)
            .ToDictionary(g => g.Key, g => g.Count());

        // Calculate missing fields from audit logs
        summary.LoginCount = auditLogs.Count(al => al.Action == "Login" && al.IsSuccess);
        summary.LastFailedLogin = auditLogs
            .Where(al => al.Action == "Login" && !al.IsSuccess)
            .OrderByDescending(al => al.CreatedOn)
            .FirstOrDefault()?.CreatedOn;
        summary.InvitationsCreated = auditLogs.Count(al => al.Action == "InvitationCreated");
        summary.PasswordChanges = auditLogs.Count(al => al.Action == "PasswordChanged");

        summary.RecentActions = auditLogs
            .OrderByDescending(al => al.CreatedOn)
            .Take(10)
            .Select(al => $"{al.Action} on {al.EntityName}")
            .ToList();

        return summary;
    }

    public async Task<PagedResultDto<UserActivityDto>> GetUserAuditLogsAsync(
    int userId, int days, int pageIndex, int pageSize, CancellationToken cancellationToken)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);

        var query = _context.Set<AuditLog>()
            .Where(al => al.UserId == userId && al.CreatedOn >= startDate);

        var totalCount = await query.CountAsync(cancellationToken);

        var auditLogs = await query
            .OrderByDescending(al => al.CreatedOn)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = auditLogs.Select(al => new UserActivityDto
        {
            Action = al.Action,
            Description = al.Description,
            Timestamp = al.CreatedOn,
            IpAddress = al.IpAddress,
            UserAgent = al.UserAgent,
            IsSuccess = al.IsSuccess
        }).ToList();

        return new PagedResultDto<UserActivityDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }


    public async Task<int> BulkUpdateStatusAsync(List<int> userIds, UserStatus status, int modifiedBy, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => userIds.Contains(u.Id))
            .ExecuteUpdateAsync(u => u
                .SetProperty(x => x.Status, status)
                .SetProperty(x => x.ModifiedBy, modifiedBy)
                .SetProperty(x => x.ModifiedOn, DateTime.UtcNow), cancellationToken);
    }

    public async Task<int> BulkUpdateRoleAsync(List<int> userIds, UserRole role, int modifiedBy, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => userIds.Contains(u.Id))
            .ExecuteUpdateAsync(u => u
                .SetProperty(x => x.Role, role)
                .SetProperty(x => x.ModifiedBy, modifiedBy)
                .SetProperty(x => x.ModifiedOn, DateTime.UtcNow), cancellationToken);
    }

    public async Task<int> BulkResetFailedLoginAttemptsAsync(List<int> userIds, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => userIds.Contains(u.Id))
            .ExecuteUpdateAsync(u => u
                .SetProperty(x => x.FailedLoginAttempts, 0)
                .SetProperty(x => x.IsLockedOut, false)
                .SetProperty(x => x.LockoutEnd, (DateTime?)null)
                .SetProperty(x => x.ModifiedOn, DateTime.UtcNow), cancellationToken);
    }

    public async Task<Dictionary<int, User>> GetUsersByIdsAsync(List<int> userIds, CancellationToken cancellationToken = default)
    {
        var users = await _dbSet
            .Where(u => userIds.Contains(u.Id) && !u.IsDeleted)
            .ToListAsync(cancellationToken);

        return users.ToDictionary(u => u.Id, u => u);
    }

    public new void Delete(User user)
    {
        _dbSet.Remove(user);
    }

    // Method moved from RepositoryExtensions
    public async Task<IEnumerable<User>> GetUsersByRoleAsync(
        string role,
        CancellationToken cancellationToken = default)
    {
        // Convert string role to UserRole enum
        var userRole = role switch
        {
            "Staff" => UserRole.Staff,
            "Administrator" => UserRole.Administrator,
            "Operator" => UserRole.Receptionist,
            _ => throw new ArgumentException($"Invalid role: {role}")
        };

        return await _dbSet
            .Where(u => u.Role == userRole && u.IsActive)
            .OrderBy(u => u.FullName)
            .ToListAsync(cancellationToken);
    }
}