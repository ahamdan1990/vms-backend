using AutoMapper;
using VisitorManagementSystem.Api.Application.DTOs.Departments;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Application.Mapping;

/// <summary>
/// AutoMapper profile for department mappings
/// </summary>
public class DepartmentMappingProfile : Profile
{
    public DepartmentMappingProfile()
    {
        // Entity to DTO mappings
        CreateMap<Department, DepartmentDto>()
            .ForMember(dest => dest.ManagerName, opt => opt.MapFrom(src => src.Manager != null ? src.Manager.FullName : null))
            .ForMember(dest => dest.ParentDepartmentName, opt => opt.MapFrom(src => src.ParentDepartment != null ? src.ParentDepartment.Name : null))
            .ForMember(dest => dest.UserCount, opt => opt.MapFrom(src => src.Users != null ? src.Users.Count : 0))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => !src.IsDeleted))
            .ForMember(dest => dest.ChildDepartments, opt => opt.MapFrom(src => src.ChildDepartments));
    }
}
