using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using VisitorManagementSystem.Api.Application.DTOs.Companies;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Domain.ValueObjects;

namespace VisitorManagementSystem.Api.Application.Commands.Companies;

/// <summary>
/// Handler for updating a company
/// </summary>
public class UpdateCompanyCommandHandler : IRequestHandler<UpdateCompanyCommand, UpdateCompanyResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateCompanyCommandHandler> _logger;

    public UpdateCompanyCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<UpdateCompanyCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UpdateCompanyResult> Handle(UpdateCompanyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate input
            if (request.Id <= 0)
            {
                return new UpdateCompanyResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid company ID"
                };
            }

            if (request.CompanyData == null)
            {
                return new UpdateCompanyResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Company data is required"
                };
            }

            // Find company
            var companiesRepo = _unitOfWork.Repository<Company>();
            var companies = await companiesRepo.GetAsync(c => c.Id == request.Id && !c.IsDeleted);

            var company = companies.FirstOrDefault();
            if (company == null)
            {
                return new UpdateCompanyResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Company with ID {request.Id} not found"
                };
            }

            // Update properties
            if (!string.IsNullOrWhiteSpace(request.CompanyData.Name))
                company.Name = request.CompanyData.Name.Trim();

            if (!string.IsNullOrWhiteSpace(request.CompanyData.Website))
                company.Website = request.CompanyData.Website.Trim();

            if (!string.IsNullOrWhiteSpace(request.CompanyData.Industry))
                company.Industry = request.CompanyData.Industry.Trim();

            if (!string.IsNullOrWhiteSpace(request.CompanyData.ContactPersonName))
                company.ContactPersonName = request.CompanyData.ContactPersonName.Trim();

            if (!string.IsNullOrWhiteSpace(request.CompanyData.Email))
            {
                try
                {
                    company.Email = new Email(request.CompanyData.Email);
                }
                catch (ArgumentException ex)
                {
                    return new UpdateCompanyResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Invalid email: {ex.Message}"
                    };
                }
            }

            if (!string.IsNullOrWhiteSpace(request.CompanyData.PhoneNumber))
            {
                try
                {
                    company.PhoneNumber = new PhoneNumber(request.CompanyData.PhoneNumber);
                }
                catch (ArgumentException ex)
                {
                    return new UpdateCompanyResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Invalid phone number: {ex.Message}"
                    };
                }
            }

            if (request.CompanyData.EmployeeCount.HasValue)
                company.EmployeeCount = request.CompanyData.EmployeeCount;

            if (!string.IsNullOrWhiteSpace(request.CompanyData.Description))
                company.Description = request.CompanyData.Description.Trim();

            // Update address if provided
            if (!string.IsNullOrWhiteSpace(request.CompanyData.Street1))
            {
                try
                {
                    company.Address = new Address(
                        street1: request.CompanyData.Street1,
                        street2: request.CompanyData.Street2,
                        city: request.CompanyData.City,
                        state: request.CompanyData.State,
                        postalCode: request.CompanyData.PostalCode,
                        country: request.CompanyData.Country
                    );
                }
                catch (ArgumentException ex)
                {
                    return new UpdateCompanyResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Invalid address: {ex.Message}"
                    };
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Company with ID {CompanyId} updated successfully", request.Id);

            var companyDto = _mapper.Map<CompanyDto>(company);

            return new UpdateCompanyResult
            {
                IsSuccess = true,
                Company = companyDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating company with ID {CompanyId}", request.Id);
            return new UpdateCompanyResult
            {
                IsSuccess = false,
                ErrorMessage = "An error occurred while updating the company"
            };
        }
    }
}
