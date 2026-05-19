using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Invitations;
using VisitorManagementSystem.Api.Application.Services.QrCode;
using VisitorManagementSystem.Api.Application.Services.Capacity;
using VisitorManagementSystem.Api.Application.Services.Email;
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
    private readonly IEmailService _emailService;
    
    public CreateInvitationCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreateInvitationCommandHandler> logger,
        IQrCodeService qrCodeService,
        ICapacityService capacityService,
        INotificationService notificationService,
        IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _qrCodeService = qrCodeService;
        _capacityService = capacityService;
        _notificationService = notificationService;
        _emailService = emailService;
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

            var creator = request.CreatedBy == host.Id
                ? host
                : await _unitOfWork.Users.GetByIdAsync(request.CreatedBy, cancellationToken)
                  ?? throw new InvalidOperationException($"User with ID '{request.CreatedBy}' not found.");

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
            // Walk-in visitors are physically present at the reception desk —
            // approval is implicit and they must never be held in Submitted status.
            // For all other types:
            //   1. Per-host override (set by admin) takes highest precedence
            //   2. Global system setting (Invitations.RequireApprovalByDefault)
            //   3. Default: approval required
            bool effectiveRequiresApproval;
            if (request.Type == InvitationType.WalkIn)
            {
                effectiveRequiresApproval = false;
                _logger.LogDebug("Walk-in invitation for host {HostId}: approval bypassed (visitor is physically present)",
                    host.Id);
            }
            else
            {
                var globalApprovalSetting = await _unitOfWork.SystemConfigurations
                    .GetByCategoryAndKeyAsync("Invitations", "RequireApprovalByDefault", cancellationToken);
                var globalRequiresApproval = globalApprovalSetting == null ||
                    !bool.TryParse(globalApprovalSetting.Value, out var parsedGlobal) || parsedGlobal;

                effectiveRequiresApproval = host.RequiresApprovalOverride ?? globalRequiresApproval;

                _logger.LogDebug("Effective approval requirement for host {HostId}: {RequiresApproval} " +
                    "(override: {Override}, global: {Global})",
                    host.Id, effectiveRequiresApproval, host.RequiresApprovalOverride, globalRequiresApproval);
            }

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
                RequiresEscort = request.RequiresEscort,
                RequiresBadge = request.RequiresBadge,
                NeedsParking = request.NeedsParking,
                ParkingInstructions = request.ParkingInstructions?.Trim(),
                Status = effectiveRequiresApproval ? InvitationStatus.Submitted : InvitationStatus.Approved,
                IsCivilian = request.IsCivilian,
                AffiliatedOrganization = request.AffiliatedOrganization?.Trim()
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
                await SendInvitationNotificationsAsync(invitation, host, creator, visitor, effectiveRequiresApproval, cancellationToken);

                // Warn the creator if the host is currently busy with another active visitor
                await NotifyIfHostIsBusyAsync(invitation, host, creator, cancellationToken);

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
    /// <summary>
    /// Notifies the invitation creator when the assigned host currently has active (checked-in) visitors.
    /// </summary>
    private async Task NotifyIfHostIsBusyAsync(Invitation invitation, User host, User creator, CancellationToken cancellationToken)
    {
        try
        {
            var hostInvitations = await _unitOfWork.Invitations.GetByHostIdAsync(host.Id, cancellationToken);
            var activeHostVisits = hostInvitations
                .Where(i => i.Status == Domain.Enums.InvitationStatus.Active && i.Id != invitation.Id)
                .ToList();

            if (!activeHostVisits.Any())
                return;

            var activeVisitorNames = activeHostVisits
                .Select(i => i.Visitor?.FullName ?? "Unknown")
                .Distinct()
                .ToList();

            var visitorList = string.Join(", ", activeVisitorNames);
            var message = $"Note: {host.FullName} is currently with {activeHostVisits.Count} active visitor(s): {visitorList}. " +
                          "They may not be immediately available.";

            // Notify the creator (receptionist)
            if (creator.Id != host.Id)
            {
                await _notificationService.NotifyUserAsync(
                    creator.Id,
                    "Host Currently Busy",
                    message,
                    NotificationAlertType.SystemAlert,
                    AlertPriority.Medium,
                    new { HostId = host.Id, HostName = host.FullName, ActiveVisitCount = activeHostVisits.Count },
                    cancellationToken);
            }

            _logger.LogInformation("Host {HostId} has {Count} active visit(s) when new invitation {InvitationId} was created",
                host.Id, activeHostVisits.Count, invitation.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not check host busy status for host {HostId}", host.Id);
            // Non-critical — don't fail the invitation creation
        }
    }

    private async Task SendInvitationNotificationsAsync(Invitation invitation, User host, User creator, Visitor visitor,
        bool requiresApproval, CancellationToken cancellationToken)
    {
        try
        {
            var isWalkIn = invitation.Type == InvitationType.WalkIn;
            var entityLabel = isWalkIn ? "walk-in" : "invitation";
            var statusNote = requiresApproval
                ? " It requires admin approval."
                : " It has been auto-approved.";

            var notificationData = new
            {
                InvitationId = invitation.Id,
                InvitationNumber = invitation.InvitationNumber,
                VisitorId = visitor.Id,
                VisitorName = visitor.FullName,
                Company = visitor.Company,
                HostId = host.Id,
                HostName = host.FullName,
                CreatedByUserId = creator.Id,
                CreatedByName = creator.FullName,
                CreatedByRole = creator.Role.ToString(),
                InvitationType = invitation.Type.ToString(),
                RequiresApproval = requiresApproval,
                ScheduledStartTime = invitation.ScheduledStartTime
            };

            if (creator.Id == host.Id)
            {
                await _notificationService.NotifyUserAsync(
                    host.Id,
                    isWalkIn ? "Walk-In Created" : "Invitation Created",
                    $"Your {entityLabel} for {visitor.FullName} has been created successfully.{statusNote} " +
                    $"Scheduled: {invitation.ScheduledStartTime:MMM dd, yyyy HH:mm}",
                    NotificationAlertType.InvitationCreated,
                    AlertPriority.Low,
                    notificationData,
                    cancellationToken);

                var receptionistRecipients = await GetActiveUsersByRoleAsync(UserRole.Receptionist, cancellationToken);
                await NotifyUsersAsync(
                    receptionistRecipients,
                    isWalkIn ? "New Walk-In Created" : "New Invitation Created",
                    $"{host.FullName} created a {entityLabel} for {visitor.FullName}.{statusNote} " +
                    $"Scheduled: {invitation.ScheduledStartTime:MMM dd, yyyy HH:mm}",
                    NotificationAlertType.InvitationCreated,
                    AlertPriority.Medium,
                    notificationData,
                    cancellationToken,
                    excludedUserIds: new[] { host.Id });

                var adminRecipients = await GetActiveUsersByRoleAsync(UserRole.Administrator, cancellationToken);
                await NotifyUsersAsync(
                    adminRecipients,
                    requiresApproval ? "New Invitation Pending Approval" : (isWalkIn ? "New Walk-In Created" : "New Invitation Created"),
                    $"{host.FullName} created a {entityLabel} for {visitor.FullName}.{statusNote} " +
                    $"Scheduled: {invitation.ScheduledStartTime:MMM dd, yyyy HH:mm}",
                    requiresApproval ? NotificationAlertType.InvitationPendingApproval : NotificationAlertType.InvitationCreated,
                    AlertPriority.Medium,
                    notificationData,
                    cancellationToken,
                    excludedUserIds: new[] { host.Id });

                _logger.LogInformation("Host-created invitation notifications sent for invitation {InvitationId}", invitation.Id);
                return;
            }

            if (creator.Role == UserRole.Receptionist)
            {
                await _notificationService.NotifyUserAsync(
                    host.Id,
                    isWalkIn ? "New Walk-In Created For You" : "New Invitation Created For You",
                    $"{creator.FullName} created a {entityLabel} for your visitor {visitor.FullName}.{statusNote} " +
                    $"Scheduled: {invitation.ScheduledStartTime:MMM dd, yyyy HH:mm}",
                    NotificationAlertType.InvitationCreated,
                    AlertPriority.Medium,
                    notificationData,
                    cancellationToken);

                var adminRecipients = await GetActiveUsersByRoleAsync(UserRole.Administrator, cancellationToken);
                await NotifyUsersAsync(
                    adminRecipients,
                    requiresApproval ? "New Invitation Pending Approval" : (isWalkIn ? "Receptionist Registered Walk-In" : "Receptionist Created Invitation"),
                    $"{creator.FullName} created a {entityLabel} for {visitor.FullName} and assigned it to {host.FullName}.{statusNote} " +
                    $"Scheduled: {invitation.ScheduledStartTime:MMM dd, yyyy HH:mm}",
                    requiresApproval ? NotificationAlertType.InvitationPendingApproval : NotificationAlertType.InvitationCreated,
                    AlertPriority.Medium,
                    notificationData,
                    cancellationToken,
                    excludedUserIds: new[] { host.Id });

                await SendNotificationEmailsAsync(
                    new[] { host },
                    isWalkIn ? "New walk-in created for you" : "New invitation created for you",
                    $"{creator.FullName} created a {entityLabel} for your visitor {visitor.FullName}.{statusNote}\n" +
                    $"Invitation Number: {invitation.InvitationNumber}\n" +
                    $"Scheduled: {invitation.ScheduledStartTime:MMM dd, yyyy HH:mm}",
                    cancellationToken);

                await SendNotificationEmailsAsync(
                    adminRecipients.Where(admin => admin.Id != host.Id),
                    requiresApproval ? "New invitation pending approval" : (isWalkIn ? "Receptionist registered walk-in" : "Receptionist created invitation"),
                    $"{creator.FullName} created a {entityLabel} for {visitor.FullName} and assigned it to {host.FullName}.{statusNote}\n" +
                    $"Invitation Number: {invitation.InvitationNumber}\n" +
                    $"Scheduled: {invitation.ScheduledStartTime:MMM dd, yyyy HH:mm}",
                    cancellationToken);

                _logger.LogInformation("Receptionist-created invitation notifications sent for invitation {InvitationId}", invitation.Id);
                return;
            }

            if (requiresApproval)
            {
                var adminRecipients = await GetActiveUsersByRoleAsync(UserRole.Administrator, cancellationToken);
                await NotifyUsersAsync(
                    adminRecipients,
                    "New Invitation Pending Approval",
                    $"Invitation for {visitor.FullName} from {visitor.Company} requires approval. Host: {host.FullName}",
                    NotificationAlertType.InvitationPendingApproval,
                    AlertPriority.Medium,
                    notificationData,
                    cancellationToken,
                    excludedUserIds: new[] { host.Id });
            }

            await _notificationService.NotifyUserAsync(
                invitation.HostId,
                "Invitation Created",
                $"Your invitation for {visitor.FullName} has been created successfully.{statusNote} " +
                $"Scheduled: {invitation.ScheduledStartTime:MMM dd, yyyy HH:mm}",
                NotificationAlertType.InvitationCreated,
                AlertPriority.Low,
                notificationData,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending invitation notifications for {InvitationId}", invitation.Id);
            // Don't throw - notification failure shouldn't break invitation creation
        }
    }

    private async Task<List<User>> GetActiveUsersByRoleAsync(UserRole role, CancellationToken cancellationToken)
    {
        var users = await _unitOfWork.Users.GetByRoleAsync(role, cancellationToken);
        return users.Where(u => u.IsActive).ToList();
    }

    private async Task NotifyUsersAsync(
        IEnumerable<User> recipients,
        string title,
        string message,
        NotificationAlertType type,
        AlertPriority priority,
        object data,
        CancellationToken cancellationToken,
        IEnumerable<int>? excludedUserIds = null)
    {
        var excludedRecipientIds = excludedUserIds?.ToHashSet() ?? new HashSet<int>();

        foreach (var recipient in recipients
                     .Where(u => u.Id > 0 && !excludedRecipientIds.Contains(u.Id))
                     .GroupBy(u => u.Id)
                     .Select(group => group.First()))
        {
            await _notificationService.NotifyUserAsync(
                recipient.Id,
                title,
                message,
                type,
                priority,
                data,
                cancellationToken);
        }
    }

    private async Task SendNotificationEmailsAsync(
        IEnumerable<User> recipients,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        foreach (var recipient in recipients
                     .Where(u => u.IsActive && !string.IsNullOrWhiteSpace(u.Email))
                     .GroupBy(u => u.Id)
                     .Select(group => group.First()))
        {
            try
            {
                await _emailService.SendAlertEmailAsync(recipient.Email!, subject, body, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification email to user {UserId}", recipient.Id);
            }
        }
    }
}
