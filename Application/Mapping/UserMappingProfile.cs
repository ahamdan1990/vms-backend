// Create: Application/Mapping/UserMappingProfile.cs

using AutoMapper;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Application.Commands.Users;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.Mapping;

/// <summary>
/// AutoMapper profile for User entity and related DTO mappings
/// </summary>
public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateUserMappings();
        CreateCommandMappings();
    }

    private void CreateUserMappings()
    {
        // User Entity -> UserDto (Main DTO)
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Value))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber != null ? src.PhoneNumber.Value : null))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.IsLockedOut, opt => opt.MapFrom(src => src.LockoutEnd.HasValue && src.LockoutEnd > DateTime.UtcNow))
            .ForMember(dest => dest.ProfilePhotoUrl, opt => opt.MapFrom(src => src.ProfilePhotoPath))
            .ForMember(dest => dest.TemporaryPassword, opt => opt.Ignore()); // Set manually when needed

        // User Entity -> UserListDto (For lists)
        CreateMap<User, UserListDto>()
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Value))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.IsLockedOut, opt => opt.MapFrom(src => src.LockoutEnd.HasValue && src.LockoutEnd > DateTime.UtcNow));

        // User Entity -> UserDetailDto (Extended details)
        CreateMap<User, UserDetailDto>()
            .IncludeBase<User, UserListDto>() // Include base mappings
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber != null ? src.PhoneNumber.Value : null))
            .ForMember(dest => dest.ProfilePhotoPath, opt => opt.MapFrom(src => src.ProfilePhotoPath))
            .ForMember(dest => dest.LockoutEnd, opt => opt.MapFrom(src => src.LockoutEnd))
            .ForMember(dest => dest.ActivitySummary, opt => opt.Ignore()) // Set manually if needed
            .ForMember(dest => dest.ActiveSessions, opt => opt.Ignore()); // Set manually if needed

        // User Entity -> UserProfileDto (Profile management)
        CreateMap<User, UserProfileDto>()
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Value))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber != null ? src.PhoneNumber.Value : null))
            .ForMember(dest => dest.ProfilePhotoUrl, opt => opt.MapFrom(src => src.ProfilePhotoPath));
    }

    private void CreateCommandMappings()
    {
        // CreateUserDto -> CreateUserCommand
        CreateMap<CreateUserDto, CreateUserCommand>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => Enum.Parse<UserRole>(src.Role)))
            .ForMember(dest => dest.TemporaryPassword, opt => opt.Ignore()) // Not used from DTO
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore()); // Set manually

        // UpdateUserDto -> UpdateUserCommand  
        CreateMap<UpdateUserDto, UpdateUserCommand>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => Enum.Parse<UserRole>(src.Role)))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Enum.Parse<UserStatus>(src.Status)))
            .ForMember(dest => dest.Id, opt => opt.Ignore()) // Set from route
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore()); // Set manually
    }
}