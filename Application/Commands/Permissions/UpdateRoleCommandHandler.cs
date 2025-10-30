using AutoMapper;
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Permissions;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Permissions;

/// <summary>
/// Handler for UpdateRoleCommand
/// </summary>
public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, RoleDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateRoleCommandHandler> _logger;

    public UpdateRoleCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UpdateRoleCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<RoleDto> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get existing role
            var role = await _unitOfWork.Roles.GetByIdAsync(request.RoleId, cancellationToken);

            if (role == null)
            {
                throw new InvalidOperationException($"Role with ID {request.RoleId} not found");
            }

            // Prevent modification of system roles' critical properties
            if (role.IsSystemRole)
            {
                _logger.LogWarning("Attempted to modify system role: {RoleName}", role.Name);
                // Only allow updating certain properties for system roles
                role.DisplayName = request.UpdateRoleDto.DisplayName;
                role.Description = request.UpdateRoleDto.Description;
                role.Color = request.UpdateRoleDto.Color;
                role.Icon = request.UpdateRoleDto.Icon;
                role.DisplayOrder = request.UpdateRoleDto.DisplayOrder;
            }
            else
            {
                // Map all changes for non-system roles
                _mapper.Map(request.UpdateRoleDto, role);
            }

            // Update role
            await _unitOfWork.Roles.UpdateAsync(role, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated role: {RoleName} with ID {RoleId}", role.Name, role.Id);

            // Return DTO
            var roleDto = _mapper.Map<RoleDto>(role);
            return roleDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role with ID {RoleId}", request.RoleId);
            throw;
        }
    }
}
