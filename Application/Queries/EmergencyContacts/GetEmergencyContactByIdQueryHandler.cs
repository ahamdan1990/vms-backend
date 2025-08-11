using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.EmergencyContacts;

/// <summary>
/// Handler for get emergency contact by ID query
/// </summary>
public class GetEmergencyContactByIdQueryHandler : IRequestHandler<GetEmergencyContactByIdQuery, EmergencyContactDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetEmergencyContactByIdQueryHandler> _logger;

    public GetEmergencyContactByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetEmergencyContactByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<EmergencyContactDto?> Handle(GetEmergencyContactByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting emergency contact by ID: {ContactId}", request.Id);

            var emergencyContact = await _unitOfWork.EmergencyContacts.GetByIdAsync(request.Id, cancellationToken);
            
            // Return null if not found or deleted (when not including deleted)
            if (emergencyContact == null || (!request.IncludeDeleted && emergencyContact.IsDeleted))
            {
                return null;
            }

            var emergencyContactDto = _mapper.Map<EmergencyContactDto>(emergencyContact);
            return emergencyContactDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting emergency contact by ID: {ContactId}", request.Id);
            throw;
        }
    }
}
