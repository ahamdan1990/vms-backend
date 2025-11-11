using System.Linq.Expressions;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Domain.Specifications;

/// <summary>
/// Specification for filtering visitors
/// </summary>
public class VisitorFilterSpecification : BaseSpecification<Visitor>
{
    public VisitorFilterSpecification(
        string? searchTerm = null,
        string? company = null,
        bool? isVip = null,
        bool? isBlacklisted = null,
        bool? isActive = null,
        string? nationality = null,
        string? securityClearance = null,
        int? minVisitCount = null,
        int? maxVisitCount = null,
        DateTime? createdFrom = null,
        DateTime? createdTo = null,
        DateTime? lastVisitFrom = null,
        DateTime? lastVisitTo = null,
        bool includeDeleted = false,
        string sortBy = "FullName",
        string sortDirection = "asc",
        int pageIndex = 0,
        int pageSize = 20)
    {
        // Build criteria expression
        var criteria = BuildCriteriaExpression(
            searchTerm, company, isVip, isBlacklisted, isActive, nationality,
            securityClearance, minVisitCount, maxVisitCount, createdFrom,
            createdTo, lastVisitFrom, lastVisitTo, includeDeleted);

        if (criteria != null)
        {
            Criteria = criteria;
        }

        // Apply sorting
        ApplySorting(sortBy, sortDirection);

        // Apply paging
        ApplyPaging(pageIndex * pageSize, pageSize);

        // Add includes for related data
        AddInclude(v => v.EmergencyContacts);
        AddInclude("CreatedByUser");
        AddInclude("ModifiedByUser");

        // Disable tracking for read-only queries
        AsNoTracking();
    }

    private Expression<Func<Visitor, bool>>? BuildCriteriaExpression(
        string? searchTerm,
        string? company,
        bool? isVip,
        bool? isBlacklisted,
        bool? isActive,
        string? nationality,
        string? securityClearance,
        int? minVisitCount,
        int? maxVisitCount,
        DateTime? createdFrom,
        DateTime? createdTo,
        DateTime? lastVisitFrom,
        DateTime? lastVisitTo,
        bool includeDeleted)
    {
        Expression<Func<Visitor, bool>>? criteria = null;

        // Deleted filter
        if (!includeDeleted)
        {
            criteria = CombineWithAnd(criteria, v => !v.IsDeleted);
        }

        // Active filter
        if (isActive.HasValue)
        {
            criteria = CombineWithAnd(criteria, v => v.IsActive == isActive.Value);
        }

        // Search term (name, email, company)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower().Trim();
            criteria = CombineWithAnd(criteria, v =>
                v.FirstName.ToLower().Contains(term) ||
                v.LastName.ToLower().Contains(term) ||
                v.Email.Value.ToLower().Contains(term) ||
                (v.Company != null && v.Company.ToLower().Contains(term)));
        }

        // Company filter
        if (!string.IsNullOrWhiteSpace(company))
        {
            criteria = CombineWithAnd(criteria, v => 
                v.Company != null && v.Company.ToLower().Contains(company.ToLower()));
        }

        // VIP filter
        if (isVip.HasValue)
        {
            criteria = CombineWithAnd(criteria, v => v.IsVip == isVip.Value);
        }

        // Blacklisted filter
        if (isBlacklisted.HasValue)
        {
            criteria = CombineWithAnd(criteria, v => v.IsBlacklisted == isBlacklisted.Value);
        }

        // Nationality filter
        if (!string.IsNullOrWhiteSpace(nationality))
        {
            criteria = CombineWithAnd(criteria, v => 
                v.Nationality != null && v.Nationality.ToLower().Contains(nationality.ToLower()));
        }

        // Security clearance filter
        if (!string.IsNullOrWhiteSpace(securityClearance))
        {
            criteria = CombineWithAnd(criteria, v => 
                v.SecurityClearance != null && v.SecurityClearance.ToLower().Contains(securityClearance.ToLower()));
        }

        // Visit count filters
        if (minVisitCount.HasValue)
        {
            criteria = CombineWithAnd(criteria, v => v.VisitCount >= minVisitCount.Value);
        }

        if (maxVisitCount.HasValue)
        {
            criteria = CombineWithAnd(criteria, v => v.VisitCount <= maxVisitCount.Value);
        }

        // Date filters
        if (createdFrom.HasValue)
        {
            criteria = CombineWithAnd(criteria, v => v.CreatedOn >= createdFrom.Value);
        }

        if (createdTo.HasValue)
        {
            var endDate = createdTo.Value.Date.AddDays(1).AddTicks(-1);
            criteria = CombineWithAnd(criteria, v => v.CreatedOn <= endDate);
        }

        if (lastVisitFrom.HasValue)
        {
            criteria = CombineWithAnd(criteria, v => v.LastVisitDate >= lastVisitFrom.Value);
        }

        if (lastVisitTo.HasValue)
        {
            var endDate = lastVisitTo.Value.Date.AddDays(1).AddTicks(-1);
            criteria = CombineWithAnd(criteria, v => v.LastVisitDate <= endDate);
        }

        return criteria;
    }

    private void ApplySorting(string sortBy, string sortDirection)
    {
        var isDescending = sortDirection.ToLower() == "desc";

        Expression<Func<Visitor, object>> orderExpression = sortBy.ToLower() switch
        {
            "firstname" => v => v.FirstName,
            "lastname" => v => v.LastName,
            "fullname" => v => v.FirstName + " " + v.LastName,
            "email" => v => v.Email.Value,
            "company" => v => v.Company ?? string.Empty,
            "createdon" => v => v.CreatedOn,
            "lastvisitdate" => v => v.LastVisitDate ?? DateTime.MinValue,
            "visitcount" => v => v.VisitCount,
            "nationality" => v => v.Nationality ?? string.Empty,
            _ => v => v.FirstName + " " + v.LastName
        };

        if (isDescending)
        {
            ApplyOrderByDescending(orderExpression);
        }
        else
        {
            ApplyOrderBy(orderExpression);
        }
    }

    private static Expression<Func<Visitor, bool>>? CombineWithAnd(
        Expression<Func<Visitor, bool>>? first,
        Expression<Func<Visitor, bool>> second)
    {
        if (first == null)
            return second;

        var parameter = Expression.Parameter(typeof(Visitor), "v");
        var leftVisitor = (LambdaExpression)first;
        var rightVisitor = (LambdaExpression)second;

        var left = leftVisitor.Body.Replace(leftVisitor.Parameters[0], parameter);
        var right = rightVisitor.Body.Replace(rightVisitor.Parameters[0], parameter);

        return Expression.Lambda<Func<Visitor, bool>>(
            Expression.AndAlso(left, right), parameter);
    }
}

/// <summary>
/// Extension method for parameter replacement in expressions
/// </summary>
public static class ExpressionExtensions
{
    public static Expression Replace(this Expression expression, Expression searchEx, Expression replaceEx)
    {
        return new ReplaceVisitor(searchEx, replaceEx).Visit(expression) ?? expression;
    }
}

/// <summary>
/// Visitor for replacing expressions
/// </summary>
internal class ReplaceVisitor : ExpressionVisitor
{
    private readonly Expression _from;
    private readonly Expression _to;

    public ReplaceVisitor(Expression from, Expression to)
    {
        _from = from;
        _to = to;
    }

    public override Expression? Visit(Expression? node)
    {
        return node == _from ? _to : base.Visit(node);
    }
}
