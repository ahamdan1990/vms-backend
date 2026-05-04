using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.CivilDefense;

public class CdSearchVisitorQueryHandler : IRequestHandler<CdSearchVisitorQuery, List<VisitorDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CdSearchVisitorQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<VisitorDto>> Handle(CdSearchVisitorQuery request, CancellationToken cancellationToken)
    {
        var (visitors, _) = await _unitOfWork.Visitors.SearchVisitorsAsync(
            request.SearchTerm, 0, request.MaxResults, cancellationToken: cancellationToken);

        return _mapper.Map<List<VisitorDto>>(visitors);
    }
}
