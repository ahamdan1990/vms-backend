using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Application.DTOs.Permissions;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Permissions;

/// <summary>
/// Handler for CreateRoleCommand
/// </summary>
public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, RoleDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateRoleCommandHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateRoleCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper _mapper,
        ILogger<CreateRoleCommandHandler> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        this._mapper = _mapper;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<RoleDto> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if role name already exists
            var existingRole = await _unitOfWork.Roles
                .GetAsync(r => r.Name == request.CreateRoleDto.Name, cancellationToken);

            if (existingRole.Any())
            {
                throw new InvalidOperationException($"Role with name '{request.CreateRoleDto.Name}' already exists");
            }

            // Map DTO to entity
            var role = _mapper.Map<Role>(request.CreateRoleDto);

            // Get current user ID for audit
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                role.CreatedBy = userId;
            }

            // Add role
            await _unitOfWork.Roles.AddAsync(role, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Grant permissions if provided
            if (request.CreateRoleDto.PermissionIds.Any())
            {
                var grantedBy = role.CreatedBy ?? 0;
                await _unitOfWork.RolePermissions.BulkGrantPermissionsAsync(
                    role.Id,
                    request.CreateRoleDto.PermissionIds,
                    grantedBy,
                    cancellationToken);
            }

            _logger.LogInformation("Created new role: {RoleName} with ID {RoleId}", role.Name, role.Id);

            // Return DTO
            var roleDto = _mapper.Map<RoleDto>(role);
            roleDto.PermissionCount = request.CreateRoleDto.PermissionIds.Count;
            roleDto.UserCount = 0;

            return roleDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role");
            throw;
        }
    }
}
