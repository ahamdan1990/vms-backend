using AutoMapper;
using VisitorManagementSystem.Api.Application.DTOs.Companies;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.ValueObjects;

namespace VisitorManagementSystem.Api.Application.Mapping;

/// <summary>
/// AutoMapper profile for company mappings
/// </summary>
public class CompanyMappingProfile : Profile
{
    public CompanyMappingProfile()
    {
        // Entity to DTO mappings
        CreateMap<Company, CompanyDto>()
            // Value object mappings
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email != null ? src.Email.Value : null))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber != null ? src.PhoneNumber.FormattedValue : null))
            .ForMember(dest => dest.Street1, opt => opt.MapFrom(src => src.Address != null ? src.Address.Street1 : null))
            .ForMember(dest => dest.Street2, opt => opt.MapFrom(src => src.Address != null ? src.Address.Street2 : null))
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.Address != null ? src.Address.City : null))
            .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.Address != null ? src.Address.State : null))
            .ForMember(dest => dest.PostalCode, opt => opt.MapFrom(src => src.Address != null ? src.Address.PostalCode : null))
            .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.Address != null ? src.Address.Country : null))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => !src.IsDeleted));
    }
}
