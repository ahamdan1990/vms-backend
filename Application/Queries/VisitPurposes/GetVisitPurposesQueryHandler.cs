using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.VisitPurposes;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.VisitPurposes;

/// <summary>
/// Handler for get visit purposes query
/// </summary>
public class GetVisitPurposesQueryHandler : IRequestHandler<GetVisitPurposesQuery, List<VisitPurposeDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetVisitPurposesQueryHandler> _logger;

    public GetVisitPurposesQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetVisitPurposesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<VisitPurposeDto>> Handle(GetVisitPurposesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting visit purposes");

            var purposes = await _unitOfWork.VisitPurposes.GetOrderedAsync(cancellationToken);

            // Apply filters
            if (!request.IncludeInactive)
            {
                purposes = purposes.Where(p => p.IsActive).ToList();
            }

            if (request.RequiresApproval.HasValue)
            {
                purposes = purposes.Where(p => p.RequiresApproval == request.RequiresApproval.Value).ToList();
            }

            var purposeDtos = _mapper.Map<List<VisitPurposeDto>>(purposes);
            return purposeDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting visit purposes");
            throw;
        }
    }
}
