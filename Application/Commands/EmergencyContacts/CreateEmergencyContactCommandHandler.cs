using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Domain.ValueObjects;

namespace VisitorManagementSystem.Api.Application.Commands.EmergencyContacts;

/// <summary>
/// Handler for create emergency contact command
/// </summary>
public class CreateEmergencyContactCommandHandler : IRequestHandler<CreateEmergencyContactCommand, EmergencyContactDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateEmergencyContactCommandHandler> _logger;

    public CreateEmergencyContactCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreateEmergencyContactCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<EmergencyContactDto> Handle(CreateEmergencyContactCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing create emergency contact command for visitor: {VisitorId}", request.VisitorId);

            // Verify visitor exists
            var visitor = await _unitOfWork.Visitors.GetByIdAsync(request.VisitorId, cancellationToken);
            if (visitor == null)
            {
                throw new InvalidOperationException($"Visitor with ID '{request.VisitorId}' not found.");
            }

            // Check if setting as primary and handle existing primary contacts
            if (request.IsPrimary)
            {
                var existingContacts = await _unitOfWork.EmergencyContacts.GetByVisitorIdAsync(request.VisitorId, cancellationToken);
                foreach (var existing in existingContacts.Where(c => c.IsPrimary))
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

            // Create emergency contact entity
            var emergencyContact = new EmergencyContact
            {
                VisitorId = request.VisitorId,
                FirstName = request.FirstName.Trim(),                LastName = request.LastName.Trim(),
                Relationship = request.Relationship.Trim(),
                PhoneNumber = new PhoneNumber(request.PhoneNumber),
                Priority = request.Priority,
                IsPrimary = request.IsPrimary,
                Notes = request.Notes?.Trim()
            };

            // Set alternate phone if provided and valid
            if (!string.IsNullOrEmpty(request.AlternatePhoneNumber) && 
                PhoneNumber.IsValidPhoneNumber(request.AlternatePhoneNumber))
            {
                emergencyContact.AlternatePhoneNumber = new PhoneNumber(request.AlternatePhoneNumber);
            }

            // Set email if provided
            if (!string.IsNullOrEmpty(request.Email))
            {
                emergencyContact.Email = new Email(request.Email);
            }

            // Set address if provided
            if (request.Address != null)
            {
                emergencyContact.Address = new Address(
                    request.Address.Street1,
                    request.Address.City,
                    request.Address.State,
                    request.Address.PostalCode,
                    request.Address.Country,
                    request.Address.Street2,
                    request.Address.AddressType ?? "Home"
                );
            }

            // Set audit information
            emergencyContact.SetCreatedBy(request.CreatedBy);

            // Add to repository
            await _unitOfWork.EmergencyContacts.AddAsync(emergencyContact, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Emergency contact created successfully: {ContactId} for visitor {VisitorId} by {CreatedBy}",
                emergencyContact.Id, request.VisitorId, request.CreatedBy);

            // Map to DTO and return
            var emergencyContactDto = _mapper.Map<EmergencyContactDto>(emergencyContact);
            return emergencyContactDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating emergency contact for visitor: {VisitorId}", request.VisitorId);
            throw;
        }
    }
}
