using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Locations;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Locations;

/// <summary>
/// Handler for update location command
/// </summary>
public class UpdateLocationCommandHandler : IRequestHandler<UpdateLocationCommand, LocationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateLocationCommandHandler> _logger;

    public UpdateLocationCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UpdateLocationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<LocationDto> Handle(UpdateLocationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Updating location {LocationId}: {Name}", request.Id, request.Name);

            // Get existing location
            var location = await _unitOfWork.Repository<Location>()
                .GetByIdAsync(request.Id, cancellationToken);

            if (location == null)
            {
                throw new KeyNotFoundException($"Location with ID {request.Id} not found");
            }

            // Check if another location with same name already exists (excluding current one)
            var existingLocation = await _unitOfWork.Repository<Location>()
                .FirstOrDefaultAsync(l => l.Name.ToLower() == request.Name.ToLower() && l.Id != request.Id, cancellationToken);

            if (existingLocation != null)
            {
                throw new InvalidOperationException($"Location with name '{request.Name}' already exists");
            }

            // Check if code is unique (if provided and different from current)
            if (!string.IsNullOrEmpty(request.Code) && request.Code != location.Code)
            {
                var existingCode = await _unitOfWork.Repository<Location>()
                    .FirstOrDefaultAsync(l => l.Code != null && l.Code.ToLower() == request.Code.ToLower() && l.Id != request.Id, cancellationToken);

                if (existingCode != null)
                {
                    throw new InvalidOperationException($"Location with code '{request.Code}' already exists");
                }
            }

            // Validate parent location (if specified)
            if (request.ParentLocationId.HasValue)
            {
                if (request.ParentLocationId == request.Id)
                {
                    throw new InvalidOperationException("Location cannot be its own parent");
                }

                var parentExists = await _unitOfWork.Repository<Location>()
                    .AnyAsync(l => l.Id == request.ParentLocationId, cancellationToken);

                if (!parentExists)
                {
                    throw new InvalidOperationException($"Parent location with ID {request.ParentLocationId} not found");
                }
            }

            // Update properties
            location.Name = request.Name.Trim();
            location.Code = request.Code?.Trim();
            location.Description = request.Description?.Trim();
            location.LocationType = request.LocationType?.Trim();
            location.Floor = request.Floor?.Trim();
            location.Building = request.Building?.Trim();
            location.Zone = request.Zone?.Trim();
            location.ParentLocationId = request.ParentLocationId;
            location.DisplayOrder = request.DisplayOrder;
            location.MaxCapacity = request.MaxCapacity ?? 0;
            location.RequiresEscort = request.RequiresEscort;
            location.AccessLevel = request.AccessLevel?.Trim();
            location.IsActive = request.IsActive;
            location.ModifiedBy = request.UpdatedBy;
            location.ModifiedOn = DateTime.UtcNow;

            // Update in repository
            _unitOfWork.Repository<Location>().Update(location);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated location {LocationId}: {Name}", 
                location.Id, location.Name);

            // Map to DTO and return
            var dto = _mapper.Map<LocationDto>(location);
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating location {LocationId}: {Name}", request.Id, request.Name);
            throw;
        }
    }
}
