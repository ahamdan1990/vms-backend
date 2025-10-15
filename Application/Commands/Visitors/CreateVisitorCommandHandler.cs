using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Application.Commands.Invitations;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Domain.ValueObjects;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Application.Services.Visitors;

namespace VisitorManagementSystem.Api.Application.Commands.Visitors;

/// <summary>
/// Handler for create visitor command
/// </summary>
public class CreateVisitorCommandHandler : IRequestHandler<CreateVisitorCommand, VisitorDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateVisitorCommandHandler> _logger;
    private readonly IVisitorNotesBridgeService _visitorNotesBridgeService;
    private readonly IMediator _mediator;

    public CreateVisitorCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreateVisitorCommandHandler> logger,
        IVisitorNotesBridgeService visitorNotesBridgeService,
        IMediator mediator)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _visitorNotesBridgeService = visitorNotesBridgeService;
        _mediator = mediator;
    }

    public async Task<VisitorDto> Handle(CreateVisitorCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing create visitor command for email: {Email}", request.Email);

            // Validate email uniqueness
            if (await _unitOfWork.Visitors.EmailExistsAsync(request.Email, cancellationToken: cancellationToken))
            {
                _logger.LogWarning("Attempt to create visitor with existing email: {Email}", request.Email);
                throw new InvalidOperationException($"A visitor with email '{request.Email}' already exists.");
            }

            // Validate government ID uniqueness if provided
            if (!string.IsNullOrEmpty(request.GovernmentId) &&
                await _unitOfWork.Visitors.GovernmentIdExistsAsync(request.GovernmentId, cancellationToken: cancellationToken))
            {
                _logger.LogWarning("Attempt to create visitor with existing government ID: {GovernmentId}", request.GovernmentId);
                throw new InvalidOperationException($"A visitor with government ID '{request.GovernmentId}' already exists.");
            }

            // Create visitor entity
            var visitor = new Visitor
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = new Email(request.Email.Trim()),
                Company = request.Company?.Trim(),
                JobTitle = request.JobTitle?.Trim(),
                DateOfBirth = request.DateOfBirth,
                GovernmentId = request.GovernmentId?.Trim(),
                GovernmentIdType = request.GovernmentIdType?.Trim(),
                Nationality = request.Nationality?.Trim(),
                Language = request.Language,
                
                // Visitor preferences
                PreferredLocationId = request.PreferredLocationId,
                DefaultVisitPurposeId = request.DefaultVisitPurposeId,
                TimeZone = request.TimeZone ?? "Asia/Beirut",
                
                DietaryRequirements = request.DietaryRequirements?.Trim(),
                AccessibilityRequirements = request.AccessibilityRequirements?.Trim(),
                SecurityClearance = request.SecurityClearance?.Trim(),
                IsVip = request.IsVip,
                Notes = request.Notes?.Trim(),
                ExternalId = request.ExternalId?.Trim(),
                IsActive = true
            };

            // Set enhanced phone number if provided
            if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                var fullPhoneNumber = !string.IsNullOrEmpty(request.PhoneCountryCode)
                    ? $"+{request.PhoneCountryCode}{request.PhoneNumber}"
                    : request.PhoneNumber;

                if (PhoneNumber.IsValidPhoneNumber(fullPhoneNumber))
                {
                    visitor.PhoneNumber = new PhoneNumber(fullPhoneNumber, request.PhoneCountryCode);
                }
            }

            // Set address if provided
            if (request.Address != null)
            {
                visitor.Address = new Address(
                    request.Address.Street1,
                    request.Address.City,
                    request.Address.State,
                    request.Address.PostalCode,
                    request.Address.Country,
                    request.Address.Street2,
                    request.Address.AddressType ?? "Home"
                );
            }

            // Update normalized email
            visitor.UpdateNormalizedEmail();

            // Set audit information
            visitor.SetCreatedBy(request.CreatedBy);

            // Add visitor to repository
            await _unitOfWork.Visitors.AddAsync(visitor, cancellationToken);

            // Create emergency contacts if provided
            foreach (var contactDto in request.EmergencyContacts)
            {
                // Validate required phone number for emergency contact
                if (string.IsNullOrEmpty(contactDto.PhoneNumber) ||
                    !PhoneNumber.IsValidPhoneNumber(contactDto.PhoneNumber))
                {
                    continue; // Skip invalid emergency contact
                }

                var contact = new EmergencyContact
                {
                    Visitor = visitor,
                    FirstName = contactDto.FirstName.Trim(),
                    LastName = contactDto.LastName.Trim(),
                    Relationship = contactDto.Relationship.Trim(),
                    PhoneNumber = new PhoneNumber(contactDto.PhoneNumber),
                    Priority = contactDto.Priority,
                    IsPrimary = contactDto.IsPrimary,
                    Notes = contactDto.Notes?.Trim()
                };

                if (!string.IsNullOrEmpty(contactDto.AlternatePhoneNumber) &&
                    PhoneNumber.IsValidPhoneNumber(contactDto.AlternatePhoneNumber))
                {
                    contact.AlternatePhoneNumber = new PhoneNumber(contactDto.AlternatePhoneNumber);
                }

                if (!string.IsNullOrEmpty(contactDto.Email))
                {
                    contact.Email = new Email(contactDto.Email);
                }

                if (contactDto.Address != null)
                {
                    contact.Address = new Address(
                        contactDto.Address.Street1,
                        contactDto.Address.City,
                        contactDto.Address.State,
                        contactDto.Address.PostalCode,
                        contactDto.Address.Country,
                        contactDto.Address.Street2,
                        contactDto.Address.AddressType ?? "Home"
                    );
                }

                contact.SetCreatedBy(request.CreatedBy);
                await _unitOfWork.EmergencyContacts.AddAsync(contact, cancellationToken);
            }

            // FIRST: Save visitor and emergency contacts to get the visitor ID
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Visitor created successfully: {VisitorId} ({Email}) by {CreatedBy}",
                visitor.Id, visitor.Email.Value, request.CreatedBy);

            // SECOND: Create visitor notes from special requirements (now visitor has an ID)
            await _visitorNotesBridgeService.CreateNotesFromRequirementsAsync(visitor, request.CreatedBy, cancellationToken);

            // THIRD: Create invitation if requested (now visitor has an ID)
            if (request.CreateInvitation)
            {
                _logger.LogDebug("CreateInvitation flag: {CreateInvitation}", request.CreateInvitation);
                _logger.LogDebug("Creating invitation for visitor: {Email} (ID: {VisitorId})", request.Email, visitor.Id);
                
                try
                {
                    var invitationCommand = new CreateInvitationCommand
                    {
                        VisitorId = visitor.Id,
                        HostId = request.CreatedBy,
                        VisitPurposeId = request.InvitationVisitPurposeId ?? request.DefaultVisitPurposeId,
                        LocationId = request.InvitationLocationId ?? request.PreferredLocationId,
                        Type = InvitationType.Single,
                        Subject = request.InvitationSubject!,
                        Message = request.InvitationMessage,
                        ScheduledStartTime = request.InvitationScheduledStartTime!.Value,
                        ScheduledEndTime = request.InvitationScheduledEndTime!.Value,
                        ExpectedVisitorCount = request.InvitationExpectedVisitorCount,
                        SpecialInstructions = request.InvitationSpecialInstructions,
                        RequiresApproval = request.InvitationRequiresApproval,
                        RequiresEscort = request.InvitationRequiresEscort,
                        RequiresBadge = request.InvitationRequiresBadge,
                        NeedsParking = request.InvitationNeedsParking,
                        ParkingInstructions = request.InvitationParkingInstructions,
                        TemplateId = null,
                        SubmitForApproval = request.InvitationSubmitForApproval,
                        CreatedBy = request.CreatedBy
                    };

                    var invitation = await _mediator.Send(invitationCommand, cancellationToken);
                    _logger.LogInformation("Invitation created successfully: {InvitationId} for visitor {VisitorId}", 
                        invitation.Id, visitor.Id);
                }
                catch (Exception invitationEx)
                {
                    // Log invitation creation error but don't fail the entire visitor creation
                    _logger.LogError(invitationEx, "Failed to create invitation for visitor {Email}. Visitor creation will continue.", request.Email);
                    // Note: We could optionally throw here if invitation creation is critical
                    // For now, we allow visitor creation to succeed even if invitation fails
                }
            }

            // FINAL: Save visitor notes and invitation (if created)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Map to DTO and return
            var visitorDto = _mapper.Map<VisitorDto>(visitor);
            return visitorDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating visitor with email: {Email}", request.Email);
            throw;
        }
    }
}
