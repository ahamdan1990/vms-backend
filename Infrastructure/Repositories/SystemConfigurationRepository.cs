using Microsoft.EntityFrameworkCore;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Infrastructure.Data;
using VisitorManagementSystem.Api.Infrastructure.Data.Repositories;

namespace VisitorManagementSystem.Api.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for SystemConfiguration entity
/// </summary>
public class SystemConfigurationRepository : BaseRepository<SystemConfiguration>, ISystemConfigurationRepository
{
    public SystemConfigurationRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<SystemConfiguration?> GetByCategoryAndKeyAsync(string category, string key, CancellationToken cancellationToken = default)
    {
        return await _context.SystemConfigurations
            .FirstOrDefaultAsync(c => c.Category == category && c.Key == key, cancellationToken);
    }

    public async Task<List<SystemConfiguration>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        return await _context.SystemConfigurations
            .Where(c => c.Category == category)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Key)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<string>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SystemConfigurations
            .Select(c => c.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SystemConfiguration>> GetConfigurationsRequiringRestartAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SystemConfigurations
            .Where(c => c.RequiresRestart)
            .OrderBy(c => c.Category)
            .ThenBy(c => c.Key)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SystemConfiguration>> GetEncryptedConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SystemConfigurations
            .Where(c => c.IsEncrypted)
            .OrderBy(c => c.Category)
            .ThenBy(c => c.Key)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SystemConfiguration>> GetByEnvironmentAsync(string environment, CancellationToken cancellationToken = default)
    {
        return await _context.SystemConfigurations
            .Where(c => c.Environment == environment || c.Environment == "All")
            .OrderBy(c => c.Category)
            .ThenBy(c => c.DisplayOrder)
            .ThenBy(c => c.Key)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SystemConfiguration>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var lowerSearchTerm = searchTerm.ToLower();
        
        return await _context.SystemConfigurations
            .Where(c => c.Key.ToLower().Contains(lowerSearchTerm) ||
                       c.Category.ToLower().Contains(lowerSearchTerm) ||
                       (c.Description != null && c.Description.ToLower().Contains(lowerSearchTerm)))
            .OrderBy(c => c.Category)
            .ThenBy(c => c.Key)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SystemConfiguration>> GetModifiedSinceAsync(DateTime since, CancellationToken cancellationToken = default)
    {
        return await _context.SystemConfigurations
            .Where(c => c.ModifiedOn.HasValue && c.ModifiedOn.Value >= since)
            .OrderByDescending(c => c.ModifiedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> BulkUpdateAsync(List<SystemConfiguration> configurations, int modifiedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var config in configurations)
            {
                config.UpdateModifiedBy(modifiedBy);
                _context.SystemConfigurations.Update(config);
            }

            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            return 0;
        }
    }

    public async Task<bool> ExistsAsync(string category, string key, CancellationToken cancellationToken = default)
    {
        return await _context.SystemConfigurations
            .AnyAsync(c => c.Category == category && c.Key == key, cancellationToken);
    }
}
