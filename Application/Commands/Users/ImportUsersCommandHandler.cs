// Application/Commands/Users/ImportUsersCommandHandler.cs
using MediatR;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Application.Services.Users;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Application.Commands.Users;

/// <summary>
/// Handles the ImportUsersCommand:
/// 1. Parses the uploaded file via IUserImportService
/// 2. Validates all rows (field-level + within-file duplicate detection)
/// 3. Batch-checks emails and employeeIds against the database
/// 4. Optionally returns early (dryRun) or proceeds to create users
/// 5. Dispatches one CreateUserCommand per valid row (each in its own transaction)
/// 6. Returns a detailed result for the UI
/// </summary>
public class ImportUsersCommandHandler : IRequestHandler<ImportUsersCommand, ImportUsersResultDto>
{
    private readonly IUserImportService _importService;
    private readonly IMediator _mediator;
    private readonly ILogger<ImportUsersCommandHandler> _logger;

    public ImportUsersCommandHandler(
        IUserImportService importService,
        IMediator mediator,
        ILogger<ImportUsersCommandHandler> logger)
    {
        _importService = importService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ImportUsersResultDto> Handle(
        ImportUsersCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting user import. DryRun={DryRun}, SkipInvalidRows={Skip}, ImportedBy={User}",
            request.DryRun, request.SkipInvalidRows, request.ImportedBy);

        var overallResult = new ImportUsersResultDto();

        // ── Step 1: Parse file ────────────────────────────────────────────────
        var parseResult = await _importService.ParseFileAsync(request.File, cancellationToken);

        if (!parseResult.Success)
        {
            // File could not be parsed at all — return immediately with file errors
            // wrapped as a single "failed" entry so the frontend can display them
            overallResult.Results.Add(new ImportUserRowResultDto
            {
                RowNumber = 0,
                Status = ImportRowStatus.Failed,
                ErrorMessage = string.Join(" | ", parseResult.FileErrors),
                FieldErrors = parseResult.FileErrors
                    .Select(e => new ImportFieldErrorDto { ColumnName = "File", Message = e })
                    .ToList()
            });
            overallResult.FailedCount = 1;
            overallResult.TotalRows = 0;
            return overallResult;
        }

        if (parseResult.Rows.Count == 0)
        {
            overallResult.Results.Add(new ImportUserRowResultDto
            {
                RowNumber = 0,
                Status = ImportRowStatus.Failed,
                ErrorMessage = "The file contains no data rows.",
                FieldErrors = [new ImportFieldErrorDto { ColumnName = "File", Message = "The file contains no data rows." }]
            });
            overallResult.FailedCount = 1;
            return overallResult;
        }

        overallResult.TotalRows = parseResult.Rows.Count;

        // ── Step 2: Field-level + within-file validation ──────────────────────
        var validationResults = _importService.ValidateRows(parseResult.Rows);

        // ── Step 3: Batch DB duplicate check ──────────────────────────────────
        var emailsToCheck = parseResult.Rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Email))
            .Select(r => r.Email!.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        var empIdsToCheck = parseResult.Rows
            .Where(r => !string.IsNullOrWhiteSpace(r.EmployeeId))
            .Select(r => r.EmployeeId!.Trim())
            .Distinct()
            .ToList();

        var duplicates = await _importService.CheckDuplicatesAsync(
            emailsToCheck, empIdsToCheck, cancellationToken);

        // Apply DB duplicate errors to the matching validation results
        foreach (var vr in validationResults)
        {
            var email = vr.Email?.Trim().ToLowerInvariant();
            if (email != null && duplicates.DuplicateEmails.Contains(email))
            {
                vr.FieldErrors.Add(new ImportFieldErrorDto
                {
                    ColumnName = "Email",
                    Message = $"The email \"{email}\" already exists in the system."
                });
            }

            var row = parseResult.Rows.FirstOrDefault(r => r.RowNumber == vr.RowNumber);
            if (row != null && !string.IsNullOrWhiteSpace(row.EmployeeId))
            {
                var empId = row.EmployeeId.Trim();
                if (duplicates.DuplicateEmployeeIds.Contains(empId))
                {
                    vr.FieldErrors.Add(new ImportFieldErrorDto
                    {
                        ColumnName = "EmployeeId",
                        Message = $"The employee ID \"{empId}\" already exists in the system."
                    });
                }
            }

            // Recalculate status after DB checks
            if (vr.FieldErrors.Count > 0)
            {
                vr.Status = ImportRowStatus.Skipped;
                vr.ErrorMessage = vr.FieldErrors[0].Message;
            }
        }

        var invalidCount = validationResults.Count(r => r.Status == ImportRowStatus.Skipped);

        // ── Step 4: If not skipping invalid rows and there are errors, stop ───
        if (!request.SkipInvalidRows && invalidCount > 0)
        {
            // Return the full validation report so the UI can show all errors
            overallResult.Results = validationResults;
            overallResult.SkippedCount = invalidCount;
            overallResult.FailedCount = 0;
            overallResult.CreatedCount = 0;
            return overallResult;
        }

        // ── Step 5: Dry run — return results without creating anything ────────
        if (request.DryRun)
        {
            overallResult.Results = validationResults;
            overallResult.SkippedCount = invalidCount;
            overallResult.CreatedCount = 0;
            overallResult.FailedCount = 0;
            return overallResult;
        }

        // ── Step 6: Create users row by row ───────────────────────────────────
        var validRows = parseResult.Rows
            .Where(r => validationResults
                .Any(vr => vr.RowNumber == r.RowNumber && vr.Status != ImportRowStatus.Skipped))
            .ToList();

        int created = 0, failed = 0, skipped = invalidCount;

        foreach (var row in validRows)
        {
            var vr = validationResults.First(r => r.RowNumber == row.RowNumber);

            if (vr.Status == ImportRowStatus.Skipped)
            {
                skipped++;
                continue;
            }

            try
            {
                if (!Enum.TryParse<UserRole>(row.Role?.Trim(), ignoreCase: true, out var role))
                {
                    vr.Status = ImportRowStatus.Failed;
                    vr.ErrorMessage = $"Could not parse role value: \"{row.Role}\".";
                    failed++;
                    continue;
                }

                var command = BuildCreateUserCommand(row, role, request.ImportedBy);
                var userDto = await _mediator.Send(command, cancellationToken);

                vr.Status = ImportRowStatus.Created;
                vr.CreatedUserId = userDto.Id;
                vr.ErrorMessage = null;
                created++;

                _logger.LogDebug("Import row {Row}: created user {UserId} ({Email})",
                    row.RowNumber, userDto.Id, userDto.Email);
            }
            catch (InvalidOperationException ex)
            {
                // Business rule violation (e.g. race-condition duplicate) — treat as failed row
                vr.Status = ImportRowStatus.Failed;
                vr.ErrorMessage = ex.Message;
                vr.FieldErrors.Add(new ImportFieldErrorDto
                {
                    ColumnName = "General",
                    Message = ex.Message
                });
                failed++;
                _logger.LogWarning(ex, "Import row {Row} ({Email}) failed: {Message}",
                    row.RowNumber, row.Email, ex.Message);
            }
            catch (Exception ex)
            {
                vr.Status = ImportRowStatus.Failed;
                vr.ErrorMessage = "An unexpected error occurred while creating this user.";
                vr.FieldErrors.Add(new ImportFieldErrorDto
                {
                    ColumnName = "General",
                    Message = vr.ErrorMessage
                });
                failed++;
                _logger.LogError(ex, "Unexpected error creating import row {Row} ({Email})",
                    row.RowNumber, row.Email);
            }
        }

        overallResult.Results = validationResults;
        overallResult.CreatedCount = created;
        overallResult.FailedCount = failed;
        overallResult.SkippedCount = skipped;

        _logger.LogInformation(
            "User import complete. Total={Total}, Created={Created}, Skipped={Skipped}, Failed={Failed}",
            overallResult.TotalRows, created, skipped, failed);

        return overallResult;
    }

    private static CreateUserCommand BuildCreateUserCommand(
        ImportUserRowDto row, UserRole role, int importedBy)
    {
        return new CreateUserCommand
        {
            FirstName = row.FirstName!.Trim(),
            LastName  = row.LastName!.Trim(),
            Email     = row.Email!.Trim().ToLowerInvariant(),
            RoleName  = role.ToString(),

            PhoneNumber     = row.PhoneNumber?.Trim(),
            PhoneCountryCode = row.PhoneCountryCode?.Trim(),
            PhoneType       = string.IsNullOrWhiteSpace(row.PhoneType) ? "Mobile" : row.PhoneType.Trim(),

            EmployeeId  = string.IsNullOrWhiteSpace(row.EmployeeId) ? null : row.EmployeeId.Trim(),
            Department  = string.IsNullOrWhiteSpace(row.Department) ? null : row.Department.Trim(),
            JobTitle    = string.IsNullOrWhiteSpace(row.JobTitle)   ? null : row.JobTitle.Trim(),

            TimeZone = string.IsNullOrWhiteSpace(row.TimeZone) ? "Asia/Beirut" : row.TimeZone.Trim(),
            Language = string.IsNullOrWhiteSpace(row.Language) ? "en-US"       : row.Language.Trim(),
            Theme    = "light",

            AddressType = string.IsNullOrWhiteSpace(row.AddressType) ? "Home" : row.AddressType.Trim(),
            Street1     = string.IsNullOrWhiteSpace(row.Street1)     ? null   : row.Street1.Trim(),
            Street2     = string.IsNullOrWhiteSpace(row.Street2)     ? null   : row.Street2.Trim(),
            City        = string.IsNullOrWhiteSpace(row.City)        ? null   : row.City.Trim(),
            State       = string.IsNullOrWhiteSpace(row.Governorate) ? null   : row.Governorate.Trim(),
            PostalCode  = string.IsNullOrWhiteSpace(row.PostalCode)  ? null   : row.PostalCode.Trim(),
            Country     = string.IsNullOrWhiteSpace(row.Country)     ? "Lebanon" : row.Country.Trim(),

            MustChangePassword = UserImportService.ParseBool(row.MustChangePassword, defaultValue: true),
            SendWelcomeEmail   = UserImportService.ParseBool(row.SendWelcomeEmail,   defaultValue: true),

            RequiresApprovalOverride = UserImportService.ParseApprovalOverride(row.ApprovalOverride),

            CreatedBy = importedBy
        };
    }
}
