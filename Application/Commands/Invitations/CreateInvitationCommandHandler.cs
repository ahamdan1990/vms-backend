using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;
using VisitorManagementSystem.Api.Application.Services.QrCode;
using VisitorManagementSystem.Api.Application.Services.Capacity;
using VisitorManagementSystem.Api.Application.Services.Notifications;
using VisitorManagementSystem.Api.Application.DTOs.Capacity;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Enums;
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
    private readonly IQrCodeService _qrCodeService;
    private readonly ICapacityService _capacityService;
    private readonly INotificationService _notificationService;
    
    public CreateInvitationCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreateInvitationCommandHandler> logger,
        IQrCodeService qrCodeService,
        ICapacityService capacityService,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _qrCodeService = qrCodeService;
        _capacityService = capacityService;
        _notificationService = notificationService;
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
                throw new InvalidOperationException($"Visitor with ID '{request.VisitorId}' not found.");

            // Validate host exists
            var host = await _unitOfWork.Users.GetByIdAsync(request.HostId, cancellationToken);
            if (host == null)
                throw new InvalidOperationException($"Host with ID '{request.HostId}' not found.");

            // Validate visit purpose if specified
            if (request.VisitPurposeId.HasValue)
            {
                var visitPurpose = await _unitOfWork.VisitPurposes.GetByIdAsync(request.VisitPurposeId.Value, cancellationToken);
                if (visitPurpose == null)
                    throw new InvalidOperationException($"Visit purpose with ID '{request.VisitPurposeId}' not found.");
            }

            // Validate location if specified
            if (request.LocationId.HasValue)
            {
                var location = await _unitOfWork.Locations.GetByIdAsync(request.LocationId.Value, cancellationToken);
                if (location == null)
                    throw new InvalidOperationException($"Location with ID '{request.LocationId}' not found.");
            }

            // Validate time slot if specified
            TimeSlot? timeSlot = null;
            if (request.TimeSlotId.HasValue)
            {
                timeSlot = await _unitOfWork.Repository<TimeSlot>().GetByIdAsync(request.TimeSlotId.Value, cancellationToken);
                if (timeSlot == null)
                    throw new InvalidOperationException($"Time slot with ID '{request.TimeSlotId}' not found.");

                if (!timeSlot.IsActive)
                    throw new InvalidOperationException("Cannot book an inactive time slot.");
            }

            // Validate capacity before creating invitation
            var capacityRequest = new CapacityValidationRequestDto
            {
                LocationId = request.LocationId,
                DateTime = request.ScheduledStartTime,
                ExpectedVisitors = request.ExpectedVisitorCount,
                IsVipRequest = false // TODO: Determine VIP status from user permissions or visitor VIP flag
            };

            var capacityValidation = await _capacityService.ValidateCapacityAsync(capacityRequest, cancellationToken);
            
            if (!capacityValidation.IsAvailable)
            {
                var errorMessage = $"Capacity validation failed: {string.Join(", ", capacityValidation.Messages)}";
                if (capacityValidation.AlternativeSlots.Any())
                {
                    var alternatives = string.Join(", ", capacityValidation.AlternativeSlots
                        .Take(3).Select(a => $"{a.DateTime:MM/dd HH:mm}"));
                    errorMessage += $" Alternative times available: {alternatives}";
                }
                throw new InvalidOperationException(errorMessage);
            }

            // Log capacity validation result
            _logger.LogInformation("Capacity validation passed: {ExpectedVisitors} visitors at {DateTime}, " +
                "Occupancy: {OccupancyPercentage}%, Available slots: {AvailableSlots}",
                request.ExpectedVisitorCount, request.ScheduledStartTime, 
                capacityValidation.OccupancyPercentage, capacityValidation.AvailableSlots);

            // Apply template if specified
            InvitationTemplate? template = null;
            if (request.TemplateId.HasValue)
            {
                template = await _unitOfWork.Repository<InvitationTemplate>().GetByIdAsync(request.TemplateId.Value, cancellationToken);
                if (template == null)
                    throw new InvalidOperationException($"Template with ID '{request.TemplateId}' not found.");
            }

            // Determine effective approval requirement:
            // 1. Per-host override (set by admin) takes highest precedence
            // 2. Global system setting (Invitations.RequireApprovalByDefault)
            // 3. Default: approval required
            var globalApprovalSetting = await _unitOfWork.SystemConfigurations
                .GetByCategoryAndKeyAsync("Invitations", "RequireApprovalByDefault", cancellationToken);
            var globalRequiresApproval = globalApprovalSetting == null ||
                !bool.TryParse(globalApprovalSetting.Value, out var parsedGlobal) || parsedGlobal;

            var effectiveRequiresApproval = host.RequiresApprovalOverride ?? globalRequiresApproval;

            _logger.LogDebug("Effective approval requirement for host {HostId}: {RequiresApproval} " +
                "(override: {Override}, global: {Global})",
                host.Id, effectiveRequiresApproval, host.RequiresApprovalOverride, globalRequiresApproval);

            // Ensure mandatory fields with fallback or error
            var subject = request.Subject?.Trim() ?? throw new InvalidOperationException("Subject is required.");

            // For walk-in invitations without a visit purpose, use the default GUEST purpose
            int? visitPurposeId = request.VisitPurposeId ?? template?.DefaultVisitPurposeId;
            if (!visitPurposeId.HasValue && request.Type == InvitationType.WalkIn)
            {
                var guestPurpose = await _unitOfWork.VisitPurposes.GetByCodeAsync("GUEST", cancellationToken);
                if (guestPurpose != null)
                {
                    visitPurposeId = guestPurpose.Id;
                    _logger.LogDebug("Using default GUEST purpose (ID: {PurposeId}) for walk-in invitation", visitPurposeId);
                }
            }

            if (!visitPurposeId.HasValue)
                throw new InvalidOperationException("VisitPurposeId is required.");

            var locationId = request.LocationId ?? template?.DefaultLocationId
                             ?? throw new InvalidOperationException("LocationId is required.");

            var specialInstructions = request.SpecialInstructions?.Trim()
                                      ?? template?.DefaultSpecialInstructions;

            // Generate unique invitation number
            var invitationNumber = await GenerateUniqueInvitationNumberAsync(cancellationToken);

            // Create invitation entity
            // Status: Submitted if approval is required, Approved if auto-approved
            var invitation = new Invitation
            {
                InvitationNumber = invitationNumber,
                VisitorId = request.VisitorId,
                HostId = request.HostId,
                VisitPurposeId = visitPurposeId,
                LocationId = locationId,
                TimeSlotId = request.TimeSlotId,
                Type = request.Type,
                Subject = subject,
                Message = request.Message?.Trim(),
                ScheduledStartTime = request.ScheduledStartTime,
                ScheduledEndTime = request.ScheduledEndTime,
                ExpectedVisitorCount = request.ExpectedVisitorCount,
                SpecialInstructions = specialInstructions,
                RequiresApproval = effectiveRequiresApproval,
                RequiresEscort = request.RequiresEscort || (template?.DefaultRequiresEscort ?? false),
                RequiresBadge = request.RequiresBadge || (template?.DefaultRequiresBadge ?? true),
                NeedsParking = request.NeedsParking,
                ParkingInstructions = request.ParkingInstructions?.Trim(),
                Status = effectiveRequiresApproval ? InvitationStatus.Submitted : InvitationStatus.Approved
            };

            // Generate QR code only for auto-approved invitations.
            // When approval is required, QR is generated by ApproveInvitationCommandHandler.
            if (!effectiveRequiresApproval)
            {
                if (_qrCodeService == null)
                    throw new InvalidOperationException("QR code service is not initialized.");

                var qrCodeData = await _qrCodeService.GenerateInvitationQrDataAsync(invitation, cancellationToken);
                invitation.UpdateQrCode(qrCodeData ?? throw new InvalidOperationException("QR code generation failed."));
            }

            // Validate invitation data
            var validationErrors = invitation.ValidateInvitation();
            if (validationErrors.Any())
                throw new InvalidOperationException($"Invitation validation failed: {string.Join(", ", validationErrors)}");

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
                    $"Invitation created by {host.FullName ?? "Unknown"}",
                    request.CreatedBy
                );
                await _unitOfWork.Repository<InvitationEvent>().AddAsync(invitationEvent, cancellationToken);

                // If approval is required, create a Submitted event
                if (effectiveRequiresApproval)
                {
                    var submitEvent = InvitationEvent.Create(
                        invitation.Id,
                        InvitationEventTypes.Submitted,
                        "Invitation submitted for admin approval",
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

                // Create time slot booking if time slot is specified
                if (request.TimeSlotId.HasValue && timeSlot != null)
                {
                    var booking = new TimeSlotBooking
                    {
                        TimeSlotId = request.TimeSlotId.Value,
                        BookingDate = request.ScheduledStartTime.Date,
                        InvitationId = invitation.Id,
                        VisitorCount = request.ExpectedVisitorCount,
                        Status = BookingStatus.Confirmed,
                        Notes = $"Auto-booked for invitation {invitation.InvitationNumber}",
                        BookedBy = request.CreatedBy,
                        BookedOn = DateTime.UtcNow
                    };

                    booking.SetCreatedBy(request.CreatedBy);

                    // Validate booking
                    var bookingErrors = booking.ValidateBooking();
                    if (bookingErrors.Any())
                    {
                        _logger.LogWarning("Time slot booking validation warnings for invitation {InvitationId}: {Errors}",
                            invitation.Id, string.Join(", ", bookingErrors));
                    }

                    await _unitOfWork.Repository<TimeSlotBooking>().AddAsync(booking, cancellationToken);

                    _logger.LogInformation("Time slot booking created for invitation {InvitationId} in slot {TimeSlotId}",
                        invitation.Id, request.TimeSlotId.Value);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // Send notifications after successful creation
                await SendInvitationNotificationsAsync(invitation, host, visitor, effectiveRequiresApproval, cancellationToken);

                _logger.LogInformation("Invitation created successfully: {InvitationId} ({InvitationNumber}) for visitor {VisitorId} by {CreatedBy}",
                    invitation.Id, invitation.InvitationNumber, request.VisitorId, request.CreatedBy);

                // Load complete invitation with navigation properties
                var completeInvitation = await _unitOfWork.Invitations.GetByIdAsync(invitation.Id, cancellationToken)
                                         ?? throw new InvalidOperationException("Failed to retrieve created invitation.");

                var invitationDto = _mapper.Map<InvitationDto>(completeInvitation)
                                     ?? throw new InvalidOperationException("Mapping to InvitationDto failed.");

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

    /// <summary>
    /// Send notifications for invitation creation
    /// </summary>
    private async Task SendInvitationNotificationsAsync(Invitation invitation, User host, Visitor visitor,
        bool requiresApproval, CancellationToken cancellationToken)
    {
        try
        {
            if (requiresApproval)
            {
                // Notify administrators about pending approval
                await _notificationService.NotifyRoleAsync(
                    UserRoles.Administrator,
                    "New Invitation Pending Approval",
                    $"Invitation for {visitor.FullName} from {visitor.Company} requires approval. Host: {host.FullName}",
                    NotificationAlertType.InvitationPendingApproval,
                    AlertPriority.Medium,
                    new { InvitationId = invitation.Id, InvitationNumber = invitation.InvitationNumber },
                    cancellationToken);

                _logger.LogInformation("Approval notification sent for invitation {InvitationId}", invitation.Id);
            }

            // Send confirmation to host
            var statusNote = requiresApproval ? " It is pending admin approval." : " It has been automatically approved.";
            await _notificationService.NotifyUserAsync(
                invitation.HostId,
                "Invitation Created",
                $"Your invitation for {visitor.FullName} has been created successfully.{statusNote} " +
                $"Scheduled: {invitation.ScheduledStartTime:MMM dd, yyyy HH:mm}",
                NotificationAlertType.InvitationCreated,
                AlertPriority.Low,
                new { InvitationId = invitation.Id, InvitationNumber = invitation.InvitationNumber },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending invitation notifications for {InvitationId}", invitation.Id);
            // Don't throw - notification failure shouldn't break invitation creation
        }
    }
}
