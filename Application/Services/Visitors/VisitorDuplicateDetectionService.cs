using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Services.Visitors;

/// <summary>
/// Service for detecting duplicate visitors during registration
/// </summary>
public class VisitorDuplicateDetectionService : IVisitorDuplicateDetectionService
{
    private readonly IUnitOfWork _unitOfWork;

    public VisitorDuplicateDetectionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Finds an existing visitor that matches the provided criteria
    /// </summary>
    public async Task<Visitor?> FindDuplicateVisitorAsync(
        string email,
        string? phoneNumber = null,
        string? firstName = null,
        string? lastName = null,
        CancellationToken cancellationToken = default)
    {
        // Primary match: Email (case-insensitive)
        // Email is the most reliable identifier for visitors
        var normalizedEmail = email.ToUpperInvariant();
        var existingVisitor = await _unitOfWork.Visitors
            .GetByEmailAsync(normalizedEmail, cancellationToken);

        if (existingVisitor != null)
        {
            return existingVisitor;
        }

        // Secondary match: Phone number (if provided)
        // Some visitors may have typos in email but same phone number
        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            existingVisitor = await _unitOfWork.Visitors
                .GetByPhoneNumberAsync(phoneNumber, cancellationToken);

            if (existingVisitor != null)
            {
                return existingVisitor;
            }
        }

        // No duplicate found
        return null;
    }
}
