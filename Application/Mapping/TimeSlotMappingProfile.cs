using AutoMapper;
using VisitorManagementSystem.Api.Application.DTOs.TimeSlots;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Application.Mapping;

/// <summary>
/// AutoMapper profile for time slot mappings
/// </summary>
public class TimeSlotMappingProfile : Profile
{
    public TimeSlotMappingProfile()
    {
        // Entity to DTO mappings
        CreateMap<TimeSlot, TimeSlotDto>()
            .ForMember(dest => dest.LocationName, opt => opt.MapFrom(src => src.Location != null ? src.Location.Name : null))
            .ForMember(dest => dest.MaxVisitors, opt => opt.MapFrom(src => src.Location != null ? src.Location.MaxCapacity : 0))
            .ForMember(dest => dest.DurationMinutes, opt => opt.MapFrom(src => src.DurationMinutes))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedOn))
            .ForMember(dest => dest.ModifiedAt, opt => opt.MapFrom(src => src.ModifiedOn));


        // Command to Entity mappings
        CreateMap<Application.Commands.TimeSlots.CreateTimeSlotCommand, TimeSlot>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedOn, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedOn, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedOn, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Location, opt => opt.Ignore())
            .ForMember(dest => dest.AllowOverlapping, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy));

        CreateMap<Application.Commands.TimeSlots.UpdateTimeSlotCommand, TimeSlot>()
            .ForMember(dest => dest.CreatedOn, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedOn, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedOn, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Location, opt => opt.Ignore())
            .ForMember(dest => dest.AllowOverlapping, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.MapFrom(src => src.ModifiedBy));

        // DTO to Command mappings
        CreateMap<CreateTimeSlotDto, Application.Commands.TimeSlots.CreateTimeSlotCommand>()
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore());

        CreateMap<UpdateTimeSlotDto, Application.Commands.TimeSlots.UpdateTimeSlotCommand>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore());
    }
}
