using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using VisitorManagementSystem.Api.Application.DTOs.Companies;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Domain.ValueObjects;

namespace VisitorManagementSystem.Api.Application.Commands.Companies
{
    /// <summary>
    /// Handler for creating a new company record
    /// </summary>
    public class CreateCompanyCommandHandler : IRequestHandler<CreateCompanyCommand, CreateCompanyResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateCompanyCommandHandler> _logger;

        public CreateCompanyCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<CreateCompanyCommandHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CreateCompanyResult> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.CompanyData == null)
                {
                    return new CreateCompanyResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Company data is required"
                    };
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(request.CompanyData.Name))
                {
                    return new CreateCompanyResult
                    {
                        IsSuccess = false,
                        Errors = new List<string> { "Company name is required" }
                    };
                }

                if (string.IsNullOrWhiteSpace(request.CompanyData.Code))
                {
                    return new CreateCompanyResult
                    {
                        IsSuccess = false,
                        Errors = new List<string> { "Company code is required" }
                    };
                }

                // Check if company with same code already exists
                var companiesRepo = _unitOfWork.Repository<Company>();
                var existingCompanies = await companiesRepo.GetAsync(
                    c => c.Code == request.CompanyData.Code.ToUpperInvariant() && !c.IsDeleted);
                if (existingCompanies.Any())
                {
                    _logger.LogWarning("Attempt to create company with duplicate code: {Code}", request.CompanyData.Code);
                    return new CreateCompanyResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"A company with code '{request.CompanyData.Code}' already exists"
                    };
                }

                _logger.LogInformation("Creating new company: {CompanyName} ({CompanyCode})", request.CompanyData.Name, request.CompanyData.Code);

                // Create company entity
                var company = new Company
                {
                    Name = request.CompanyData.Name.Trim(),
                    Code = request.CompanyData.Code.Trim().ToUpperInvariant(),
                    Website = string.IsNullOrWhiteSpace(request.CompanyData.Website) ? null : request.CompanyData.Website.Trim(),
                    Industry = string.IsNullOrWhiteSpace(request.CompanyData.Industry) ? null : request.CompanyData.Industry.Trim(),
                    TaxId = string.IsNullOrWhiteSpace(request.CompanyData.TaxId) ? null : request.CompanyData.TaxId.Trim(),
                    ContactPersonName = string.IsNullOrWhiteSpace(request.CompanyData.ContactPersonName) ? null : request.CompanyData.ContactPersonName.Trim(),
                    Email = string.IsNullOrWhiteSpace(request.CompanyData.Email) ? null : new Email(request.CompanyData.Email),
                    PhoneNumber = string.IsNullOrWhiteSpace(request.CompanyData.PhoneNumber) ? null : new PhoneNumber(request.CompanyData.PhoneNumber),
                    Address = BuildAddress(request.CompanyData),
                    EmployeeCount = request.CompanyData.EmployeeCount,
                    Description = string.IsNullOrWhiteSpace(request.CompanyData.Description) ? null : request.CompanyData.Description.Trim(),
                    IsVerified = false,
                    IsActive = true,
                    DisplayOrder = 0,
                    VisitorCount = 0,
                    CreatedOn = DateTime.UtcNow,
                    IsDeleted = false
                };

                // Add company to repository
                await companiesRepo.AddAsync(company, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Company created successfully: Id={CompanyId}, Code={Code}", company.Id, company.Code);

                // Map to DTO and return
                var companyDto = _mapper.Map<CompanyDto>(company);
                return new CreateCompanyResult
                {
                    IsSuccess = true,
                    Company = companyDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating company");
                return new CreateCompanyResult
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred while creating the company"
                };
            }
        }

        /// <summary>
        /// Builds an Address value object from the request
        /// </summary>
        private Address? BuildAddress(CreateCompanyDto companyData)
        {
            if (string.IsNullOrWhiteSpace(companyData.Street1) &&
                string.IsNullOrWhiteSpace(companyData.City) &&
                string.IsNullOrWhiteSpace(companyData.Country))
            {
                return null; // No address provided
            }

            return new Address(
                street1: companyData.Street1?.Trim() ?? string.Empty,
                street2: companyData.Street2?.Trim(),
                city: companyData.City?.Trim() ?? string.Empty,
                state: companyData.State?.Trim(),
                postalCode: companyData.PostalCode?.Trim(),
                country: companyData.Country?.Trim() ?? string.Empty
            );
        }
    }
}
