using System.Linq.Expressions;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Domain.Specifications;

/// <summary>
/// Base specification class for building queries
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public abstract class BaseSpecification<T> where T : BaseEntity
{
    /// <summary>
    /// Criteria expression for filtering
    /// </summary>
    public Expression<Func<T, bool>>? Criteria { get; set; }

    /// <summary>
    /// Include expressions for related entities
    /// </summary>
    public List<Expression<Func<T, object>>> Includes { get; private set; } = new();

    /// <summary>
    /// Include string expressions for related entities
    /// </summary>
    public List<string> IncludeStrings { get; private set; } = new();

    /// <summary>
    /// Order by expression
    /// </summary>
    public Expression<Func<T, object>>? OrderBy { get; set; }

    /// <summary>
    /// Order by descending expression
    /// </summary>
    public Expression<Func<T, object>>? OrderByDescending { get; set; }

    /// <summary>
    /// Group by expression
    /// </summary>
    public Expression<Func<T, object>>? GroupBy { get; set; }

    /// <summary>
    /// Take count for pagination
    /// </summary>
    public int? Take { get; set; }

    /// <summary>
    /// Skip count for pagination
    /// </summary>
    public int? Skip { get; set; }

    /// <summary>
    /// Whether to apply paging
    /// </summary>
    public bool IsPagingEnabled => Skip.HasValue && Take.HasValue;

    /// <summary>
    /// Whether to track changes
    /// </summary>
    public bool IsTracking { get; set; } = true;

    /// <summary>
    /// Whether to split queries
    /// </summary>
    public bool IsSplitQuery { get; set; } = false;

    /// <summary>
    /// Adds an include expression
    /// </summary>
    /// <param name="includeExpression">Include expression</param>
    protected void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    /// <summary>
    /// Adds an include string
    /// </summary>
    /// <param name="includeString">Include string</param>
    protected void AddInclude(string includeString)
    {
        IncludeStrings.Add(includeString);
    }

    /// <summary>
    /// Applies paging
    /// </summary>
    /// <param name="skip">Skip count</param>
    /// <param name="take">Take count</param>
    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
    }

    /// <summary>
    /// Applies ordering
    /// </summary>
    /// <param name="orderByExpression">Order by expression</param>
    protected void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        OrderBy = orderByExpression;
    }

    /// <summary>
    /// Applies descending ordering
    /// </summary>
    /// <param name="orderByDescExpression">Order by descending expression</param>
    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescExpression)
    {
        OrderByDescending = orderByDescExpression;
    }

    /// <summary>
    /// Disables change tracking
    /// </summary>
    protected void AsNoTracking()
    {
        IsTracking = false;
    }

    /// <summary>
    /// Enables split query
    /// </summary>
    protected void AsSplitQuery()
    {
        IsSplitQuery = true;
    }
}
