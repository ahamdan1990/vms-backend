using System.Linq.Expressions;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Domain.Specifications;

/// <summary>
/// Base specification class for implementing the specification pattern
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public abstract class BaseSpecification<T>
{
    /// <summary>
    /// The criteria expression
    /// </summary>
    public Expression<Func<T, bool>>? Criteria { get; protected set; }

    /// <summary>
    /// List of include expressions for eager loading
    /// </summary>
    public List<Expression<Func<T, object>>> Includes { get; } = new();

    /// <summary>
    /// List of include string expressions for eager loading
    /// </summary>
    public List<string> IncludeStrings { get; } = new();

    /// <summary>
    /// Order by expression
    /// </summary>
    public Expression<Func<T, object>>? OrderBy { get; private set; }

    /// <summary>
    /// Order by descending expression
    /// </summary>
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }

    /// <summary>
    /// Group by expression
    /// </summary>
    public Expression<Func<T, object>>? GroupBy { get; private set; }

    /// <summary>
    /// Take count for pagination
    /// </summary>
    public int Take { get; private set; }

    /// <summary>
    /// Skip count for pagination
    /// </summary>
    public int Skip { get; private set; }

    /// <summary>
    /// Whether paging is enabled
    /// </summary>
    public bool IsPagingEnabled { get; private set; }

    /// <summary>
    /// Adds an include expression
    /// </summary>
    /// <param name="includeExpression">Include expression</param>
    protected virtual void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    /// <summary>
    /// Adds an include string
    /// </summary>
    /// <param name="includeString">Include string</param>
    protected virtual void AddInclude(string includeString)
    {
        IncludeStrings.Add(includeString);
    }

    /// <summary>
    /// Applies ordering
    /// </summary>
    /// <param name="orderByExpression">Order by expression</param>
    protected virtual void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        OrderBy = orderByExpression;
    }

    /// <summary>
    /// Applies descending ordering
    /// </summary>
    /// <param name="orderByDescExpression">Order by descending expression</param>
    protected virtual void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescExpression)
    {
        OrderByDescending = orderByDescExpression;
    }

    /// <summary>
    /// Applies grouping
    /// </summary>
    /// <param name="groupByExpression">Group by expression</param>
    protected virtual void ApplyGroupBy(Expression<Func<T, object>> groupByExpression)
    {
        GroupBy = groupByExpression;
    }

    /// <summary>
    /// Applies paging
    /// </summary>
    /// <param name="skip">Skip count</param>
    /// <param name="take">Take count</param>
    protected virtual void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }
}

/// <summary>
/// Specification for active users
/// </summary>
public class ActiveUsersSpecification : BaseSpecification<User>
{
    public ActiveUsersSpecification()
    {
        Criteria = u => u.IsActive && !u.IsDeleted && u.Status == UserStatus.Active;
        ApplyOrderBy(u => u.LastName);
        AddInclude(u => u.RefreshTokens);
    }
}

/// <summary>
/// Specification for users by role
/// </summary>
public class UsersByRoleSpecification : BaseSpecification<User>
{
    public UsersByRoleSpecification(UserRole role)
    {
        Criteria = u => u.Role == role && u.IsActive && !u.IsDeleted;
        ApplyOrderBy(u => u.LastName);
    }
}

/// <summary>
/// Specification for users by status
/// </summary>
public class UsersByStatusSpecification : BaseSpecification<User>
{
    public UsersByStatusSpecification(UserStatus status)
    {
        Criteria = u => u.Status == status && !u.IsDeleted;
        ApplyOrderBy(u => u.LastName);
    }
}

/// <summary>
/// Specification for users by email
/// </summary>
public class UserByEmailSpecification : BaseSpecification<User>
{
    public UserByEmailSpecification(string email)
    {
        Criteria = u => u.Email.Value.ToLower() == email.ToLower() && !u.IsDeleted;
        AddInclude(u => u.RefreshTokens);
    }
}

/// <summary>
/// Specification for users by employee ID
/// </summary>
public class UserByEmployeeIdSpecification : BaseSpecification<User>
{
    public UserByEmployeeIdSpecification(string employeeId)
    {
        Criteria = u => u.EmployeeId == employeeId && !u.IsDeleted;
    }
}

/// <summary>
/// Specification for locked out users
/// </summary>
public class LockedOutUsersSpecification : BaseSpecification<User>
{
    public LockedOutUsersSpecification()
    {
        Criteria = u => u.IsLockedOut && u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTime.UtcNow && !u.IsDeleted;
        ApplyOrderByDescending(u => u.LockoutEnd);
    }
}

