using AutoMapper;
using MediatR;
using System.Security.Claims;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Application.Services.Visitors;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Domain.ValueObjects;
using DomainPermissions = VisitorManagementSystem.Api.Domain.Constants.Permissions;

namespace VisitorManagementSystem.Api.Application.Commands.Visitors;

/// <summary>
/// Handler for update visitor command
/// </summary>
public class UpdateVisitorCommandHandler : IRequestHandler<UpdateVisitorCommand, VisitorDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateVisitorCommandHandler> _logger;
    private readonly IVisitorNotesBridgeService _visitorNotesBridgeService;
    private readonly IMediator _mediator;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UpdateVisitorCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UpdateVisitorCommandHandler> logger,
        IVisitorNotesBridgeService visitorNotesBridgeService,
        IMediator mediator,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _visitorNotesBridgeService = visitorNotesBridgeService;
        _mediator = mediator;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<VisitorDto> Handle(UpdateVisitorCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing update visitor command for ID: {Id}", request.Id);

            // Get existing visitor
            var visitor = await _unitOfWork.Visitors.GetByIdAsync(request.Id, cancellationToken);
            if (visitor == null)
            {
                _logger.LogWarning("Visitor not found: {Id}", request.Id);
                throw new InvalidOperationException($"Visitor with ID '{request.Id}' not found.");
            }

            // Check if visitor is deleted
            if (visitor.IsDeleted)
            {
                _logger.LogWarning("Attempt to update deleted visitor: {Id}", request.Id);
                throw new InvalidOperationException($"Cannot update deleted visitor.");
            }

            // SECURITY: Check update permissions
            var userPermissions = GetCurrentUserPermissions();
            bool hasReadAll = userPermissions.Contains(DomainPermissions.Visitor.ReadAll);
            bool hasUpdate = userPermissions.Contains(DomainPermissions.Visitor.Update);

            // If user has Update permission but NOT ReadAll, verify they have access to this specific visitor
            // (ReadAll is used as a proxy for admin-level access since there's no UpdateAll permission)
            if (hasUpdate && !hasReadAll)
            {
                var hasAccess = await _unitOfWork.VisitorAccess.HasAccessAsync(
                    request.ModifiedBy,
                    visitor.Id,
                    cancellationToken);

                if (!hasAccess)
                {
                    _logger.LogWarning("User {UserId} attempted to update visitor {VisitorId} without access permission",
                        request.ModifiedBy, visitor.Id);
                    throw new UnauthorizedAccessException("You do not have permission to update this visitor.");
                }
            }

            // Validate email uniqueness (excluding current visitor)
            if (await _unitOfWork.Visitors.EmailExistsAsync(request.Email, request.Id, cancellationToken))
            {
                _logger.LogWarning("Attempt to update visitor with existing email: {Email}", request.Email);
                throw new InvalidOperationException($"A visitor with email '{request.Email}' already exists.");
            }

            // Validate government ID uniqueness if provided (excluding current visitor)
            if (!string.IsNullOrEmpty(request.GovernmentId) &&
                await _unitOfWork.Visitors.GovernmentIdExistsAsync(request.GovernmentId, request.Id, cancellationToken))
            {
                _logger.LogWarning("Attempt to update visitor with existing government ID: {GovernmentId}", request.GovernmentId);
                throw new InvalidOperationException($"A visitor with government ID '{request.GovernmentId}' already exists.");
            }

            // Store previous state for notes comparison (create a simple copy of relevant fields)
            var previousVisitor = new Domain.Entities.Visitor
            {
                Id = visitor.Id,
                DietaryRequirements = visitor.DietaryRequirements,
                AccessibilityRequirements = visitor.AccessibilityRequirements,
                SecurityClearance = visitor.SecurityClearance,
                Notes = visitor.Notes
            };

            // Update visitor properties
            visitor.FirstName = request.FirstName.Trim();
            visitor.LastName = request.LastName.Trim();
            visitor.Email = new Email(request.Email.Trim());
            visitor.Company = request.Company?.Trim();
            visitor.JobTitle = request.JobTitle?.Trim();
            visitor.DateOfBirth = request.DateOfBirth;
            visitor.GovernmentId = request.GovernmentId?.Trim();
            visitor.GovernmentIdType = request.GovernmentIdType?.Trim();
            visitor.Nationality = request.Nationality?.Trim();
            visitor.Language = request.Language;
            visitor.DietaryRequirements = request.DietaryRequirements?.Trim();
            visitor.AccessibilityRequirements = request.AccessibilityRequirements?.Trim();
            visitor.SecurityClearance = request.SecurityClearance?.Trim();
            visitor.Notes = request.Notes?.Trim();
            visitor.ExternalId = request.ExternalId?.Trim();

            // Update enhanced phone number
            if (!string.IsNullOrEmpty(request.PhoneNumber))
            {
                var fullPhoneNumber = !string.IsNullOrEmpty(request.PhoneCountryCode)
                    ? $"+{request.PhoneCountryCode}{request.PhoneNumber}"
                    : request.PhoneNumber;

                if (PhoneNumber.IsValidPhoneNumber(fullPhoneNumber))
                {
                    visitor.PhoneNumber = new PhoneNumber(fullPhoneNumber, request.PhoneCountryCode);
                }
                else
                {
                    visitor.PhoneNumber = null;
                }
            }
            else
            {
                visitor.PhoneNumber = null;
            }

            // Update address
            if (request.Address != null)
            {
                visitor.Address = new Address(
                    request.Address.Street1!,
                    request.Address.City!,
                    request.Address.State!,
                    request.Address.PostalCode!,
                    request.Address.Country!,
                    request.Address.Street2,
                    request.Address.AddressType ?? "Home"
                );
            }
            else
            {
                visitor.Address = null;
            }

            // Update normalized email
            visitor.UpdateNormalizedEmail();

            // Update audit information
            visitor.UpdateModifiedBy(request.ModifiedBy);

            // Update visitor notes based on changes to special requirements
            await _visitorNotesBridgeService.UpdateNotesFromRequirementsAsync(visitor, previousVisitor, request.ModifiedBy, cancellationToken);

            // Update in repository
            _unitOfWork.Visitors.Update(visitor);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Visitor updated successfully: {VisitorId} ({Email}) by {ModifiedBy}",
                visitor.Id, visitor.Email.Value, request.ModifiedBy);

            // Map to DTO and return
            var visitorDto = _mapper.Map<VisitorDto>(visitor);
            return visitorDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating visitor with ID: {Id}", request.Id);
            throw;
        }
    }

    private List<string> GetCurrentUserPermissions()
    {
        return _httpContextAccessor.HttpContext?.User
            .FindAll("permission")
            .Select(c => c.Value)
            .ToList() ?? new List<string>();
    }
}
