using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.VisitPurposes;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.VisitPurposes;

/// <summary>
/// Handler for getting visit purpose by ID query
/// </summary>
public class GetVisitPurposeByIdQueryHandler : IRequestHandler<GetVisitPurposeByIdQuery, VisitPurposeDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetVisitPurposeByIdQueryHandler> _logger;

    public GetVisitPurposeByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetVisitPurposeByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<VisitPurposeDto?> Handle(GetVisitPurposeByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting visit purpose by ID: {VisitPurposeId}", request.Id);

            var visitPurpose = await _unitOfWork.Repository<Domain.Entities.VisitPurpose>()
                .GetByIdAsync(request.Id, cancellationToken);
            
            if (visitPurpose == null)
            {
                _logger.LogWarning("Visit purpose not found with ID: {VisitPurposeId}", request.Id);
                return null;
            }

            var dto = _mapper.Map<VisitPurposeDto>(visitPurpose);
            
            _logger.LogDebug("Successfully retrieved visit purpose {VisitPurposeId}", request.Id);

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting visit purpose by ID: {VisitPurposeId}", request.Id);
            throw;
        }
    }
}
