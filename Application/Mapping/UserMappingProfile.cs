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
            .ForMember(dest => dest.PhoneCountryCode, opt => opt.MapFrom(src => src.PhoneNumber != null ? src.PhoneNumber.CountryCode : null))
            .ForMember(dest => dest.PhoneType, opt => opt.MapFrom(src => src.PhoneNumber != null ? src.PhoneNumber.PhoneType : null))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.IsLockedOut, opt => opt.MapFrom(src => src.LockoutEnd.HasValue && src.LockoutEnd > DateTime.UtcNow))
            .ForMember(dest => dest.ProfilePhotoUrl, opt => opt.MapFrom(src => src.ProfilePhotoPath))
            // Enhanced address mappings
            .ForMember(dest => dest.AddressType, opt => opt.MapFrom(src => src.Address != null ? src.Address.AddressType : null))
            .ForMember(dest => dest.Street1, opt => opt.MapFrom(src => src.Address != null ? src.Address.Street1 : null))
            .ForMember(dest => dest.Street2, opt => opt.MapFrom(src => src.Address != null ? src.Address.Street2 : null))
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Address != null ? src.Address.City : null))
            .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.Address != null ? src.Address.State : null))
            .ForMember(dest => dest.PostalCode, opt => opt.MapFrom(src => src.Address != null ? src.Address.PostalCode : null))
            .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.Address != null ? src.Address.Country : null))
            .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Address != null ? src.Address.Latitude : null))
            .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Address != null ? src.Address.Longitude : null));
            

        // User Entity -> UserListDto (For lists)
        CreateMap<User, UserListDto>()
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Value))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber != null ? src.PhoneNumber.Value : null))
            .ForMember(dest => dest.PhoneCountryCode, opt => opt.MapFrom(src => src.PhoneNumber != null ? src.PhoneNumber.CountryCode : null))
            .ForMember(dest => dest.PhoneType, opt => opt.MapFrom(src => src.PhoneNumber != null ? src.PhoneNumber.PhoneType : null))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.IsLockedOut, opt => opt.MapFrom(src => src.LockoutEnd.HasValue && src.LockoutEnd > DateTime.UtcNow));

        // User Entity -> UserDetailDto (Extended details)
        CreateMap<User, UserDetailDto>()
            .IncludeBase<User, UserListDto>() // Include base mappings
            .ForMember(dest => dest.ProfilePhotoPath, opt => opt.MapFrom(src => src.ProfilePhotoPath))
            .ForMember(dest => dest.LockoutEnd, opt => opt.MapFrom(src => src.LockoutEnd))
            // Enhanced address mappings for detail view
            .ForMember(dest => dest.AddressType, opt => opt.MapFrom(src => src.Address != null ? src.Address.AddressType : null))
            .ForMember(dest => dest.Street1, opt => opt.MapFrom(src => src.Address != null ? src.Address.Street1 : null))
            .ForMember(dest => dest.Street2, opt => opt.MapFrom(src => src.Address != null ? src.Address.Street2 : null))
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Address != null ? src.Address.City : null))
            .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.Address != null ? src.Address.State : null))
            .ForMember(dest => dest.PostalCode, opt => opt.MapFrom(src => src.Address != null ? src.Address.PostalCode : null))
            .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.Address != null ? src.Address.Country : null))
            .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Address != null ? src.Address.Latitude : null))
            .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Address != null ? src.Address.Longitude : null))
            .ForMember(dest => dest.ActivitySummary, opt => opt.Ignore()) // Set manually if needed
            .ForMember(dest => dest.ActiveSessions, opt => opt.Ignore()); // Set manually if needed

        // User Entity -> UserProfileDto (Profile management)
        CreateMap<User, UserProfileDto>()
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Value))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber != null ? src.PhoneNumber.Value : null))
            .ForMember(dest => dest.PhoneCountryCode, opt => opt.MapFrom(src => src.PhoneNumber != null ? src.PhoneNumber.CountryCode : null))
            .ForMember(dest => dest.PhoneType, opt => opt.MapFrom(src => src.PhoneNumber != null ? src.PhoneNumber.PhoneType : null))
            .ForMember(dest => dest.ProfilePhotoUrl, opt => opt.MapFrom(src => src.ProfilePhotoPath))
            // Address mappings
            .ForMember(dest => dest.AddressType, opt => opt.MapFrom(src => src.Address != null ? src.Address.AddressType : null))
            .ForMember(dest => dest.Street1, opt => opt.MapFrom(src => src.Address != null ? src.Address.Street1 : null))
            .ForMember(dest => dest.Street2, opt => opt.MapFrom(src => src.Address != null ? src.Address.Street2 : null))
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Address != null ? src.Address.City : null))
            .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.Address != null ? src.Address.State : null))
            .ForMember(dest => dest.PostalCode, opt => opt.MapFrom(src => src.Address != null ? src.Address.PostalCode : null))
            .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.Address != null ? src.Address.Country : null))
            .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Address != null ? src.Address.Latitude : null))
            .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Address != null ? src.Address.Longitude : null));
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

        // UpdateUserProfileDto -> UpdateUserProfileCommand
        CreateMap<UpdateUserProfileDto, UpdateUserProfileCommand>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore()); // Set manually
    }
}