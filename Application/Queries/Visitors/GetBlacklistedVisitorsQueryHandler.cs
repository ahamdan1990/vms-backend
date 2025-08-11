using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Visitors;

/// <summary>
/// Handler for get blacklisted visitors query
/// </summary>
public class GetBlacklistedVisitorsQueryHandler : IRequestHandler<GetBlacklistedVisitorsQuery, List<VisitorListDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetBlacklistedVisitorsQueryHandler> _logger;

    public GetBlacklistedVisitorsQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetBlacklistedVisitorsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<VisitorListDto>> Handle(GetBlacklistedVisitorsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing get blacklisted visitors query");

            // Get blacklisted visitors from repository
            var blacklistedVisitors = await _unitOfWork.Visitors.GetBlacklistedVisitorsAsync(cancellationToken);

            // Filter out deleted visitors if not including them
            if (!request.IncludeDeleted)
            {
                blacklistedVisitors = blacklistedVisitors.Where(v => !v.IsDeleted).ToList();
            }

            // Map to DTOs with masked information
            var blacklistedVisitorDtos = blacklistedVisitors
                .Select(v => v.GetMaskedInfo())
                .OrderBy(v => v.FullName)
                .ToList();

            var mappedDtos = _mapper.Map<List<VisitorListDto>>(blacklistedVisitorDtos);

            _logger.LogDebug("Retrieved {Count} blacklisted visitors", mappedDtos.Count);

            return mappedDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving blacklisted visitors");
            throw;
        }
    }
}
