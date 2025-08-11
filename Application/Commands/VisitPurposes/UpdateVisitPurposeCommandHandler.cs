using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.VisitPurposes;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.VisitPurposes;

/// <summary>
/// Handler for update visit purpose command
/// </summary>
public class UpdateVisitPurposeCommandHandler : IRequestHandler<UpdateVisitPurposeCommand, VisitPurposeDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateVisitPurposeCommandHandler> _logger;

    public UpdateVisitPurposeCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UpdateVisitPurposeCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<VisitPurposeDto> Handle(UpdateVisitPurposeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Updating visit purpose {VisitPurposeId}: {Name}", request.Id, request.Name);

            // Get existing visit purpose
            var visitPurpose = await _unitOfWork.Repository<VisitPurpose>()
                .GetByIdAsync(request.Id, cancellationToken);

            if (visitPurpose == null)
            {
                throw new KeyNotFoundException($"Visit purpose with ID {request.Id} not found");
            }

            // Check if another visit purpose with same name already exists (excluding current one)
            var existingPurpose = await _unitOfWork.Repository<VisitPurpose>()
                .FirstOrDefaultAsync(vp => vp.Name.ToLower() == request.Name.ToLower() && vp.Id != request.Id, cancellationToken);

            if (existingPurpose != null)
            {
                throw new InvalidOperationException($"Visit purpose with name '{request.Name}' already exists");
            }

            // Update properties
            visitPurpose.Name = request.Name.Trim();
            visitPurpose.Description = request.Description?.Trim();
            visitPurpose.RequiresApproval = request.RequiresApproval;
            visitPurpose.IsActive = request.IsActive;
            visitPurpose.DisplayOrder = request.DisplayOrder;
            visitPurpose.ColorCode = request.ColorCode?.Trim();
            visitPurpose.IconName = request.IconName?.Trim();
            visitPurpose.ModifiedBy = request.UpdatedBy;
            visitPurpose.ModifiedOn = DateTime.UtcNow;

            // Update in repository
            _unitOfWork.Repository<VisitPurpose>().Update(visitPurpose);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated visit purpose {VisitPurposeId}: {Name}", 
                visitPurpose.Id, visitPurpose.Name);

            // Map to DTO and return
            var dto = _mapper.Map<VisitPurposeDto>(visitPurpose);
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating visit purpose {VisitPurposeId}: {Name}", request.Id, request.Name);
            throw;
        }
    }
}
