using MediatR;
using AutoMapper;
using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Application.DTOs.Auth;

namespace VisitorManagementSystem.Api.Application.Queries.Auth
{
    /// <summary>
    /// Query for validating access token
    /// </summary>
    public class ValidateTokenQuery : IRequest<TokenValidationResult>
    {
        public string AccessToken { get; set; } = string.Empty;
    }
}