/// <summary>
/// Specification for users with expired passwords
/// </summary>
public class UsersWithExpiredPasswordsSpecification : BaseSpecification<User>
{
    public UsersWithExpiredPasswordsSpecification(int passwordExpiryDays)
    {
        var expiryDate = DateTime.UtcNow.AddDays(-passwordExpiryDays);
        Criteria = u => u.PasswordChangedDate.HasValue &&
                       u.PasswordChangedDate.Value < expiryDate &&
                       u.IsActive &&
                       !u.IsDeleted &&
                       u.Status == UserStatus.Active;
        ApplyOrderBy(u => u.PasswordChangedDate);
    }
}

/// <summary>
/// Specification for users who must change password
/// </summary>
public class UsersMustChangePasswordSpecification : BaseSpecification<User>
{
    public UsersMustChangePasswordSpecification()
    {
        Criteria = u => u.MustChangePassword && u.IsActive && !u.IsDeleted;
        ApplyOrderBy(u => u.LastName);
    }
}

/// <summary>
/// Specification for inactive users for cleanup
/// </summary>
public class InactiveUsersForCleanupSpecification : BaseSpecification<User>
{
    public InactiveUsersForCleanupSpecification(int inactiveDays)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-inactiveDays);
        Criteria = u => (!u.LastLoginDate.HasValue || u.LastLoginDate.Value < cutoffDate) &&
                       u.Status == UserStatus.Inactive &&
                       !u.IsDeleted;
        ApplyOrderBy(u => u.LastLoginDate);
    }
}

/// <summary>
/// Specification for searching users
/// </summary>
public class UserSearchSpecification : BaseSpecification<User>
{
    public UserSearchSpecification(string searchTerm, UserRole? role = null, UserStatus? status = null, string? department = null)
    {
        var criteria = BuildSearchCriteria(searchTerm, role, status, department);
        Criteria = criteria;
        ApplyOrderBy(u => u.LastName);
    }

    private Expression<Func<User, bool>> BuildSearchCriteria(string searchTerm, UserRole? role, UserStatus? status, string? department)
    {
        Expression<Func<User, bool>> criteria = u => !u.IsDeleted;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerSearchTerm = searchTerm.ToLower();
            Expression<Func<User, bool>> searchCriteria = u =>
                u.FirstName.ToLower().Contains(lowerSearchTerm) ||
                u.LastName.ToLower().Contains(lowerSearchTerm) ||
                u.Email.Value.ToLower().Contains(lowerSearchTerm) ||
                (u.EmployeeId != null && u.EmployeeId.ToLower().Contains(lowerSearchTerm)) ||
                (u.Department != null && u.Department.ToLower().Contains(lowerSearchTerm));

            criteria = CombineExpressions(criteria, searchCriteria);
        }

        if (role.HasValue)
        {
            Expression<Func<User, bool>> roleCriteria = u => u.Role == role.Value;
            criteria = CombineExpressions(criteria, roleCriteria);
        }

        if (status.HasValue)
        {
            Expression<Func<User, bool>> statusCriteria = u => u.Status == status.Value;
            criteria = CombineExpressions(criteria, statusCriteria);
        }

        if (!string.IsNullOrWhiteSpace(department))
        {
            var lowerDept = department.ToLower();
            Expression<Func<User, bool>> deptCriteria = u =>
                u.Department != null && u.Department.ToLower() == lowerDept;

            criteria = CombineExpressions(criteria, deptCriteria);
        }

        return criteria;
    }

    private Expression<Func<User, bool>> CombineExpressions(Expression<Func<User, bool>> first, Expression<Func<User, bool>> second)
    {
        var parameter = Expression.Parameter(typeof(User), "u");
        var firstBody = ReplaceParameter(first.Body, first.Parameters[0], parameter);
        var secondBody = ReplaceParameter(second.Body, second.Parameters[0], parameter);
        var combined = Expression.AndAlso(firstBody, secondBody);
        return Expression.Lambda<Func<User, bool>>(combined, parameter);
    }

    private Expression ReplaceParameter(Expression expression, ParameterExpression oldParameter, ParameterExpression newParameter)
    {
        return new ParameterReplacer(oldParameter, newParameter).Visit(expression);
    }
}

/// <summary>
/// Specification for users created within a date range
/// </summary>
public class UsersCreatedInPeriodSpecification : BaseSpecification<User>
{
    public UsersCreatedInPeriodSpecification(DateTime startDate, DateTime endDate)
    {
        Criteria = u => u.CreatedOn >= startDate && u.CreatedOn <= endDate && !u.IsDeleted;
        ApplyOrderByDescending(u => u.CreatedOn);
    }
}

