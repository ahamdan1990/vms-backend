using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.CivilDefense;

namespace VisitorManagementSystem.Api.Application.Queries.CivilDefense;

public class CdGetLiveBuildingOccupancyQuery : IRequest<CdLiveOccupancyDto>
{
}
