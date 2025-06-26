namespace VisitorManagementSystem.Api.Application.DTOs.Common;

/// <summary>
/// Paginated result data transfer object
/// </summary>
/// <typeparam name="T">Item type</typeparam>
public class PagedResultDto<T>
{
    /// <summary>
    /// List of items
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Total number of items
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page index (0-based)
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => PageIndex > 0;

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage => PageIndex < TotalPages - 1;

    /// <summary>
    /// Creates a paginated result
    /// </summary>
    /// <param name="items">Items for current page</param>
    /// <param name="totalCount">Total number of items</param>
    /// <param name="pageIndex">Current page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated result</returns>
    public static PagedResultDto<T> Create(List<T> items, int totalCount, int pageIndex, int pageSize)
    {
        return new PagedResultDto<T>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }
}