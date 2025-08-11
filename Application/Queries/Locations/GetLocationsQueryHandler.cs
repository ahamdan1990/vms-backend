using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Locations;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Locations;

/// <summary>
/// Handler for get locations query
/// </summary>
public class GetLocationsQueryHandler : IRequestHandler<GetLocationsQuery, List<LocationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetLocationsQueryHandler> _logger;

    public GetLocationsQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetLocationsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<LocationDto>> Handle(GetLocationsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting locations with filters - Type: {Type}, RootOnly: {RootOnly}", 
                request.LocationType, request.RootOnly);

            List<Domain.Entities.Location> locations;

            if (request.RootOnly)
            {
                locations = await _unitOfWork.Locations.GetRootLocationsAsync(cancellationToken);
            }
            else if (!string.IsNullOrEmpty(request.LocationType))
            {
                locations = await _unitOfWork.Locations.GetByTypeAsync(request.LocationType, cancellationToken);
            }
            else
            {
                locations = await _unitOfWork.Locations.GetOrderedAsync(cancellationToken);
            }

            // Filter inactive if not requested
            if (!request.IncludeInactive)
            {
                locations = locations.Where(l => l.IsActive).ToList();
            }

            var locationDtos = _mapper.Map<List<LocationDto>>(locations);
            return locationDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting locations");
            throw;
        }
    }
}
