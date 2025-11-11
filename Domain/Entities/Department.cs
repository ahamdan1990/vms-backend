using System;
using System.Collections.Generic;

namespace VisitorManagementSystem.Api.Domain.Entities
{
    /// <summary>
    /// Department entity - organizational units within the company
    /// Users can be assigned to departments for organizational hierarchy
    /// </summary>
    public class Department : SoftDeleteEntity
    {
        /// <summary>
        /// Department name (required)
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Short code or identifier (e.g., "IT", "HR", "SALES") (required)
        /// </summary>
        public required string Code { get; set; }

        /// <summary>
        /// Detailed description of the department (optional)
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Manager of the department (optional)
        /// </summary>
        public int? ManagerId { get; set; }
        public User? Manager { get; set; }

        /// <summary>
        /// Parent department (for hierarchical organization) (optional)
        /// </summary>
        public int? ParentDepartmentId { get; set; }
        public Department? ParentDepartment { get; set; }

        /// <summary>
        /// Contact email for the department (optional)
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Contact phone for the department (optional)
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// Location/Office of the department (optional)
        /// </summary>
        public string? Location { get; set; }

        /// <summary>
        /// Budget allocated to department
        /// </summary>
        public decimal? Budget { get; set; }

        /// <summary>
        /// Display order for UI presentation
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Users in this department
        /// </summary>
        public List<User> Users { get; set; } = [];

        /// <summary>
        /// Child departments (if hierarchical)
        /// </summary>
        public List<Department> ChildDepartments { get; set; } = [];
    }
}
