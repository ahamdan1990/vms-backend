using AutoMapper;
using VisitorManagementSystem.Api.Application.DTOs.Locations;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Application.Mapping;

/// <summary>
/// AutoMapper profile for location mappings
/// </summary>
public class LocationMappingProfile : Profile
{
    public LocationMappingProfile()
    {
        // Entity to DTO mappings
        CreateMap<Location, LocationDto>()
            .ForMember(dest => dest.ParentLocationName, opt => opt.MapFrom(src => src.ParentLocation != null ? src.ParentLocation.Name : null))
            .ForMember(dest => dest.ChildLocations, opt => opt.MapFrom(src => src.ChildLocations))
            .ForMember(dest => dest.CreatedByUser, opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.FullName : null))
            .ForMember(dest => dest.ModifiedByUser, opt => opt.MapFrom(src => src.ModifiedByUser != null ? src.ModifiedByUser.FullName : null))
            .ForMember(dest => dest.DeletedByUser, opt => opt.MapFrom(src => src.DeletedByUser != null ? src.DeletedByUser.FullName : null))
            .ForMember(dest => dest.InvitationsCount, opt => opt.MapFrom(src => src.Invitations.Count));

        // Command to Entity mappings
        CreateMap<Application.Commands.Locations.CreateLocationCommand, Location>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedOn, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedOn, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedOn, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ParentLocation, opt => opt.Ignore())
            .ForMember(dest => dest.ChildLocations, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.Invitations, opt => opt.Ignore());

        CreateMap<Application.Commands.Locations.UpdateLocationCommand, Location>()
            .ForMember(dest => dest.CreatedOn, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedOn, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedOn, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ParentLocation, opt => opt.Ignore())
            .ForMember(dest => dest.ChildLocations, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.Invitations, opt => opt.Ignore());

        // DTO to Command mappings
        CreateMap<CreateLocationDto, Application.Commands.Locations.CreateLocationCommand>()
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore());

        CreateMap<UpdateLocationDto, Application.Commands.Locations.UpdateLocationCommand>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());
    }
}
