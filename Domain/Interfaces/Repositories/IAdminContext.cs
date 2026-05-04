using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

/// <summary>
/// Domain-scoped context for the administration aggregate.
/// Handlers dealing with roles, permissions, system configuration, and audit logs should
/// depend on this rather than the full IUnitOfWork.
/// </summary>
public interface IAdminContext : ITransactionalContext
{
    IRoleRepository Roles { get; }
    IRolePermissionRepository RolePermissions { get; }
    IPermissionRepository Permissions { get; }
    ISystemConfigurationRepository SystemConfigurations { get; }
    IConfigurationAuditRepository ConfigurationAudits { get; }
    IGenericRepository<AuditLog> AuditLogs { get; }
}
