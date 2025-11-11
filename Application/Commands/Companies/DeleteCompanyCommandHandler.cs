using MediatR;
using Microsoft.Extensions.Logging;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Commands.Companies;

/// <summary>
/// Handler for deleting a company (soft delete)
/// </summary>
public class DeleteCompanyCommandHandler : IRequestHandler<DeleteCompanyCommand, DeleteCompanyResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCompanyCommandHandler> _logger;

    public DeleteCompanyCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteCompanyCommandHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DeleteCompanyResult> Handle(DeleteCompanyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate input
            if (request.Id <= 0)
            {
                return new DeleteCompanyResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid company ID"
                };
            }

            // Find company
            var companiesRepo = _unitOfWork.Repository<Company>();
            var companies = await companiesRepo.GetAsync(c => c.Id == request.Id && !c.IsDeleted);

            var company = companies.FirstOrDefault();
            if (company == null)
            {
                return new DeleteCompanyResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Company with ID {request.Id} not found"
                };
            }

            // Check if company has active visitors
            var visitorCount = company.Visitors?.Where(v => !v.IsDeleted).Count() ?? 0;
            if (visitorCount > 0)
            {
                _logger.LogWarning(
                    "Cannot delete company {CompanyId} because it has {VisitorCount} active visitors",
                    request.Id,
                    visitorCount);

                return new DeleteCompanyResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Cannot delete company with {visitorCount} active visitors. Please reassign visitors first."
                };
            }

            // Soft delete the company
            company.IsDeleted = true;
            company.DeletedOn = DateTime.UtcNow;
            // Note: DeletedBy should be set by the DbContext interceptor using the current user context

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Company with ID {CompanyId} deleted successfully. Reason: {Reason}",
                request.Id,
                request.Reason ?? "Not provided");

            return new DeleteCompanyResult
            {
                IsSuccess = true,
                Message = $"Company '{company.Name}' has been successfully deleted"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting company with ID {CompanyId}", request.Id);
            return new DeleteCompanyResult
            {
                IsSuccess = false,
                ErrorMessage = "An error occurred while deleting the company"
            };
        }
    }
}
