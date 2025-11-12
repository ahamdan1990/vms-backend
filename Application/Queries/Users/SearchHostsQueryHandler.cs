using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Application.Services.Auth;
using VisitorManagementSystem.Api.Domain.Enums;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Queries.Users;

/// <summary>
/// Handles host search queries by merging local staff users with LDAP directory entries.
/// </summary>
public class SearchHostsQueryHandler : IRequestHandler<SearchHostsQuery, List<HostSearchResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILdapService _ldapService;
    private readonly ILogger<SearchHostsQueryHandler> _logger;
    private readonly ILdapSettingsProvider _ldapSettingsProvider;

    public SearchHostsQueryHandler(
        IUnitOfWork unitOfWork,
        ILdapService ldapService,
        ILdapSettingsProvider ldapSettingsProvider,
        ILogger<SearchHostsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _ldapService = ldapService ?? throw new ArgumentNullException(nameof(ldapService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ldapSettingsProvider = ldapSettingsProvider ?? throw new ArgumentNullException(nameof(ldapSettingsProvider));
    }

    public async Task<List<HostSearchResultDto>> Handle(SearchHostsQuery request, CancellationToken cancellationToken)
    {
        var results = new List<HostSearchResultDto>();
        var trimmedTerm = request.SearchTerm?.Trim();

        if (string.IsNullOrWhiteSpace(trimmedTerm) || trimmedTerm.Length < 2)
        {
            return results;
        }

        var maxResults = request.MaxResults <= 0 ? 10 : Math.Min(request.MaxResults, 25);

        // First pull staff users from the local database
        var localUsers = await _unitOfWork.Users.SearchAsync(
            trimmedTerm,
            role: null,
            status: UserStatus.Active,
            department: null,
            cancellationToken);

        var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var user in localUsers
                     .OrderBy(u => u.FirstName)
                     .ThenBy(u => u.LastName)
                     .Take(maxResults))
        {
            if (string.IsNullOrWhiteSpace(user.Email?.Value))
            {
                continue;
            }

            seenEmails.Add(user.Email.Value);

            results.Add(new HostSearchResultDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                Email = user.Email.Value,
                Department = user.Department,
                JobTitle = user.JobTitle,
                PhoneNumber = user.PhoneNumber?.Value,
                ExistsInSystem = true,
                IsLdapUser = user.IsLdapUser,
                Source = "local"
            });
        }

        var remainingSlots = maxResults - results.Count;

        if (remainingSlots > 0 && request.IncludeDirectory)
        {
            try
            {
                var ldapSettings = await _ldapSettingsProvider.GetSettingsAsync(cancellationToken: cancellationToken);

                if (!ldapSettings.Enabled || !ldapSettings.IncludeDirectoryUsersInHostSearch)
                {
                    return results;
                }

                var directoryResults = await _ldapService.SearchUsersAsync(trimmedTerm);

                foreach (var directoryUser in directoryResults)
                {
                    if (remainingSlots <= 0)
                    {
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(directoryUser.Email))
                    {
                        continue;
                    }

                    if (seenEmails.Contains(directoryUser.Email))
                    {
                        continue;
                    }

                    seenEmails.Add(directoryUser.Email);
                    remainingSlots--;

                    results.Add(new HostSearchResultDto
                    {
                        Id = null,
                        FirstName = directoryUser.FirstName,
                        LastName = directoryUser.LastName,
                        FullName = !string.IsNullOrWhiteSpace(directoryUser.DisplayName)
                            ? directoryUser.DisplayName!
                            : $"{directoryUser.FirstName} {directoryUser.LastName}".Trim(),
                        Email = directoryUser.Email,
                        Department = directoryUser.Department,
                        JobTitle = directoryUser.JobTitle,
                        PhoneNumber = directoryUser.Phone,
                        Company = directoryUser.Company,
                        ExistsInSystem = false,
                        IsLdapUser = true,
                        Source = "directory",
                        DirectoryIdentifier = directoryUser.Username ?? directoryUser.Email
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search LDAP directory for hosts using term {SearchTerm}", trimmedTerm);
            }
        }

        return results;
    }
}
