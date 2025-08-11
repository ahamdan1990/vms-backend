using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Invitations;

/// <summary>
/// Handler for create invitation command
/// </summary>
public class CreateInvitationCommandHandler : IRequestHandler<CreateInvitationCommand, InvitationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateInvitationCommandHandler> _logger;

    public CreateInvitationCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreateInvitationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<InvitationDto> Handle(CreateInvitationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing create invitation command for visitor: {VisitorId} by host: {HostId}", 
                request.VisitorId, request.HostId);

            // Validate visitor exists
            var visitor = await _unitOfWork.Visitors.GetByIdAsync(request.VisitorId, cancellationToken);
            if (visitor == null)
            {
                throw new InvalidOperationException($"Visitor with ID '{request.VisitorId}' not found.");
            }

            // Validate host exists
            var host = await _unitOfWork.Users.GetByIdAsync(request.HostId, cancellationToken);
            if (host == null)
            {
                throw new InvalidOperationException($"Host with ID '{request.HostId}' not found.");
            }

            // Validate visit purpose if specified
            if (request.VisitPurposeId.HasValue)
            {
                var visitPurpose = await _unitOfWork.VisitPurposes.GetByIdAsync(request.VisitPurposeId.Value, cancellationToken);
                if (visitPurpose == null)
                {
                    throw new InvalidOperationException($"Visit purpose with ID '{request.VisitPurposeId}' not found.");
                }
            }

            // Validate location if specified
            if (request.LocationId.HasValue)
            {
                var location = await _unitOfWork.Locations.GetByIdAsync(request.LocationId.Value, cancellationToken);
                if (location == null)
                {
                    throw new InvalidOperationException($"Location with ID '{request.LocationId}' not found.");
                }
            }

            // Apply template if specified
            InvitationTemplate? template = null;
            if (request.TemplateId.HasValue)
            {
                template = await _unitOfWork.Repository<InvitationTemplate>().GetByIdAsync(request.TemplateId.Value, cancellationToken);
                if (template == null)
                {
                    throw new InvalidOperationException($"Template with ID '{request.TemplateId}' not found.");
                }
            }

            // Generate unique invitation number
            var invitationNumber = await GenerateUniqueInvitationNumberAsync(cancellationToken);

            // Create invitation entity
            var invitation = new Invitation
            {
                InvitationNumber = invitationNumber,
                VisitorId = request.VisitorId,
                HostId = request.HostId,
                VisitPurposeId = request.VisitPurposeId ?? template?.DefaultVisitPurposeId,
                LocationId = request.LocationId ?? template?.DefaultLocationId,
                Type = request.Type,
                Subject = request.Subject.Trim(),
                Message = request.Message?.Trim(),
                ScheduledStartTime = request.ScheduledStartTime,
                ScheduledEndTime = request.ScheduledEndTime,
                ExpectedVisitorCount = request.ExpectedVisitorCount,
                SpecialInstructions = request.SpecialInstructions?.Trim() ?? template?.DefaultSpecialInstructions,
                RequiresApproval = request.RequiresApproval || (template?.DefaultRequiresApproval ?? true),
                RequiresEscort = request.RequiresEscort || (template?.DefaultRequiresEscort ?? false),
                RequiresBadge = request.RequiresBadge || (template?.DefaultRequiresBadge ?? true),
                NeedsParking = request.NeedsParking,
                ParkingInstructions = request.ParkingInstructions?.Trim()
            };

            // Validate invitation data
            var validationErrors = invitation.ValidateInvitation();
            if (validationErrors.Any())
            {
                throw new InvalidOperationException($"Invitation validation failed: {string.Join(", ", validationErrors)}");
            }

            // Set audit information
            invitation.SetCreatedBy(request.CreatedBy);

            using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Add invitation to repository
                await _unitOfWork.Invitations.AddAsync(invitation, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Create invitation event
                var invitationEvent = InvitationEvent.Create(
                    invitation.Id,
                    InvitationEventTypes.Created,
                    $"Invitation created by {host.FullName}",
                    request.CreatedBy
                );
                await _unitOfWork.Repository<InvitationEvent>().AddAsync(invitationEvent, cancellationToken);

                // Submit for approval if requested
                if (request.SubmitForApproval)
                {
                    invitation.Submit(request.CreatedBy);
                    
                    var submitEvent = InvitationEvent.Create(
                        invitation.Id,
                        InvitationEventTypes.Submitted,
                        "Invitation submitted for approval",
                        request.CreatedBy
                    );
                    await _unitOfWork.Repository<InvitationEvent>().AddAsync(submitEvent, cancellationToken);
                }

                // Increment template usage if used
                if (template != null)
                {
                    template.IncrementUsage();
                    _unitOfWork.Repository<InvitationTemplate>().Update(template);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Invitation created successfully: {InvitationId} ({InvitationNumber}) for visitor {VisitorId} by {CreatedBy}",
                    invitation.Id, invitation.InvitationNumber, request.VisitorId, request.CreatedBy);

                // Load complete invitation with navigation properties
                var completeInvitation = await _unitOfWork.Invitations.GetByIdAsync(invitation.Id, cancellationToken);
                var invitationDto = _mapper.Map<InvitationDto>(completeInvitation);
                return invitationDto;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invitation for visitor: {VisitorId}", request.VisitorId);
            throw;
        }
    }

    private async Task<string> GenerateUniqueInvitationNumberAsync(CancellationToken cancellationToken)
    {
        string invitationNumber;
        int attempts = 0;
        const int maxAttempts = 10;

        do
        {
            invitationNumber = Invitation.GenerateInvitationNumber();
            attempts++;

            if (attempts > maxAttempts)
            {
                throw new InvalidOperationException("Unable to generate unique invitation number after maximum attempts.");
            }
        }
        while (await _unitOfWork.Invitations.InvitationNumberExistsAsync(invitationNumber, cancellationToken: cancellationToken));

        return invitationNumber;
    }
}
