using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Common;
using VisitorManagementSystem.Api.Controllers;

namespace VisitorManagementSystem.Api.Application.Commands.Invitations
{
    public record SendQrEmailCommand(int InvitationId, SendQrEmailDto EmailOptions)
    : IRequest<ApiResponseDto<object>>;
}
