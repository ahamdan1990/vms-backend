using AutoMapper;
using VisitorManagementSystem.Api.Application.DTOs.Cameras;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.ValueObjects;

namespace VisitorManagementSystem.Api.Application.Mapping;

/// <summary>
/// AutoMapper profile for camera mappings
/// Handles entity-DTO conversions for camera management operations
/// </summary>
public class CameraMappingProfile : Profile
{
    public CameraMappingProfile()
    {
        // Camera Entity to DTO mappings
        CreateMap<Camera, CameraDto>()
            .ForMember(dest => dest.LocationName, opt => opt.MapFrom(src => src.Location != null ? src.Location.Name : null))
            .ForMember(dest => dest.CreatedByName, opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.FullName : null))
            .ForMember(dest => dest.ModifiedByName, opt => opt.MapFrom(src => src.ModifiedByUser != null ? src.ModifiedByUser.FullName : null))
            .ForMember(dest => dest.CameraTypeDisplay, opt => opt.MapFrom(src => src.CameraType.ToString()))
            .ForMember(dest => dest.StatusDisplay, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.GetDisplayName()))
            .ForMember(dest => dest.IsOperational, opt => opt.MapFrom(src => src.IsOperational()))
            .ForMember(dest => dest.IsAvailableForStreaming, opt => opt.MapFrom(src => src.IsAvailableForStreaming()))
            .ForMember(dest => dest.ConnectionString, opt => opt.MapFrom(src => src.GetSafeConnectionString()))
            .ForMember(dest => dest.MinutesSinceLastHealthCheck, opt => opt.MapFrom(src => 
                src.LastHealthCheck.HasValue 
                    ? (int)(DateTime.UtcNow - src.LastHealthCheck.Value).TotalMinutes 
                    : (int?)null))
            .ForMember(dest => dest.MinutesSinceLastOnline, opt => opt.MapFrom(src =>
                src.LastOnlineTime.HasValue
                    ? (int)(DateTime.UtcNow - src.LastOnlineTime.Value).TotalMinutes
                    : (int?)null))
            .ForMember(dest => dest.Configuration, opt => opt.Ignore()); // Handled separately

        CreateMap<Camera, CameraListDto>()
            .ForMember(dest => dest.LocationName, opt => opt.MapFrom(src => src.Location != null ? src.Location.Name : null))
            .ForMember(dest => dest.CameraTypeDisplay, opt => opt.MapFrom(src => src.CameraType.ToString()))
            .ForMember(dest => dest.StatusDisplay, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.GetDisplayName()))
            .ForMember(dest => dest.IsOperational, opt => opt.MapFrom(src => src.IsOperational()))
            .ForMember(dest => dest.MinutesSinceLastHealthCheck, opt => opt.MapFrom(src =>
                src.LastHealthCheck.HasValue
                    ? (int)(DateTime.UtcNow - src.LastHealthCheck.Value).TotalMinutes
                    : (int?)null))
            .ForMember(dest => dest.HealthStatus, opt => opt.Ignore()); // Computed in query handlers

        // Camera Configuration mappings
        CreateMap<CameraConfiguration, CameraConfigurationDto>()
            .ForMember(dest => dest.ResolutionDisplay, opt => opt.MapFrom(src => src.GetResolutionString()));

        CreateMap<CameraConfigurationDto, CameraConfiguration>();

        // Command to Entity mappings
        CreateMap<Application.Commands.Cameras.CreateCameraCommand, Camera>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore()) // Set by handler
            .ForMember(dest => dest.ConfigurationJson, opt => opt.Ignore()) // Set by handler
            .ForMember(dest => dest.LastHealthCheck, opt => opt.Ignore())
            .ForMember(dest => dest.LastOnlineTime, opt => opt.Ignore())
            .ForMember(dest => dest.LastErrorMessage, opt => opt.Ignore())
            .ForMember(dest => dest.FailureCount, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedOn, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedOn, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedOn, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Location, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore());

        CreateMap<Application.Commands.Cameras.UpdateCameraCommand, Camera>()
            .ForMember(dest => dest.CreatedOn, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedOn, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedOn, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore()) // Handled by service logic
            .ForMember(dest => dest.ConfigurationJson, opt => opt.Ignore()) // Set by handler
            .ForMember(dest => dest.LastHealthCheck, opt => opt.Ignore())
            .ForMember(dest => dest.LastOnlineTime, opt => opt.Ignore())
            .ForMember(dest => dest.LastErrorMessage, opt => opt.Ignore())
            .ForMember(dest => dest.FailureCount, opt => opt.Ignore())
            .ForMember(dest => dest.Location, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore());

        // DTO to Command mappings (for API layer)
        CreateMap<CreateCameraDto, Application.Commands.Cameras.CreateCameraCommand>();

        CreateMap<UpdateCameraDto, Application.Commands.Cameras.UpdateCameraCommand>()
            .ForMember(dest => dest.Id, opt => opt.Ignore()) // Set by controller
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore()) // Set by controller
            .ForMember(dest => dest.TestConnection, opt => opt.Ignore()); // Set by controller
    }
}