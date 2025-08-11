using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Locations;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Locations;

/// <summary>
/// Handler for create location command
/// </summary>
public class CreateLocationCommandHandler : IRequestHandler<CreateLocationCommand, LocationDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateLocationCommandHandler> _logger;

    public CreateLocationCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CreateLocationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<LocationDto> Handle(CreateLocationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing create location command: {LocationName}", request.Name);

            // Check if code already exists
            var existingLocation = await _unitOfWork.Locations.GetByCodeAsync(request.Code, cancellationToken);
            if (existingLocation != null)
            {
                throw new InvalidOperationException($"A location with code '{request.Code}' already exists.");
            }

            // Validate parent location if specified
            if (request.ParentLocationId.HasValue)
            {
                var parentLocation = await _unitOfWork.Locations.GetByIdAsync(request.ParentLocationId.Value, cancellationToken);
                if (parentLocation == null)
                {
                    throw new InvalidOperationException($"Parent location with ID '{request.ParentLocationId}' not found.");
                }
            }

            // Create location entity
            var location = new Location
            {
                Name = request.Name.Trim(),
                Code = request.Code.Trim().ToUpper(),
                Description = request.Description?.Trim(),
                LocationType = request.LocationType.Trim(),
                Floor = request.Floor?.Trim(),
                Building = request.Building?.Trim(),
                Zone = request.Zone?.Trim(),
                ParentLocationId = request.ParentLocationId,
                DisplayOrder = request.DisplayOrder,
                MaxCapacity = request.MaxCapacity,
                RequiresEscort = request.RequiresEscort,
                AccessLevel = request.AccessLevel.Trim(),
                IsActive = true
            };

            // Set audit information
            location.SetCreatedBy(request.CreatedBy);

            // Add to repository
            await _unitOfWork.Locations.AddAsync(location, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Location created successfully: {LocationId} ({Code}) by {CreatedBy}",
                location.Id, location.Code, request.CreatedBy);

            // Map to DTO and return
            var locationDto = _mapper.Map<LocationDto>(location);
            return locationDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating location: {LocationName}", request.Name);
            throw;
        }
    }
}
