namespace VisitorManagementSystem.Api.Application.DTOs.Users
{
    /// <summary>
    /// Role DTO
    /// </summary>
    public class RoleDto
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int HierarchyLevel { get; set; }
        public bool CanAssign { get; set; }
    }
}
