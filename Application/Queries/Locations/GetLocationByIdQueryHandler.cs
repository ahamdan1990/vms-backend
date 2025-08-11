using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Locations;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Locations;

/// <summary>
/// Handler for getting location by ID query
/// </summary>
public class GetLocationByIdQueryHandler : IRequestHandler<GetLocationByIdQuery, LocationDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetLocationByIdQueryHandler> _logger;

    public GetLocationByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetLocationByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<LocationDto?> Handle(GetLocationByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting location by ID: {LocationId}", request.Id);

            var location = await _unitOfWork.Repository<Domain.Entities.Location>()
                .GetByIdAsync(request.Id, cancellationToken);
            
            if (location == null)
            {
                _logger.LogWarning("Location not found with ID: {LocationId}", request.Id);
                return null;
            }

            var dto = _mapper.Map<LocationDto>(location);
            
            _logger.LogDebug("Successfully retrieved location {LocationId}", request.Id);

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location by ID: {LocationId}", request.Id);
            throw;
        }
    }
}
