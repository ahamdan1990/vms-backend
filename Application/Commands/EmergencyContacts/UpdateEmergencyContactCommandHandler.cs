using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Domain.ValueObjects;

namespace VisitorManagementSystem.Api.Application.Commands.EmergencyContacts;

/// <summary>
/// Handler for update emergency contact command
/// </summary>
public class UpdateEmergencyContactCommandHandler : IRequestHandler<UpdateEmergencyContactCommand, EmergencyContactDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateEmergencyContactCommandHandler> _logger;

    public UpdateEmergencyContactCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UpdateEmergencyContactCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<EmergencyContactDto> Handle(UpdateEmergencyContactCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing update emergency contact command for ID: {ContactId}", request.Id);

            // Get existing emergency contact
            var emergencyContact = await _unitOfWork.EmergencyContacts.GetByIdAsync(request.Id, cancellationToken);
            if (emergencyContact == null)
            {
                throw new InvalidOperationException($"Emergency contact with ID '{request.Id}' not found.");
            }

            // Check if setting as primary and handle existing primary contacts
            if (request.IsPrimary && !emergencyContact.IsPrimary)
            {
                var existingContacts = await _unitOfWork.EmergencyContacts.GetByVisitorIdAsync(
                    emergencyContact.VisitorId, cancellationToken);
                foreach (var existing in existingContacts.Where(c => c.IsPrimary && c.Id != request.Id))
                {
                    existing.IsPrimary = false;
                    _unitOfWork.EmergencyContacts.Update(existing);
                }
            }

            // Validate phone number
            if (!PhoneNumber.IsValidPhoneNumber(request.PhoneNumber))
            {
                throw new InvalidOperationException($"Invalid phone number format: {request.PhoneNumber}");
            }

            // Update properties
            emergencyContact.FirstName = request.FirstName.Trim();
            emergencyContact.LastName = request.LastName.Trim();
            emergencyContact.Relationship = request.Relationship.Trim();
            emergencyContact.PhoneNumber = new PhoneNumber(request.PhoneNumber);
            emergencyContact.Priority = request.Priority;
            emergencyContact.IsPrimary = request.IsPrimary;
            emergencyContact.Notes = request.Notes?.Trim();

            // Update alternate phone if provided and valid
            if (!string.IsNullOrEmpty(request.AlternatePhoneNumber) && 
                PhoneNumber.IsValidPhoneNumber(request.AlternatePhoneNumber))
            {
                emergencyContact.AlternatePhoneNumber = new PhoneNumber(request.AlternatePhoneNumber);
            }
            else
            {
                emergencyContact.AlternatePhoneNumber = null;
            }

            // Update email if provided
            if (!string.IsNullOrEmpty(request.Email))
            {
                emergencyContact.Email = new Email(request.Email);
            }
            else
            {
                emergencyContact.Email = null;
            }

            // Update address if provided
            if (request.Address != null)
            {
                emergencyContact.Address = new Address(
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
                emergencyContact.Address = null;
            }

            // Set audit information
            emergencyContact.UpdateModifiedBy(request.ModifiedBy);

            // Update in repository
            _unitOfWork.EmergencyContacts.Update(emergencyContact);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Emergency contact updated successfully: {ContactId} by {ModifiedBy}",
                request.Id, request.ModifiedBy);

            // Map to DTO and return
            var emergencyContactDto = _mapper.Map<EmergencyContactDto>(emergencyContact);
            return emergencyContactDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating emergency contact: {ContactId}", request.Id);
            throw;
        }
    }
}
