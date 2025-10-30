using AutoMapper;
using VisitorManagementSystem.Api.Application.DTOs.Permissions;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Application.Mapping;

/// <summary>
/// AutoMapper profile for Permission entities
/// </summary>
public class PermissionMappingProfile : Profile
{
    public PermissionMappingProfile()
    {
        // Permission entity to DTO
        CreateMap<Permission, PermissionDto>();

        // Role entity to DTO
        CreateMap<Role, RoleDto>()
            .ForMember(dest => dest.PermissionCount, opt => opt.Ignore())
            .ForMember(dest => dest.UserCount, opt => opt.Ignore());

        // Role with permissions
        CreateMap<Role, RoleWithPermissionsDto>()
            .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src =>
                src.RolePermissions.Select(rp => rp.Permission).ToList()));

        // RolePermission to DTO
        CreateMap<RolePermission, RolePermissionDto>()
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.Name))
            .ForMember(dest => dest.PermissionName, opt => opt.MapFrom(src => src.Permission.Name))
            .ForMember(dest => dest.PermissionDisplayName, opt => opt.MapFrom(src => src.Permission.DisplayName))
            .ForMember(dest => dest.GrantedByUserName, opt => opt.MapFrom(src =>
                src.GrantedByUser != null ? src.GrantedByUser.FullName : "System"));

        // Create DTOs to entities
        CreateMap<CreateRoleDto, Role>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.RolePermissions, opt => opt.Ignore())
            .ForMember(dest => dest.Users, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsSystemRole, opt => opt.MapFrom(_ => false))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true));

        // Update DTOs to entities
        CreateMap<UpdateRoleDto, Role>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Name, opt => opt.Ignore())
            .ForMember(dest => dest.HierarchyLevel, opt => opt.Ignore())
            .ForMember(dest => dest.IsSystemRole, opt => opt.Ignore())
            .ForMember(dest => dest.RolePermissions, opt => opt.Ignore())
            .ForMember(dest => dest.Users, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore());
    }
}
