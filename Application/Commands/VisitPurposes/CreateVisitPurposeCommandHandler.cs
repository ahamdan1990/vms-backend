using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.VisitPurposes;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.VisitPurposes;

/// <summary>
/// Handler for create visit purpose command
/// </summary>
public class CreateVisitPurposeCommandHandler : IRequestHandler<CreateVisitPurposeCommand, VisitPurposeDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateVisitPurposeCommandHandler> _logger;

    public CreateVisitPurposeCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreateVisitPurposeCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<VisitPurposeDto> Handle(CreateVisitPurposeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Creating visit purpose: {Name}", request.Name);

            // Check if visit purpose with same name already exists
            var existingPurpose = await _unitOfWork.Repository<VisitPurpose>()
                .FirstOrDefaultAsync(vp => vp.Name.ToLower() == request.Name.ToLower(), cancellationToken);

            if (existingPurpose != null)
            {
                throw new InvalidOperationException($"Visit purpose with name '{request.Name}' already exists");
            }

            // Create new visit purpose
            var visitPurpose = new VisitPurpose
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                RequiresApproval = request.RequiresApproval,
                IsActive = request.IsActive,
                DisplayOrder = request.DisplayOrder,
                ColorCode = request.ColorCode?.Trim(),
                IconName = request.IconName?.Trim(),
                CreatedBy = request.CreatedBy,
                CreatedOn = DateTime.UtcNow
            };

            // Add to repository
            await _unitOfWork.Repository<VisitPurpose>().AddAsync(visitPurpose, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created visit purpose {VisitPurposeId}: {Name}", 
                visitPurpose.Id, visitPurpose.Name);

            // Map to DTO and return
            var dto = _mapper.Map<VisitPurposeDto>(visitPurpose);
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating visit purpose: {Name}", request.Name);
            throw;
        }
    }
}
