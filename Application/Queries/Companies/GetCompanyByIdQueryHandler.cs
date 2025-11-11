using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using VisitorManagementSystem.Api.Application.DTOs.Companies;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Companies;

/// <summary>
/// Handler for getting a single company by ID
/// </summary>
public class GetCompanyByIdQueryHandler : IRequestHandler<GetCompanyByIdQuery, GetCompanyByIdResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<GetCompanyByIdQueryHandler> _logger;

    public GetCompanyByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<GetCompanyByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetCompanyByIdResult> Handle(GetCompanyByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Id <= 0)
            {
                return new GetCompanyByIdResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid company ID"
                };
            }

            var companiesRepo = _unitOfWork.Repository<Company>();
            var companies = await companiesRepo.GetAsync(c => c.Id == request.Id && !c.IsDeleted);

            var company = companies.FirstOrDefault();

            if (company == null)
            {
                _logger.LogWarning("Company with ID {CompanyId} not found", request.Id);
                return new GetCompanyByIdResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Company with ID {request.Id} not found"
                };
            }

            var companyDto = _mapper.Map<CompanyDto>(company);

            _logger.LogInformation("Retrieved company with ID {CompanyId}", request.Id);

            return new GetCompanyByIdResult
            {
                IsSuccess = true,
                Company = companyDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving company with ID {CompanyId}", request.Id);
            return new GetCompanyByIdResult
            {
                IsSuccess = false,
                ErrorMessage = "An error occurred while retrieving the company"
            };
        }
    }
}
