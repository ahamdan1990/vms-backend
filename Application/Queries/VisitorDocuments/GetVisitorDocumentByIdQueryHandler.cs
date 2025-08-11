using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.VisitorDocuments;

/// <summary>
/// Handler for get visitor document by ID query
/// </summary>
public class GetVisitorDocumentByIdQueryHandler : IRequestHandler<GetVisitorDocumentByIdQuery, VisitorDocumentDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetVisitorDocumentByIdQueryHandler> _logger;

    public GetVisitorDocumentByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetVisitorDocumentByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<VisitorDocumentDto?> Handle(GetVisitorDocumentByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting visitor document by ID: {DocumentId}", request.Id);

            var document = await _unitOfWork.VisitorDocuments.GetByIdAsync(request.Id, cancellationToken);
            
            // Return null if not found or deleted (when not including deleted)
            if (document == null || (!request.IncludeDeleted && document.IsDeleted))
            {
                return null;
            }

            var documentDto = _mapper.Map<VisitorDocumentDto>(document);
            return documentDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting visitor document by ID: {DocumentId}", request.Id);
            throw;
        }
    }
}
