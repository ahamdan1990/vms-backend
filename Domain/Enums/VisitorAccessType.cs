namespace VisitorManagementSystem.Api.Domain.Enums;

/// <summary>
/// Defines the type of access a user has to a visitor record
/// </summary>
public enum VisitorAccessType
{
    /// <summary>
    /// User created the visitor record
    /// </summary>
    Creator = 1,

    /// <summary>
    /// Access granted automatically due to attempting to create a duplicate visitor
    /// System detected an existing visitor with matching email/phone and granted access
    /// </summary>
    SharedDuplicate = 2,

    /// <summary>
    /// Access manually granted by an administrator
    /// </summary>
    GrantedByAdmin = 3
}
