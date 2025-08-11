using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Domain.ValueObjects;

namespace VisitorManagementSystem.Api.Application.Commands.Visitors;

/// <summary>
/// Handler for update visitor command
/// </summary>
public class UpdateVisitorCommandHandler : IRequestHandler<UpdateVisitorCommand, VisitorDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateVisitorCommandHandler> _logger;

    public UpdateVisitorCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UpdateVisitorCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
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

            // Update phone number with validation
            if (!string.IsNullOrEmpty(request.PhoneNumber) &&
                PhoneNumber.IsValidPhoneNumber(request.PhoneNumber))
            {
                visitor.PhoneNumber = new PhoneNumber(request.PhoneNumber);
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
}
