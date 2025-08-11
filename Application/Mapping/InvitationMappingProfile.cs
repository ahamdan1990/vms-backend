using AutoMapper;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;
using VisitorManagementSystem.Api.Application.DTOs.Locations;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Application.DTOs.VisitPurposes;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Application.Mapping;

/// <summary>
/// AutoMapper profile for invitation domain
/// </summary>
public class InvitationMappingProfile : Profile
{
    public InvitationMappingProfile()
    {
        CreateMap<Invitation, InvitationDto>()
            .ForMember(dest => dest.VisitDurationHours, opt => opt.MapFrom(src => src.VisitDurationHours))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.IsApproved, opt => opt.MapFrom(src => src.IsApproved))
            .ForMember(dest => dest.CanBeModified, opt => opt.MapFrom(src => src.CanBeModified))
            .ForMember(dest => dest.CanBeCancelled, opt => opt.MapFrom(src => src.CanBeCancelled))
            .ForMember(dest => dest.IsExpired, opt => opt.MapFrom(src => src.IsExpired));

        CreateMap<InvitationApproval, InvitationApprovalDto>()
            .ForMember(dest => dest.ApproverName, opt => opt.MapFrom(src => src.Approver != null ? src.Approver.FullName : null))
            .ForMember(dest => dest.EscalatedToUserName, opt => opt.MapFrom(src => src.EscalatedToUser != null ? src.EscalatedToUser.FullName : null))
            .ForMember(dest => dest.IsPending, opt => opt.MapFrom(src => src.IsPending))
            .ForMember(dest => dest.IsCompleted, opt => opt.MapFrom(src => src.IsCompleted));

        CreateMap<InvitationEvent, InvitationEventDto>()
            .ForMember(dest => dest.TriggeredByUserName, opt => opt.MapFrom(src => src.TriggeredByUser != null ? src.TriggeredByUser.FullName : null));

        CreateMap<InvitationTemplate, InvitationTemplateDto>()
            .ForMember(dest => dest.DefaultVisitPurposeName, opt => opt.MapFrom(src => src.DefaultVisitPurpose != null ? src.DefaultVisitPurpose.Name : null))
            .ForMember(dest => dest.DefaultLocationName, opt => opt.MapFrom(src => src.DefaultLocation != null ? src.DefaultLocation.Name : null))
            .ForMember(dest => dest.CreatedByName, opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.FullName : null))
            .ForMember(dest => dest.ModifiedByName, opt => opt.MapFrom(src => src.ModifiedByUser != null ? src.ModifiedByUser.FullName : null));

        // Reverse mappings for create/update operations
        CreateMap<CreateInvitationDto, Invitation>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.InvitationNumber, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedOn, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedOn, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore());

        CreateMap<UpdateInvitationDto, Invitation>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.InvitationNumber, opt => opt.Ignore())
            .ForMember(dest => dest.VisitorId, opt => opt.Ignore())
            .ForMember(dest => dest.HostId, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.Type, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedOn, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedOn, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore());
    }
}
