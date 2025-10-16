using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Visitors;

/// <summary>
/// Handler for get VIP visitors query
/// </summary>
public class GetVipVisitorsQueryHandler : IRequestHandler<GetVipVisitorsQuery, List<VisitorListDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetVipVisitorsQueryHandler> _logger;

    public GetVipVisitorsQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetVipVisitorsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<VisitorListDto>> Handle(GetVipVisitorsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing get VIP visitors query");

            // Get VIP visitors from repository
            var vipVisitors = await _unitOfWork.Visitors.GetVipVisitorsAsync(cancellationToken);

            // Filter out deleted visitors if not including them
            if (!request.IncludeDeleted)
            {
                vipVisitors = vipVisitors.Where(v => !v.IsDeleted).ToList();
            }

            // Map to DTOs with masked information
            var vipVisitorDtos = vipVisitors
                .Select(v => v.GetMaskedInfo())
                .OrderBy(v => v.FullName)
                .ToList();

            var mappedDtos = _mapper.Map<List<VisitorListDto>>(vipVisitorDtos);

            _logger.LogDebug("Retrieved {Count} VIP visitors", mappedDtos.Count);

            return mappedDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving VIP visitors");
            throw;
        }
    }
}
