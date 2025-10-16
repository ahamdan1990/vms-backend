using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Visitors;

/// <summary>
/// Handler for get visitor by ID query
/// </summary>
public class GetVisitorByIdQueryHandler : IRequestHandler<GetVisitorByIdQuery, VisitorDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetVisitorByIdQueryHandler> _logger;

    public GetVisitorByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetVisitorByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<VisitorDto?> Handle(GetVisitorByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing get visitor by ID query: {Id}", request.Id);

            // Get visitor with related data
            var visitor = await _unitOfWork.Visitors.GetByIdAsync(
                request.Id,
                v => v.EmergencyContacts,
                v => v.Documents,
                v => v.VisitorNotes,
                v => v.CreatedByUser!,
                v => v.ModifiedByUser!,
                v => v.BlacklistedByUser!
            );

            if (visitor == null)
            {
                _logger.LogDebug("Visitor not found: {Id}", request.Id);
                return null;
            }

            // Check if deleted and not including deleted
            if (visitor.IsDeleted && !request.IncludeDeleted)
            {
                _logger.LogDebug("Visitor is deleted and IncludeDeleted is false: {Id}", request.Id);
                return null;
            }

            // Map to DTO
            var visitorDto = _mapper.Map<VisitorDto>(visitor);

            _logger.LogDebug("Retrieved visitor: {Id} - {FullName}", visitor.Id, visitor.FullName);

            return visitorDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving visitor with ID: {Id}", request.Id);
            throw;
        }
    }
}
