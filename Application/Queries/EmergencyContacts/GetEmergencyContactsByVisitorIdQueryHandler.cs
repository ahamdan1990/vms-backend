using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.EmergencyContacts;

/// <summary>
/// Handler for get emergency contacts by visitor ID query
/// </summary>
public class GetEmergencyContactsByVisitorIdQueryHandler : IRequestHandler<GetEmergencyContactsByVisitorIdQuery, List<EmergencyContactDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetEmergencyContactsByVisitorIdQueryHandler> _logger;

    public GetEmergencyContactsByVisitorIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetEmergencyContactsByVisitorIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<EmergencyContactDto>> Handle(GetEmergencyContactsByVisitorIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting emergency contacts for visitor: {VisitorId}", request.VisitorId);

            var emergencyContacts = await _unitOfWork.EmergencyContacts.GetByVisitorIdAsync(
                request.VisitorId, cancellationToken);

            // Filter out deleted contacts if not requested
            if (!request.IncludeDeleted)
            {
                emergencyContacts = emergencyContacts.Where(c => !c.IsDeleted).ToList();
            }

            // Order by priority then by primary status
            emergencyContacts = emergencyContacts
                .OrderBy(c => c.Priority)
                .ThenByDescending(c => c.IsPrimary)
                .ToList();

            var emergencyContactDtos = _mapper.Map<List<EmergencyContactDto>>(emergencyContacts);
            return emergencyContactDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting emergency contacts for visitor: {VisitorId}", request.VisitorId);
            throw;
        }
    }
}
