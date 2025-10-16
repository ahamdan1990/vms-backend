using AutoMapper;
using VisitorManagementSystem.Api.Application.DTOs.VisitPurposes;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Application.Mapping;

/// <summary>
/// AutoMapper profile for visit purpose mappings
/// </summary>
public class VisitPurposeMappingProfile : Profile
{
    public VisitPurposeMappingProfile()
    {
        // Entity to DTO mappings
        CreateMap<VisitPurpose, VisitPurposeDto>()
            .ForMember(dest => dest.CreatedByUser, opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.FullName : null))
            .ForMember(dest => dest.ModifiedByUser, opt => opt.MapFrom(src => src.ModifiedByUser != null ? src.ModifiedByUser.FullName : null))
            .ForMember(dest => dest.DeletedByUser, opt => opt.MapFrom(src => src.DeletedByUser != null ? src.DeletedByUser.FullName : null))
            .ForMember(dest => dest.InvitationsCount, opt => opt.MapFrom(src => src.Invitations.Count));

        // Command to Entity mappings
        CreateMap<Application.Commands.VisitPurposes.CreateVisitPurposeCommand, VisitPurpose>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedOn, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedOn, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedOn, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.Invitations, opt => opt.Ignore());

        CreateMap<Application.Commands.VisitPurposes.UpdateVisitPurposeCommand, VisitPurpose>()
            .ForMember(dest => dest.CreatedOn, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedOn, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedOn, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.Invitations, opt => opt.Ignore());

        // DTO to Command mappings
        CreateMap<CreateVisitPurposeDto, Application.Commands.VisitPurposes.CreateVisitPurposeCommand>()
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore());

        CreateMap<UpdateVisitPurposeDto, Application.Commands.VisitPurposes.UpdateVisitPurposeCommand>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore());
    }
}
