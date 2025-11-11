namespace VisitorManagementSystem.Api.Application.DTOs.Departments;

/// <summary>
/// Department data transfer object
/// </summary>
public class DepartmentDto
{
    /// <summary>
    /// Department ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Department name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Short code or identifier
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Manager ID
    /// </summary>
    public int? ManagerId { get; set; }

    /// <summary>
    /// Manager name
    /// </summary>
    public string? ManagerName { get; set; }

    /// <summary>
    /// Parent department ID
    /// </summary>
    public int? ParentDepartmentId { get; set; }

    /// <summary>
    /// Parent department name
    /// </summary>
    public string? ParentDepartmentName { get; set; }

    /// <summary>
    /// Contact email
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Contact phone
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Office location
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Budget allocated
    /// </summary>
    public decimal? Budget { get; set; }

    /// <summary>
    /// Display order for UI
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Number of users in department
    /// </summary>
    public int UserCount { get; set; }

    /// <summary>
    /// Is department active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Date created
    /// </summary>
    public DateTime CreatedOn { get; set; }

    /// <summary>
    /// Date last modified
    /// </summary>
    public DateTime? ModifiedOn { get; set; }

    /// <summary>
    /// Child departments
    /// </summary>
    public List<DepartmentDto> ChildDepartments { get; set; } = new();
}

/// <summary>
/// Create department data transfer object
/// </summary>
public class CreateDepartmentDto
{
    /// <summary>
    /// Department name (required)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Short code (required)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Description (optional)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Manager ID (optional)
    /// </summary>
    public int? ManagerId { get; set; }

    /// <summary>
    /// Parent department ID for hierarchical organization (optional)
    /// </summary>
    public int? ParentDepartmentId { get; set; }

    /// <summary>
    /// Contact email (optional)
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Contact phone (optional)
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Office location (optional)
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Budget allocated (optional)
    /// </summary>
    public decimal? Budget { get; set; }

    /// <summary>
    /// Display order (optional)
    /// </summary>
    public int DisplayOrder { get; set; } = 0;
}

/// <summary>
/// Update department data transfer object
/// </summary>
public class UpdateDepartmentDto
{
    /// <summary>
    /// Department name (optional)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Description (optional)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Manager ID (optional)
    /// </summary>
    public int? ManagerId { get; set; }

    /// <summary>
    /// Parent department ID (optional)
    /// </summary>
    public int? ParentDepartmentId { get; set; }

    /// <summary>
    /// Contact email (optional)
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Contact phone (optional)
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Office location (optional)
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Budget allocated (optional)
    /// </summary>
    public decimal? Budget { get; set; }

    /// <summary>
    /// Display order (optional)
    /// </summary>
    public int? DisplayOrder { get; set; }
}
