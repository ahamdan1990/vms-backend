// Application/Services/Users/UserImportService.cs
using System.Net.Mail;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using OfficeOpenXml.DataValidation;
using OfficeOpenXml.Style;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Application.Services.Users;

/// <summary>
/// Implements file parsing, row validation, duplicate detection, and template generation
/// for the user bulk-import feature.
/// </summary>
public class UserImportService : IUserImportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserImportService> _logger;

    // ── File limits ───────────────────────────────────────────────────────────
    private const int MaxRows = 500;
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    // ── Allowed enum values (case-insensitive matching in validator) ───────────
    private static readonly HashSet<string> AllowedRoles =
        new(StringComparer.OrdinalIgnoreCase) { "Staff", "Receptionist", "Administrator" };

    private static readonly HashSet<string> AllowedStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "Active", "Inactive", "Suspended" };

    private static readonly HashSet<string> AllowedPhoneTypes =
        new(StringComparer.OrdinalIgnoreCase) { "Mobile", "Landline", "Unknown" };

    private static readonly HashSet<string> AllowedAddressTypes =
        new(StringComparer.OrdinalIgnoreCase) { "Home", "Work", "Billing", "Shipping", "Other" };

    private static readonly HashSet<string> AllowedGovernorates =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Beirut", "Mount Lebanon", "North Lebanon", "South Lebanon",
            "Beqaa", "Akkar", "Baalbek-Hermel", "Nabatieh"
        };

    private static readonly HashSet<string> AllowedApprovalOverrides =
        new(StringComparer.OrdinalIgnoreCase) { "FollowGlobal", "AlwaysRequire", "AlwaysAutoApprove" };

    // ── Required column headers (case-insensitive) ────────────────────────────
    private static readonly HashSet<string> RequiredColumns =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "FirstName", "LastName", "Email", "Role", "Status"
        };

    // ── All recognized column headers → property names ────────────────────────
    private static readonly Dictionary<string, string> ColumnMapping =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["FirstName"]           = nameof(ImportUserRowDto.FirstName),
            ["LastName"]            = nameof(ImportUserRowDto.LastName),
            ["Email"]               = nameof(ImportUserRowDto.Email),
            ["Role"]                = nameof(ImportUserRowDto.Role),
            ["Status"]              = nameof(ImportUserRowDto.Status),
            ["PhoneCountryCode"]    = nameof(ImportUserRowDto.PhoneCountryCode),
            ["PhoneNumber"]         = nameof(ImportUserRowDto.PhoneNumber),
            ["PhoneType"]           = nameof(ImportUserRowDto.PhoneType),
            ["EmployeeId"]          = nameof(ImportUserRowDto.EmployeeId),
            ["Department"]          = nameof(ImportUserRowDto.Department),
            ["JobTitle"]            = nameof(ImportUserRowDto.JobTitle),
            ["TimeZone"]            = nameof(ImportUserRowDto.TimeZone),
            ["Language"]            = nameof(ImportUserRowDto.Language),
            ["ApprovalOverride"]    = nameof(ImportUserRowDto.ApprovalOverride),
            ["AddressType"]         = nameof(ImportUserRowDto.AddressType),
            ["Street1"]             = nameof(ImportUserRowDto.Street1),
            ["Street2"]             = nameof(ImportUserRowDto.Street2),
            ["City"]                = nameof(ImportUserRowDto.City),
            ["Governorate"]         = nameof(ImportUserRowDto.Governorate),
            ["PostalCode"]          = nameof(ImportUserRowDto.PostalCode),
            ["Country"]             = nameof(ImportUserRowDto.Country),
            ["MustChangePassword"]  = nameof(ImportUserRowDto.MustChangePassword),
            ["SendWelcomeEmail"]    = nameof(ImportUserRowDto.SendWelcomeEmail),
        };

    public UserImportService(IUnitOfWork unitOfWork, ILogger<UserImportService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    // ── Parse ─────────────────────────────────────────────────────────────────

    public async Task<UserImportParseResult> ParseFileAsync(
        IFormFile file, CancellationToken cancellationToken = default)
    {
        var result = new UserImportParseResult();

        // File-level guards
        if (file == null || file.Length == 0)
        {
            result.FileErrors.Add("No file was uploaded or the file is empty.");
            return result;
        }

        if (file.Length > MaxFileSizeBytes)
        {
            result.FileErrors.Add($"File size ({file.Length / 1024 / 1024:F1} MB) exceeds the maximum allowed size of 5 MB.");
            return result;
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not ".xlsx" and not ".csv")
        {
            result.FileErrors.Add("Only .xlsx and .csv files are supported.");
            return result;
        }

        result.DetectedFormat = ext.TrimStart('.');

        try
        {
            using var stream = file.OpenReadStream();
            result.Rows = ext == ".xlsx"
                ? await ParseXlsxAsync(stream, result.FileErrors, cancellationToken)
                : await ParseCsvAsync(stream, result.FileErrors, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while parsing import file {FileName}", file.FileName);
            result.FileErrors.Add($"The file could not be read. Ensure it is a valid {ext.ToUpper()} file and is not password-protected or corrupted.");
        }

        result.Success = result.FileErrors.Count == 0;
        return result;
    }

    private Task<List<ImportUserRowDto>> ParseXlsxAsync(
        Stream stream, List<string> fileErrors, CancellationToken _)
    {
        var rows = new List<ImportUserRowDto>();

        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
        {
            fileErrors.Add("The Excel file contains no worksheets.");
            return Task.FromResult(rows);
        }

        if (worksheet.Dimension == null)
        {
            fileErrors.Add("The worksheet is empty.");
            return Task.FromResult(rows);
        }

        var totalRows = worksheet.Dimension.Rows;
        var totalCols = worksheet.Dimension.Columns;

        // Row 1 is the header
        var headerMap = new Dictionary<int, string>(); // colIndex → property name
        var missingRequired = new List<string>();

        for (int col = 1; col <= totalCols; col++)
        {
            var header = GetCellText(worksheet.Cells[1, col]);
            if (string.IsNullOrEmpty(header)) continue;

            if (ColumnMapping.TryGetValue(header, out var propName))
                headerMap[col] = propName;
            // Unknown columns are silently ignored
        }

        // Check required columns
        foreach (var req in RequiredColumns)
            if (!ColumnMapping.TryGetValue(req, out var prop) || !headerMap.ContainsValue(prop))
                missingRequired.Add(req);

        if (missingRequired.Count > 0)
        {
            fileErrors.Add($"The following required columns are missing: {string.Join(", ", missingRequired)}. " +
                           "Please download the template and ensure all required column headers are present.");
            return Task.FromResult(rows);
        }

        if (totalRows > MaxRows + 1)
        {
            fileErrors.Add($"The file contains {totalRows - 1} data rows, which exceeds the maximum of {MaxRows} rows per import. Split the data into multiple files.");
            return Task.FromResult(rows);
        }

        // Parse data rows (row 2 onward)
        for (int row = 2; row <= totalRows; row++)
        {
            // Skip entirely blank rows
            var hasAnyValue = false;
            for (int col = 1; col <= totalCols; col++)
            {
                if (!string.IsNullOrWhiteSpace(GetCellText(worksheet.Cells[row, col])))
                { hasAnyValue = true; break; }
            }
            if (!hasAnyValue) continue;

            var dto = new ImportUserRowDto { RowNumber = row - 1 }; // 1-based data row number

            foreach (var (col, propName) in headerMap)
            {
                var cellValue = GetCellText(worksheet.Cells[row, col]);
                SetDtoProp(dto, propName, cellValue);
            }

            rows.Add(dto);
        }

        return Task.FromResult(rows);
    }

    private async Task<List<ImportUserRowDto>> ParseCsvAsync(
        Stream stream, List<string> fileErrors, CancellationToken cancellationToken)
    {
        var rows = new List<ImportUserRowDto>();

        // Detect and skip UTF-8 BOM automatically
        var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);

        var config = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,    // Silently ignore missing optional columns
            BadDataFound = null          // Don't throw on bad data — we'll catch it in validation
        };

        using var csv = new CsvReader(reader, config);

        // Read header
        await csv.ReadAsync();
        csv.ReadHeader();

        var headers = csv.HeaderRecord ?? [];

        // Check required columns
        var missingRequired = RequiredColumns
            .Where(req => !headers.Any(h => string.Equals(h?.Trim(), req, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (missingRequired.Count > 0)
        {
            fileErrors.Add($"The following required columns are missing: {string.Join(", ", missingRequired)}.");
            return rows;
        }

        int dataRowNumber = 0;

        while (await csv.ReadAsync())
        {
            dataRowNumber++;

            if (dataRowNumber > MaxRows)
            {
                fileErrors.Add($"The file contains more than {MaxRows} data rows. Split the data into multiple files.");
                break;
            }

            // Skip blank rows
            var allBlank = headers.All(h =>
                string.IsNullOrWhiteSpace(csv.GetField<string>(h) ?? string.Empty));
            if (allBlank) continue;

            var dto = new ImportUserRowDto { RowNumber = dataRowNumber };

            foreach (var header in headers)
            {
                if (string.IsNullOrWhiteSpace(header)) continue;
                if (!ColumnMapping.TryGetValue(header.Trim(), out var propName)) continue;

                var value = csv.GetField<string>(header)?.Trim();
                SetDtoProp(dto, propName, value);
            }

            rows.Add(dto);
        }

        return rows;
    }

    // ── Validate rows ─────────────────────────────────────────────────────────

    public List<ImportUserRowResultDto> ValidateRows(List<ImportUserRowDto> rows)
    {
        var results = new List<ImportUserRowResultDto>();

        // Track within-file uniqueness
        var seenEmails = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var seenEmployeeIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var result = new ImportUserRowResultDto
            {
                RowNumber = row.RowNumber,
                Email = row.Email?.Trim().ToLowerInvariant(),
                FirstName = row.FirstName?.Trim(),
                LastName = row.LastName?.Trim(),
                Role = row.Role?.Trim()
            };

            ValidateRow(row, result, seenEmails, seenEmployeeIds);

            result.Status = result.FieldErrors.Count == 0
                ? ImportRowStatus.Created  // Tentatively — actual status set after DB operation
                : ImportRowStatus.Skipped;

            if (result.FieldErrors.Count > 0)
                result.ErrorMessage = result.FieldErrors[0].Message;

            results.Add(result);
        }

        return results;
    }

    private static void ValidateRow(
        ImportUserRowDto row,
        ImportUserRowResultDto result,
        Dictionary<string, int> seenEmails,
        Dictionary<string, int> seenEmployeeIds)
    {
        // ── FirstName ────────────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(row.FirstName))
            AddError(result, "FirstName", "First name is required.");
        else if (row.FirstName.Trim().Length > 50)
            AddError(result, "FirstName", "First name cannot exceed 50 characters.");

        // ── LastName ─────────────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(row.LastName))
            AddError(result, "LastName", "Last name is required.");
        else if (row.LastName.Trim().Length > 50)
            AddError(result, "LastName", "Last name cannot exceed 50 characters.");

        // ── Email ────────────────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(row.Email))
        {
            AddError(result, "Email", "Email is required.");
        }
        else
        {
            var email = row.Email.Trim().ToLowerInvariant();

            if (!IsValidEmail(email))
                AddError(result, "Email", $"\"{row.Email}\" is not a valid email address.");
            else if (email.Length > 256)
                AddError(result, "Email", "Email cannot exceed 256 characters.");
            else if (seenEmails.TryGetValue(email, out var firstRow))
                AddError(result, "Email", $"Duplicate email in this file (first seen in row {firstRow}).");
            else
                seenEmails[email] = row.RowNumber;
        }

        // ── Role ─────────────────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(row.Role))
            AddError(result, "Role", "Role is required.");
        else if (!AllowedRoles.Contains(row.Role.Trim()))
            AddError(result, "Role", $"\"{row.Role}\" is not a valid role. Allowed values: Staff, Receptionist, Administrator.");

        // ── Status ───────────────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(row.Status))
            AddError(result, "Status", "Status is required.");
        else if (!AllowedStatuses.Contains(row.Status.Trim()))
            AddError(result, "Status", $"\"{row.Status}\" is not a valid status. Allowed values: Active, Inactive, Suspended.");

        // ── Phone (cross-field: both must be provided or neither) ─────────────
        var hasPhone = !string.IsNullOrWhiteSpace(row.PhoneNumber);
        var hasCode = !string.IsNullOrWhiteSpace(row.PhoneCountryCode);

        if (hasPhone && !hasCode)
            AddError(result, "PhoneCountryCode", "PhoneCountryCode is required when PhoneNumber is provided.");
        else if (!hasPhone && hasCode)
            AddError(result, "PhoneNumber", "PhoneNumber is required when PhoneCountryCode is provided.");
        else if (hasPhone && hasCode)
        {
            // Validate country code is numeric
            if (!row.PhoneCountryCode!.Trim().All(char.IsDigit))
                AddError(result, "PhoneCountryCode", "PhoneCountryCode must contain digits only (e.g. 961, 1, 44).");

            // Validate phone digits only
            if (!row.PhoneNumber!.Trim().All(char.IsDigit))
                AddError(result, "PhoneNumber", "PhoneNumber must contain digits only, without dashes or spaces.");
            else if (row.PhoneNumber.Trim().Length < 4 || row.PhoneNumber.Trim().Length > 15)
                AddError(result, "PhoneNumber", "PhoneNumber must be between 4 and 15 digits.");
        }

        // ── PhoneType ─────────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(row.PhoneType) && !AllowedPhoneTypes.Contains(row.PhoneType.Trim()))
            AddError(result, "PhoneType", $"\"{row.PhoneType}\" is not a valid phone type. Allowed values: Mobile, Landline, Unknown.");

        // ── EmployeeId ────────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(row.EmployeeId))
        {
            var empId = row.EmployeeId.Trim();
            if (empId.Length > 50)
                AddError(result, "EmployeeId", "Employee ID cannot exceed 50 characters.");
            else if (seenEmployeeIds.TryGetValue(empId, out var firstEmpRow))
                AddError(result, "EmployeeId", $"Duplicate Employee ID in this file (first seen in row {firstEmpRow}).");
            else
                seenEmployeeIds[empId] = row.RowNumber;
        }

        // ── Department ────────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(row.Department) && row.Department.Trim().Length > 100)
            AddError(result, "Department", "Department cannot exceed 100 characters.");

        // ── JobTitle ──────────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(row.JobTitle) && row.JobTitle.Trim().Length > 100)
            AddError(result, "JobTitle", "Job title cannot exceed 100 characters.");

        // ── AddressType ───────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(row.AddressType) && !AllowedAddressTypes.Contains(row.AddressType.Trim()))
            AddError(result, "AddressType", $"\"{row.AddressType}\" is not a valid address type. Allowed values: Home, Work, Billing, Shipping, Other.");

        // ── Governorate ───────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(row.Governorate) && !AllowedGovernorates.Contains(row.Governorate.Trim()))
            AddError(result, "Governorate", $"\"{row.Governorate}\" is not a valid governorate.");

        // ── Street1 ───────────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(row.Street1) && row.Street1.Trim().Length > 100)
            AddError(result, "Street1", "Street address (line 1) cannot exceed 100 characters.");

        // ── Street2 ───────────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(row.Street2) && row.Street2.Trim().Length > 100)
            AddError(result, "Street2", "Street address (line 2) cannot exceed 100 characters.");

        // ── City ──────────────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(row.City) && row.City.Trim().Length > 50)
            AddError(result, "City", "City cannot exceed 50 characters.");

        // ── PostalCode ────────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(row.PostalCode) && row.PostalCode.Trim().Length > 20)
            AddError(result, "PostalCode", "Postal code cannot exceed 20 characters.");

        // ── Country ───────────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(row.Country) && row.Country.Trim().Length > 50)
            AddError(result, "Country", "Country cannot exceed 50 characters.");

        // ── MustChangePassword ────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(row.MustChangePassword) &&
            !string.Equals(row.MustChangePassword.Trim(), "TRUE", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(row.MustChangePassword.Trim(), "FALSE", StringComparison.OrdinalIgnoreCase))
            AddError(result, "MustChangePassword", "MustChangePassword must be TRUE or FALSE (case-insensitive).");

        // ── SendWelcomeEmail ──────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(row.SendWelcomeEmail) &&
            !string.Equals(row.SendWelcomeEmail.Trim(), "TRUE", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(row.SendWelcomeEmail.Trim(), "FALSE", StringComparison.OrdinalIgnoreCase))
            AddError(result, "SendWelcomeEmail", "SendWelcomeEmail must be TRUE or FALSE (case-insensitive).");

        // ── ApprovalOverride ──────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(row.ApprovalOverride) &&
            !AllowedApprovalOverrides.Contains(row.ApprovalOverride.Trim()))
            AddError(result, "ApprovalOverride",
                $"\"{row.ApprovalOverride}\" is not valid. Allowed values: FollowGlobal, AlwaysRequire, AlwaysAutoApprove.");
    }

    // ── Duplicate check ───────────────────────────────────────────────────────

    public async Task<CheckDuplicatesResultDto> CheckDuplicatesAsync(
        List<string> emails,
        List<string> employeeIds,
        CancellationToken cancellationToken = default)
    {
        var result = new CheckDuplicatesResultDto();

        // Batch-check emails with a single IN query via the existing repository method
        var normalizedEmails = emails
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        foreach (var email in normalizedEmails)
        {
            if (await _unitOfWork.Users.EmailExistsAsync(email, cancellationToken: cancellationToken))
                result.DuplicateEmails.Add(email);
        }

        // Batch-check employeeIds
        var normalizedEmpIds = employeeIds
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim())
            .Distinct()
            .ToList();

        foreach (var empId in normalizedEmpIds)
        {
            if (await _unitOfWork.Users.EmployeeIdExistsAsync(empId, cancellationToken: cancellationToken))
                result.DuplicateEmployeeIds.Add(empId);
        }

        return result;
    }

    // ── Template generation ───────────────────────────────────────────────────

    public Task<byte[]> GenerateImportTemplateAsync(CancellationToken cancellationToken = default)
    {
        using var package = new ExcelPackage();

        CreateDataSheet(package);
        CreateInstructionsSheet(package);
        CreateAllowedValuesSheet(package);

        // Activate the Data sheet
        package.Workbook.Worksheets["Users"].Select();

        return Task.FromResult(package.GetAsByteArray());
    }

    public Task<byte[]> GenerateCsvImportTemplateAsync(CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();

        // Header row
        sb.AppendLine("FirstName,LastName,Email,Role,Status,ApprovalOverride,PhoneCountryCode,PhoneNumber,PhoneType,EmployeeId,Department,JobTitle,TimeZone,Language,AddressType,Street1,Street2,City,Governorate,PostalCode,Country,MustChangePassword,SendWelcomeEmail");

        // Example row
        sb.AppendLine("Jane,Smith,jane.smith@company.com,Staff,Active,FollowGlobal,961,70123456,Mobile,EMP002,HR,HR Manager,Asia/Beirut,en-US,Home,Mar Elias St,,,Beirut,,Lebanon,TRUE,TRUE");

        return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    // ── Excel template builder ────────────────────────────────────────────────

    private static void CreateDataSheet(ExcelPackage package)
    {
        var ws = package.Workbook.Worksheets.Add("Users");

        // Define columns in order
        var columns = new[]
        {
            ("FirstName", "First Name*", true),
            ("LastName", "Last Name*", true),
            ("Email", "Email*", true),
            ("Role", "Role*", true),
            ("Status", "Status*", true),
            ("ApprovalOverride", "Approval Override", false),
            ("PhoneCountryCode", "Phone Country Code", false),
            ("PhoneNumber", "Phone Number", false),
            ("PhoneType", "Phone Type", false),
            ("EmployeeId", "Employee ID", false),
            ("Department", "Department", false),
            ("JobTitle", "Job Title", false),
            ("TimeZone", "Time Zone", false),
            ("Language", "Language", false),
            ("AddressType", "Address Type", false),
            ("Street1", "Street (Line 1)", false),
            ("Street2", "Street (Line 2)", false),
            ("City", "City", false),
            ("Governorate", "Governorate", false),
            ("PostalCode", "Postal Code", false),
            ("Country", "Country", false),
            ("MustChangePassword", "Must Change Password", false),
            ("SendWelcomeEmail", "Send Welcome Email", false),
        };

        // Header row styling
        using (var header = ws.Cells[1, 1, 1, columns.Length])
        {
            header.Style.Font.Bold = true;
            header.Style.Fill.PatternType = ExcelFillStyle.Solid;
            header.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(31, 73, 125));
            header.Style.Font.Color.SetColor(System.Drawing.Color.White);
            header.Style.Border.Bottom.Style = ExcelBorderStyle.Medium;
        }

        for (int i = 0; i < columns.Length; i++)
        {
            var (key, label, required) = columns[i];
            int col = i + 1;

            ws.Cells[1, col].Value = key; // Machine-readable header (what the parser reads)
            ws.Cells[1, col].AddComment(label + (required ? " (Required)" : " (Optional)"), "VMS");

            // Required columns: light red background in header
            if (required)
            {
                ws.Cells[1, col].Style.Fill.BackgroundColor.SetColor(
                    System.Drawing.Color.FromArgb(192, 0, 0));
            }

            ws.Column(col).Width = 20;
        }

        // Example row (row 2) - light yellow background
        using (var example = ws.Cells[2, 1, 2, columns.Length])
        {
            example.Style.Fill.PatternType = ExcelFillStyle.Solid;
            example.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 255, 204));
            example.Style.Font.Italic = true;
        }

        ws.Cells[2, 1].Value = "John";
        ws.Cells[2, 2].Value = "Doe";
        ws.Cells[2, 3].Value = "john.doe@company.com";
        ws.Cells[2, 4].Value = "Staff";
        ws.Cells[2, 5].Value = "Active";
        ws.Cells[2, 6].Value = "FollowGlobal";
        ws.Cells[2, 7].Value = "961";
        ws.Cells[2, 8].Value = "70123456";
        ws.Cells[2, 9].Value = "Mobile";
        ws.Cells[2, 10].Value = "EMP001";
        ws.Cells[2, 11].Value = "Engineering";
        ws.Cells[2, 12].Value = "Software Engineer";
        ws.Cells[2, 13].Value = "Asia/Beirut";
        ws.Cells[2, 14].Value = "en-US";
        ws.Cells[2, 15].Value = "Home";
        ws.Cells[2, 16].Value = "Hamra Street";
        ws.Cells[2, 17].Value = "";
        ws.Cells[2, 18].Value = "Beirut";
        ws.Cells[2, 19].Value = "Beirut";
        ws.Cells[2, 20].Value = "1100";
        ws.Cells[2, 21].Value = "Lebanon";
        ws.Cells[2, 22].Value = "TRUE";
        ws.Cells[2, 23].Value = "TRUE";

        // Freeze header row
        ws.View.FreezePanes(2, 1);

        // Column-level dropdowns for enum columns (rows 2–502)
        AddListValidation(ws, 4, 2, 501, "Role",
            "\"Staff,Receptionist,Administrator\"");
        AddListValidation(ws, 5, 2, 501, "Status",
            "\"Active,Inactive,Suspended\"");
        AddListValidation(ws, 6, 2, 501, "ApprovalOverride",
            "\"FollowGlobal,AlwaysRequire,AlwaysAutoApprove\"");
        AddListValidation(ws, 9, 2, 501, "PhoneType",
            "\"Mobile,Landline,Unknown\"");
        AddListValidation(ws, 15, 2, 501, "AddressType",
            "\"Home,Work,Billing,Shipping,Other\"");
        AddListValidation(ws, 19, 2, 501, "Governorate",
            "\"Beirut,Mount Lebanon,North Lebanon,South Lebanon,Beqaa,Akkar,Baalbek-Hermel,Nabatieh\"");
        AddListValidation(ws, 22, 2, 501, "MustChangePassword", "\"TRUE,FALSE\"");
        AddListValidation(ws, 23, 2, 501, "SendWelcomeEmail", "\"TRUE,FALSE\"");
    }

    private static void AddListValidation(
        ExcelWorksheet ws, int col, int fromRow, int toRow,
        string title, string formula)
    {
        var validation = ws.DataValidations.AddListValidation(
            ws.Cells[fromRow, col, toRow, col].Address);
        validation.ShowErrorMessage = true;
        validation.ErrorTitle = "Invalid value";
        validation.Error = $"Please select a valid {title} from the list.";
        validation.Formula.ExcelFormula = formula;
    }

    private static void CreateInstructionsSheet(ExcelPackage package)
    {
        var ws = package.Workbook.Worksheets.Add("Instructions");
        ws.Column(1).Width = 25;
        ws.Column(2).Width = 60;

        // Title
        ws.Cells["A1"].Value = "VMS User Import — Instructions";
        ws.Cells["A1"].Style.Font.Bold = true;
        ws.Cells["A1"].Style.Font.Size = 14;
        ws.Cells["A1:B1"].Merge = true;

        var instructions = new[]
        {
            ("", ""),
            ("HOW TO USE", ""),
            ("1.", "Fill in user data starting from row 3 on the \"Users\" sheet."),
            ("2.", "Row 2 (yellow) is an example — you may delete it before uploading."),
            ("3.", "Required columns are marked with * in their tooltip."),
            ("4.", "Use the dropdown lists provided in each column where applicable."),
            ("5.", "Save the file and upload it on the User Management page."),
            ("", ""),
            ("REQUIRED COLUMNS", ""),
            ("FirstName", "User's first name. Max 50 characters."),
            ("LastName", "User's last name. Max 50 characters."),
            ("Email", "Valid email address. Must be unique in the system. Max 256 characters."),
            ("Role", "Must be one of: Staff, Receptionist, Administrator."),
            ("Status", "Must be one of: Active, Inactive, Suspended."),
            ("", ""),
            ("OPTIONAL COLUMNS", ""),
            ("ApprovalOverride", "Controls invitation approval for this user. FollowGlobal (default), AlwaysRequire, AlwaysAutoApprove."),
            ("PhoneCountryCode", "Numeric digits only (e.g. 961 for Lebanon, 1 for USA). Required if PhoneNumber is provided."),
            ("PhoneNumber", "Digits only, no dashes or spaces. Required if PhoneCountryCode is provided."),
            ("PhoneType", "Mobile (default), Landline, or Unknown."),
            ("EmployeeId", "Internal employee ID. Must be unique if provided. Max 50 characters."),
            ("Department", "Department name (free text). Max 100 characters."),
            ("JobTitle", "Job title. Max 100 characters."),
            ("TimeZone", "IANA timezone string, e.g. Asia/Beirut. Default: Asia/Beirut."),
            ("Language", "BCP-47 language tag, e.g. en-US, ar-LB. Default: en-US."),
            ("AddressType", "Home (default), Work, Billing, Shipping, or Other."),
            ("Street1", "Street address line 1. Max 100 characters."),
            ("Street2", "Street address line 2. Max 100 characters."),
            ("City", "City. Max 50 characters."),
            ("Governorate", "Lebanese governorate — see allowed values on the Allowed Values sheet."),
            ("PostalCode", "Postal code. Max 20 characters."),
            ("Country", "Country name. Default: Lebanon. Max 50 characters."),
            ("MustChangePassword", "TRUE or FALSE. Default: TRUE. When TRUE, user must set a new password on first login."),
            ("SendWelcomeEmail", "TRUE or FALSE. Default: TRUE. When TRUE, a welcome email with login instructions is sent."),
        };

        for (int i = 0; i < instructions.Length; i++)
        {
            var (col1, col2) = instructions[i];
            ws.Cells[i + 1, 1].Value = col1;
            ws.Cells[i + 1, 2].Value = col2;

            if (col2 == "" && !string.IsNullOrEmpty(col1))
            {
                ws.Cells[i + 1, 1].Style.Font.Bold = true;
                ws.Cells[i + 1, 1].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(31, 73, 125));
            }
        }

        ws.View.FreezePanes(1, 1);
    }

    private static void CreateAllowedValuesSheet(ExcelPackage package)
    {
        var ws = package.Workbook.Worksheets.Add("Allowed Values");
        ws.Column(1).Width = 25;
        ws.Column(2).Width = 50;

        ws.Cells["A1"].Value = "Column";
        ws.Cells["B1"].Value = "Allowed Values";
        ws.Cells["A1:B1"].Style.Font.Bold = true;

        var data = new[]
        {
            ("Role", "Staff, Receptionist, Administrator"),
            ("Status", "Active, Inactive, Suspended"),
            ("ApprovalOverride", "FollowGlobal, AlwaysRequire, AlwaysAutoApprove"),
            ("PhoneType", "Mobile, Landline, Unknown"),
            ("AddressType", "Home, Work, Billing, Shipping, Other"),
            ("Governorate", "Beirut, Mount Lebanon, North Lebanon, South Lebanon, Beqaa, Akkar, Baalbek-Hermel, Nabatieh"),
            ("MustChangePassword", "TRUE, FALSE"),
            ("SendWelcomeEmail", "TRUE, FALSE"),
        };

        for (int i = 0; i < data.Length; i++)
        {
            ws.Cells[i + 2, 1].Value = data[i].Item1;
            ws.Cells[i + 2, 2].Value = data[i].Item2;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads a cell value as a normalised string regardless of how EPPlus typed it.
    /// EPPlus 7 reads Excel boolean TRUE/FALSE as C# <c>bool</c>, whose <c>.Text</c>
    /// property returns "True"/"False" (mixed-case).  We normalise those to "TRUE"/"FALSE"
    /// so the downstream validator's case-insensitive checks work correctly.
    /// Numeric values are converted via InvariantCulture to avoid locale issues.
    /// </summary>
    private static string? GetCellText(ExcelRange cell)
    {
        var value = cell.Value;

        return value switch
        {
            null        => null,
            bool b      => b ? "TRUE" : "FALSE",          // normalise Excel booleans
            double d    => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
            int i       => i.ToString(System.Globalization.CultureInfo.InvariantCulture),
            long l      => l.ToString(System.Globalization.CultureInfo.InvariantCulture),
            _           => cell.Text?.Trim()               // string, date-as-text, etc.
        };
    }

    private static void AddError(ImportUserRowResultDto result, string column, string message)
    {
        result.FieldErrors.Add(new ImportFieldErrorDto { ColumnName = column, Message = message });
    }

    private static bool IsValidEmail(string email)
    {
        try { _ = new MailAddress(email); return true; }
        catch { return false; }
    }

    private static void SetDtoProp(ImportUserRowDto dto, string propName, string? value)
    {
        if (string.IsNullOrEmpty(value)) return;

        switch (propName)
        {
            case nameof(ImportUserRowDto.FirstName):          dto.FirstName = value; break;
            case nameof(ImportUserRowDto.LastName):           dto.LastName = value; break;
            case nameof(ImportUserRowDto.Email):              dto.Email = value; break;
            case nameof(ImportUserRowDto.Role):               dto.Role = value; break;
            case nameof(ImportUserRowDto.Status):             dto.Status = value; break;
            case nameof(ImportUserRowDto.PhoneCountryCode):   dto.PhoneCountryCode = value; break;
            case nameof(ImportUserRowDto.PhoneNumber):        dto.PhoneNumber = value; break;
            case nameof(ImportUserRowDto.PhoneType):          dto.PhoneType = value; break;
            case nameof(ImportUserRowDto.EmployeeId):         dto.EmployeeId = value; break;
            case nameof(ImportUserRowDto.Department):         dto.Department = value; break;
            case nameof(ImportUserRowDto.JobTitle):           dto.JobTitle = value; break;
            case nameof(ImportUserRowDto.TimeZone):           dto.TimeZone = value; break;
            case nameof(ImportUserRowDto.Language):           dto.Language = value; break;
            case nameof(ImportUserRowDto.ApprovalOverride):   dto.ApprovalOverride = value; break;
            case nameof(ImportUserRowDto.AddressType):        dto.AddressType = value; break;
            case nameof(ImportUserRowDto.Street1):            dto.Street1 = value; break;
            case nameof(ImportUserRowDto.Street2):            dto.Street2 = value; break;
            case nameof(ImportUserRowDto.City):               dto.City = value; break;
            case nameof(ImportUserRowDto.Governorate):        dto.Governorate = value; break;
            case nameof(ImportUserRowDto.PostalCode):         dto.PostalCode = value; break;
            case nameof(ImportUserRowDto.Country):            dto.Country = value; break;
            case nameof(ImportUserRowDto.MustChangePassword): dto.MustChangePassword = value; break;
            case nameof(ImportUserRowDto.SendWelcomeEmail):   dto.SendWelcomeEmail = value; break;
        }
    }

    /// <summary>
    /// Converts the raw ApprovalOverride string from the file to a nullable bool
    /// for use in CreateUserCommand.
    /// </summary>
    public static bool? ParseApprovalOverride(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) ||
            string.Equals(raw.Trim(), "FollowGlobal", StringComparison.OrdinalIgnoreCase))
            return null;

        if (string.Equals(raw.Trim(), "AlwaysRequire", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.Equals(raw.Trim(), "AlwaysAutoApprove", StringComparison.OrdinalIgnoreCase))
            return false;

        return null; // Safe fallback
    }

    /// <summary>Parses TRUE/FALSE cell value to bool with a safe default.</summary>
    public static bool ParseBool(string? raw, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(raw)) return defaultValue;
        return string.Equals(raw.Trim(), "TRUE", StringComparison.OrdinalIgnoreCase);
    }
}