/// <summary>
/// Specification for users by department
/// </summary>
public class UsersByDepartmentSpecification : BaseSpecification<User>
{
    public UsersByDepartmentSpecification(string department)
    {
        Criteria = u => u.Department != null &&
                       u.Department.ToLower() == department.ToLower() &&
                       u.IsActive &&
                       !u.IsDeleted;
        ApplyOrderBy(u => u.LastName);
    }
}

/// <summary>
/// Specification for users with recent login
/// </summary>
public class UsersWithRecentLoginSpecification : BaseSpecification<User>
{
    public UsersWithRecentLoginSpecification(int recentDays)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-recentDays);
        Criteria = u => u.LastLoginDate.HasValue &&
                       u.LastLoginDate.Value >= cutoffDate &&
                       u.IsActive &&
                       !u.IsDeleted;
        ApplyOrderByDescending(u => u.LastLoginDate);
    }
}

/// <summary>
/// Specification for users with failed login attempts
/// </summary>
public class UsersWithFailedLoginAttemptsSpecification : BaseSpecification<User>
{
    public UsersWithFailedLoginAttemptsSpecification(int minFailedAttempts)
    {
        Criteria = u => u.FailedLoginAttempts >= minFailedAttempts && !u.IsDeleted;
        ApplyOrderByDescending(u => u.FailedLoginAttempts);
    }
}

/// <summary>
/// Specification for paginated users
/// </summary>
public class PaginatedUsersSpecification : BaseSpecification<User>
{
    public PaginatedUsersSpecification(int pageIndex, int pageSize, string? sortBy = null, bool sortDescending = false)
    {
        Criteria = u => !u.IsDeleted;

        // Apply sorting
        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            switch (sortBy.ToLower())
            {
                case "firstname":
                    if (sortDescending)
                        ApplyOrderByDescending(u => u.FirstName);
                    else
                        ApplyOrderBy(u => u.FirstName);
                    break;
                case "lastname":
                    if (sortDescending)
                        ApplyOrderByDescending(u => u.LastName);
                    else
                        ApplyOrderBy(u => u.LastName);
                    break;
                case "email":
                    if (sortDescending)
                        ApplyOrderByDescending(u => u.Email.Value);
                    else
                        ApplyOrderBy(u => u.Email.Value);
                    break;
                case "role":
                    if (sortDescending)
                        ApplyOrderByDescending(u => u.Role);
                    else
                        ApplyOrderBy(u => u.Role);
                    break;
                case "status":
                    if (sortDescending)
                        ApplyOrderByDescending(u => u.Status);
                    else
                        ApplyOrderBy(u => u.Status);
                    break;
                case "createdon":
                    if (sortDescending)
                        ApplyOrderByDescending(u => u.CreatedOn);
                    else
                        ApplyOrderBy(u => u.CreatedOn);
                    break;
                case "lastlogin":
                    if (sortDescending)
                        ApplyOrderByDescending(u => u.LastLoginDate);
                    else
                        ApplyOrderBy(u => u.LastLoginDate);
                    break;
                default:
                    ApplyOrderBy(u => u.LastName);
                    break;
            }
        }
        else
        {
            ApplyOrderBy(u => u.LastName);
        }

        // Apply paging
        ApplyPaging(pageIndex * pageSize, pageSize);
    }
}

/// <summary>
/// Specification for users requiring security attention
/// </summary>
public class UsersRequiringSecurityAttentionSpecification : BaseSpecification<User>
{
    public UsersRequiringSecurityAttentionSpecification()
    {
        Criteria = u => (u.IsLockedOut ||
                        u.FailedLoginAttempts >= 3 ||
                        u.Status == UserStatus.Suspended ||
                        u.MustChangePassword) &&
                       !u.IsDeleted;
        ApplyOrderByDescending(u => u.ModifiedOn);
    }
}

/// <summary>
/// Specification for users with valid refresh tokens
/// </summary>
public class UsersWithValidRefreshTokensSpecification : BaseSpecification<User>
{
    public UsersWithValidRefreshTokensSpecification()
    {
        Criteria = u => u.RefreshTokens.Any(rt => rt.IsActive &&
                                                  !rt.IsUsed &&
                                                  !rt.IsRevoked &&
                                                  rt.ExpiryDate > DateTime.UtcNow) &&
                       !u.IsDeleted;
        AddInclude(u => u.RefreshTokens);
        ApplyOrderBy(u => u.LastName);
    }
}

/// <summary>
/// Helper class for replacing parameters in expressions
/// </summary>
internal class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _oldParameter;
    private readonly ParameterExpression _newParameter;

    public ParameterReplacer(ParameterExpression oldParameter, ParameterExpression newParameter)
    {
        _oldParameter = oldParameter;
        _newParameter = newParameter;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == _oldParameter ? _newParameter : base.VisitParameter(node);
    }
}