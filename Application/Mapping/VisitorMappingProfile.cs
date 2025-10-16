using AutoMapper;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Application.DTOs.Locations;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Application.DTOs.VisitPurposes;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Models;
using VisitorManagementSystem.Api.Domain.ValueObjects;

namespace VisitorManagementSystem.Api.Application.Mapping;

/// <summary>
/// AutoMapper profile for visitor domain
/// </summary>
public class VisitorMappingProfile : Profile
{
    public VisitorMappingProfile()
    {
        // Address value object mapping
        CreateMap<Address, AddressDto>()
            .ForMember(dest => dest.FormattedAddress, opt => opt.MapFrom(src => src.GetSingleLine()));

        CreateMap<Visitor, VisitorDto>()
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Value))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber != null ? src.PhoneNumber.Value : null))
            .ForMember(dest => dest.PhoneCountryCode, opt => opt.MapFrom(src => src.PhoneNumber != null ? src.PhoneNumber.CountryCode : null))
            .ForMember(dest => dest.PhoneType, opt => opt.MapFrom(src => src.PhoneNumber != null ? src.PhoneNumber.PhoneType : null))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
            .ForMember(dest => dest.ProfilePhotoUrl, opt => opt.MapFrom(src => GetProfilePhotoUrl(src)))
            .ForMember(dest => dest.CreatedByName, opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.FullName : null))
            .ForMember(dest => dest.ModifiedByName, opt => opt.MapFrom(src => src.ModifiedByUser != null ? src.ModifiedByUser.FullName : null))
            .ForMember(dest => dest.BlacklistedByName, opt => opt.MapFrom(src => src.BlacklistedByUser != null ? src.BlacklistedByUser.FullName : null));

        CreateMap<Visitor, VisitorListDto>()
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Value))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber != null ? src.PhoneNumber.Value : null))
            .ForMember(dest => dest.PhoneCountryCode, opt => opt.MapFrom(src => src.PhoneNumber != null ? src.PhoneNumber.CountryCode : null))
            .ForMember(dest => dest.PhoneType, opt => opt.MapFrom(src => src.PhoneNumber != null ? src.PhoneNumber.PhoneType : null))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName));

        // Add mapping for VisitorDisplayInfo (used in VIP visitors and statistics)
        CreateMap<VisitorDisplayInfo, VisitorListDto>();

        CreateMap<VisitorDocument, VisitorDocumentDto>()
            .ForMember(destinationMember => destinationMember.DownloadUrl, opt => opt.MapFrom(src => src.FilePath.ToString()))
            .ForMember(dest => dest.FormattedFileSize, opt => opt.MapFrom(src => FormatFileSize(src.FileSize)))
            .ForMember(dest => dest.IsExpired, opt => opt.MapFrom(src => src.ExpirationDate.HasValue && src.ExpirationDate.Value < DateTime.UtcNow))
            .ForMember(dest => dest.CreatedByName, opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.FullName : null))
            .ForMember(dest => dest.ModifiedByName, opt => opt.MapFrom(src => src.ModifiedByUser != null ? src.ModifiedByUser.FullName : null));

        CreateMap<VisitorNote, VisitorNoteDto>()
            .ForMember(dest => dest.IsFollowUpOverdue, opt => opt.MapFrom(src => src.IsFlagged && src.FollowUpDate.HasValue && src.FollowUpDate.Value < DateTime.UtcNow))
            .ForMember(dest => dest.CreatedByName, opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.FullName : null))
            .ForMember(dest => dest.ModifiedByName, opt => opt.MapFrom(src => src.ModifiedByUser != null ? src.ModifiedByUser.FullName : null));

        CreateMap<EmergencyContact, EmergencyContactDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber.FormattedValue))
            .ForMember(dest => dest.AlternatePhoneNumber, opt => opt.MapFrom(src => src.AlternatePhoneNumber != null ? src.AlternatePhoneNumber.FormattedValue : null))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email != null ? src.Email.Value : null));

        CreateMap<CompanyVisitorCount, CompanyVisitorCountDto>();

        CreateMap<VisitPurpose, VisitPurposeDto>();
        CreateMap<Location, LocationDto>()
            .ForMember(dest => dest.ParentLocationName, opt => opt.MapFrom(src => src.ParentLocation != null ? src.ParentLocation.Name : null));
    }

    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        return string.Format("{0:n1} {1}", number, suffixes[counter]);
    }

    private static string? GetProfilePhotoUrl(Visitor visitor)
    {
        // Base URL should come from configuration, but for now use relative paths
        // The frontend should prepend the correct backend URL
        
        // First check if visitor has ProfilePhotoPath set
        if (!string.IsNullOrEmpty(visitor.ProfilePhotoPath))
        {
            return $"http://localhost:5000/api/visitors/{visitor.Id}/photo";
        }

        // Then check for photo document
        var photoDocument = visitor.Documents?.FirstOrDefault(d => 
            d.DocumentType.Equals("Photo", StringComparison.OrdinalIgnoreCase) && 
            !d.IsDeleted);

        if (photoDocument != null)
        {
            return $"http://localhost:5000/api/visitors/{visitor.Id}/documents/{photoDocument.Id}/download";
        }

        return null;
    }
}
