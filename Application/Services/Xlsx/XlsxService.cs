using OfficeOpenXml;
using OfficeOpenXml.Style;
using OfficeOpenXml.DataValidation;
using Microsoft.Extensions.Logging;
using VisitorManagementSystem.Api.Application.Services.Pdf;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;
using System.Drawing;

namespace VisitorManagementSystem.Api.Application.Services.Xlsx;

/// <summary>
/// XLSX service implementation for invitation workflows with dropdown support
/// </summary>
public class XlsxService : IXlsxService
{
    private readonly ILogger<XlsxService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    // XLSX worksheet names
    private const string MainWorksheetName = "Data Entry";
    private const string HostsWorksheetName = "Available Hosts";
    private const string VisitorsWorksheetName = "Available Visitors";
    private const string InstructionsWorksheetName = "Instructions";
    private const string MetadataWorksheetName = "Metadata";

    public XlsxService(ILogger<XlsxService> logger, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        
        // Set EPPlus license context (for non-commercial use)
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }
    /// <summary>
    /// Generates a blank XLSX template with dropdowns for manual invitation creation
    /// </summary>
    public async Task<byte[]> GenerateInvitationTemplateAsync(bool includeMultipleVisitors = true, CancellationToken cancellationToken = default)
    {
        try
        {
            using var package = new ExcelPackage();
            
            // Get reference data from database
            var hosts = await GetActiveHostsAsync(cancellationToken);
            var recentVisitors = await GetRecentVisitorsAsync(cancellationToken);
            
            // Create worksheets
            CreateInstructionsWorksheet(package);
            CreateMainDataWorksheet(package, includeMultipleVisitors);
            CreateHostsReferenceWorksheet(package, hosts);
            CreateVisitorsReferenceWorksheet(package, recentVisitors);
            CreateMetadataWorksheet(package, includeMultipleVisitors);
            
            // Set up dropdowns and validation
            ConfigureDropdownsAndValidation(package, hosts, recentVisitors, includeMultipleVisitors);
            
            // Set active worksheet to main data entry
            package.Workbook.Worksheets[MainWorksheetName].Select();
            
            return package.GetAsByteArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate XLSX invitation template");
            throw;
        }
    }
    /// <summary>
    /// Gets active hosts from the database for dropdown population
    /// </summary>
    private async Task<List<UserListDto>> GetActiveHostsAsync(CancellationToken cancellationToken)
    {
        var activeUsers = await _unitOfWork.Users.GetActiveUsersAsync(cancellationToken);
        
        return activeUsers.Select(u => new UserListDto
        {
            Id = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            FullName = u.FullName,
            Email = u.Email.Value,
            Department = u.Department,
            Role = u.Role.ToString()
        }).OrderBy(u => u.FullName).ToList();
    }

    /// <summary>
    /// Gets recent visitors from the database for dropdown population
    /// </summary>
    private async Task<List<VisitorListDto>> GetRecentVisitorsAsync(CancellationToken cancellationToken)
    {
        // Get visitors from the last 2 years
        var cutoffDate = DateTime.UtcNow.AddYears(-2);
        var recentVisitors = await _unitOfWork.Visitors.GetVisitorsByDateRangeAsync(cutoffDate, DateTime.UtcNow, cancellationToken);
        
        return recentVisitors.Select(v => new VisitorListDto
        {
            Id = v.Id,
            FullName = v.FullName,
            Email = v.Email.Value,
            Company = v.Company,
            PhoneNumber = v.PhoneNumber?.Value
        }).OrderBy(v => v.FullName).Take(1000).ToList(); // Limit to 1000 most recent
    }
    /// <summary>
    /// Creates the instructions worksheet with usage guide
    /// </summary>
    private void CreateInstructionsWorksheet(ExcelPackage package)
    {
        var worksheet = package.Workbook.Worksheets.Add(InstructionsWorksheetName);
        
        // Title
        worksheet.Cells["A1"].Value = "Visitor Invitation Template - Instructions";
        worksheet.Cells["A1"].Style.Font.Size = 16;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["A1"].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
        
        // Instructions
        var instructions = new[]
        {
            "",
            "HOW TO USE THIS TEMPLATE:",
            "1. Go to the 'Data Entry' tab to fill in your invitation details",
            "2. Select hosts from the dropdown (populated from active system users)",
            "3. Select existing visitors from dropdown OR choose 'Create New Visitor' to add new ones",
            "4. Fill in all required fields (marked with *)",
            "5. Use multiple visitor rows if needed",
            "6. Save the file and upload it through the system",
            "",
            "IMPORTANT NOTES:",
            "• Dropdown selections are mandatory - don't type manually",
            "• Date format: YYYY-MM-DD HH:MM (e.g., 2024-12-01 14:30)",
            "• Times are in 24-hour format",
            "• Yes/No fields: Select exactly 'Yes' or 'No'",
            "• Don't modify the reference sheets (Available Hosts/Visitors)",
            "",
            "NEED HELP?",
            "Contact IT support if you encounter any issues."
        };
        
        for (int i = 0; i < instructions.Length; i++)
        {
            worksheet.Cells[i + 2, 1].Value = instructions[i];
            if (instructions[i].Contains(":"))
            {
                worksheet.Cells[i + 2, 1].Style.Font.Bold = true;
            }
        }
        
        worksheet.Column(1).Width = 80;
        worksheet.Column(1).Style.WrapText = true;
    }
    /// <summary>
    /// Creates the main data entry worksheet with structured input fields
    /// </summary>
    private void CreateMainDataWorksheet(ExcelPackage package, bool includeMultipleVisitors)
    {
        var worksheet = package.Workbook.Worksheets.Add(MainWorksheetName);
        
        // Title
        worksheet.Cells["A1"].Value = "Visitor Invitation Data Entry";
        worksheet.Cells["A1:G1"].Merge = true;
        worksheet.Cells["A1"].Style.Font.Size = 14;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        
        int currentRow = 3;
        
        // Host Section
        currentRow = CreateHostSection(worksheet, currentRow);
        
        // Meeting Details Section
        currentRow = CreateMeetingSection(worksheet, currentRow);
        
        // Visitors Section
        int visitorCount = includeMultipleVisitors ? 3 : 1;
        currentRow = CreateVisitorsSection(worksheet, currentRow, visitorCount);
        
        // Set column widths
        worksheet.Column(1).Width = 25; // Field labels
        worksheet.Column(2).Width = 30; // Values
        worksheet.Column(3).Width = 15; // Required
        worksheet.Column(4).Width = 40; // Instructions
        worksheet.Column(5).Width = 25; // Example
        
        // Freeze panes
        worksheet.View.FreezePanes(3, 1);
    }
    /// <summary>
    /// Creates the host selection section
    /// </summary>
    private int CreateHostSection(ExcelWorksheet worksheet, int startRow)
    {
        // Section header
        worksheet.Cells[startRow, 1].Value = "HOST INFORMATION";
        worksheet.Cells[startRow, 1].Style.Font.Bold = true;
        worksheet.Cells[startRow, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells[startRow, 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
        worksheet.Cells[startRow, 1, startRow, 5].Merge = true;
        
        startRow += 2;
        
        // Headers
        var headers = new[] { "Field", "Value", "Required", "Instructions", "Example" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[startRow, i + 1].Value = headers[i];
            worksheet.Cells[startRow, i + 1].Style.Font.Bold = true;
        }
        
        startRow++;
        
        // Host selection field
        worksheet.Cells[startRow, 1].Value = "Select Host";
        worksheet.Cells[startRow, 2].Value = ""; // Will be dropdown
        worksheet.Cells[startRow, 3].Value = "Yes";
        worksheet.Cells[startRow, 4].Value = "Select the host from the dropdown list";
        worksheet.Cells[startRow, 5].Value = "John Smith (john.smith@company.com) - Engineering";
        
        // Mark host field for dropdown
        worksheet.Cells[startRow, 2].AddComment("Select host from dropdown", "System");
        
        return startRow + 2;
    }
    /// <summary>
    /// Creates the meeting details section
    /// </summary>
    private int CreateMeetingSection(ExcelWorksheet worksheet, int startRow)
    {
        // Section header
        worksheet.Cells[startRow, 1].Value = "MEETING DETAILS";
        worksheet.Cells[startRow, 1].Style.Font.Bold = true;
        worksheet.Cells[startRow, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells[startRow, 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
        worksheet.Cells[startRow, 1, startRow, 5].Merge = true;
        
        startRow += 2;
        
        var meetingFields = new[]
        {
            new { Field = "Meeting Subject", Required = "Yes", Instructions = "Brief description of the meeting purpose", Example = "Project Discussion Meeting" },
            new { Field = "Start Date & Time", Required = "Yes", Instructions = "Format: YYYY-MM-DD HH:MM", Example = "2024-12-01 14:00" },
            new { Field = "End Date & Time", Required = "Yes", Instructions = "Format: YYYY-MM-DD HH:MM", Example = "2024-12-01 15:30" },
            new { Field = "Meeting Location", Required = "No", Instructions = "Meeting room or location", Example = "Conference Room A" },
            new { Field = "Visit Purpose", Required = "No", Instructions = "Purpose of the visit", Example = "Business Meeting" },
            new { Field = "Special Instructions", Required = "No", Instructions = "Any special requirements", Example = "Please bring ID" },
            new { Field = "Requires Escort", Required = "No", Instructions = "Select Yes or No", Example = "No" },
            new { Field = "Requires Badge", Required = "No", Instructions = "Select Yes or No", Example = "Yes" },
            new { Field = "Parking Instructions", Required = "No", Instructions = "Parking details", Example = "Visitor parking in lot B" }
        };
        
        foreach (var field in meetingFields)
        {
            worksheet.Cells[startRow, 1].Value = field.Field;
            worksheet.Cells[startRow, 2].Value = ""; // Empty for user input
            worksheet.Cells[startRow, 3].Value = field.Required;
            worksheet.Cells[startRow, 4].Value = field.Instructions;
            worksheet.Cells[startRow, 5].Value = field.Example;
            
            // Color code required fields
            if (field.Required == "Yes")
            {
                worksheet.Cells[startRow, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[startRow, 3].Style.Fill.BackgroundColor.SetColor(Color.LightCoral);
            }
            
            startRow++;
        }
        
        return startRow + 1;
    }
    /// <summary>
    /// Creates the visitors section with support for multiple visitors
    /// </summary>
    private int CreateVisitorsSection(ExcelWorksheet worksheet, int startRow, int visitorCount)
    {
        for (int visitorIndex = 1; visitorIndex <= visitorCount; visitorIndex++)
        {
            // Section header
            worksheet.Cells[startRow, 1].Value = $"VISITOR {visitorIndex}";
            worksheet.Cells[startRow, 1].Style.Font.Bold = true;
            worksheet.Cells[startRow, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[startRow, 1].Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
            worksheet.Cells[startRow, 1, startRow, 5].Merge = true;
            
            startRow += 2;
            
            var visitorFields = new[]
            {
                new { Field = "Select Visitor", Required = "Yes", Instructions = "Select existing visitor or 'Create New Visitor'", Example = "Jane Doe (jane.doe@email.com) - ABC Corp" },
                new { Field = "First Name", Required = "Yes*", Instructions = "Required only for new visitors", Example = "Jane" },
                new { Field = "Last Name", Required = "Yes*", Instructions = "Required only for new visitors", Example = "Doe" },
                new { Field = "Email", Required = "Yes*", Instructions = "Required only for new visitors", Example = "jane.doe@email.com" },
                new { Field = "Phone Number", Required = "No", Instructions = "Visitor's phone number", Example = "+1-555-987-6543" },
                new { Field = "Company", Required = "No", Instructions = "Visitor's company", Example = "ABC Corporation" },
                new { Field = "Government ID", Required = "No", Instructions = "ID number for security", Example = "123456789" },
                new { Field = "Nationality", Required = "No", Instructions = "Visitor's nationality", Example = "American" },
                new { Field = "Emergency Contact Name", Required = "No", Instructions = "Emergency contact full name", Example = "John Doe" },
                new { Field = "Emergency Contact Phone", Required = "No", Instructions = "Emergency contact phone", Example = "+1-555-111-2222" },
                new { Field = "Emergency Contact Relationship", Required = "No", Instructions = "Relationship to visitor", Example = "Spouse" }
            };
            
            foreach (var field in visitorFields)
            {
                worksheet.Cells[startRow, 1].Value = field.Field;
                worksheet.Cells[startRow, 2].Value = ""; // Empty for user input
                worksheet.Cells[startRow, 3].Value = field.Required;
                worksheet.Cells[startRow, 4].Value = field.Instructions;
                worksheet.Cells[startRow, 5].Value = field.Example;
                
                // Color code required fields
                if (field.Required.StartsWith("Yes"))
                {
                    worksheet.Cells[startRow, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[startRow, 3].Style.Fill.BackgroundColor.SetColor(Color.LightCoral);
                }
                
                startRow++;
            }
            
            startRow++; // Space between visitors
        }
        
        return startRow;
    }
    /// <summary>
    /// Creates the hosts reference worksheet (hidden from users)
    /// </summary>
    private void CreateHostsReferenceWorksheet(ExcelPackage package, List<UserListDto> hosts)
    {
        var worksheet = package.Workbook.Worksheets.Add(HostsWorksheetName);
        
        // Headers
        worksheet.Cells[1, 1].Value = "ID";
        worksheet.Cells[1, 2].Value = "Display Name";
        worksheet.Cells[1, 3].Value = "Email";
        worksheet.Cells[1, 4].Value = "Department";
        
        // Style headers
        using (var range = worksheet.Cells[1, 1, 1, 4])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
        }
        
        // Add host data
        for (int i = 0; i < hosts.Count; i++)
        {
            var host = hosts[i];
            var row = i + 2;
            
            worksheet.Cells[row, 1].Value = host.Id;
            worksheet.Cells[row, 2].Value = $"{host.FullName} ({host.Email}){(string.IsNullOrEmpty(host.Department) ? "" : $" - {host.Department}")}";
            worksheet.Cells[row, 3].Value = host.Email;
            worksheet.Cells[row, 4].Value = host.Department ?? "";
        }
        
        // Auto-fit columns
        worksheet.Cells.AutoFitColumns();
        
        // Hide the worksheet
        worksheet.Hidden = eWorkSheetHidden.Hidden;
    }
    /// <summary>
    /// Creates the visitors reference worksheet (hidden from users)
    /// </summary>
    private void CreateVisitorsReferenceWorksheet(ExcelPackage package, List<VisitorListDto> visitors)
    {
        var worksheet = package.Workbook.Worksheets.Add(VisitorsWorksheetName);
        
        // Headers
        worksheet.Cells[1, 1].Value = "ID";
        worksheet.Cells[1, 2].Value = "Display Name";
        worksheet.Cells[1, 3].Value = "Email";
        worksheet.Cells[1, 4].Value = "Company";
        
        // Style headers
        using (var range = worksheet.Cells[1, 1, 1, 4])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
        }
        
        // Add "Create New Visitor" option at the top
        worksheet.Cells[2, 1].Value = 0;
        worksheet.Cells[2, 2].Value = "Create New Visitor";
        worksheet.Cells[2, 3].Value = "";
        worksheet.Cells[2, 4].Value = "";
        
        // Add visitor data
        for (int i = 0; i < visitors.Count; i++)
        {
            var visitor = visitors[i];
            var row = i + 3; // Start from row 3 due to "Create New" option
            
            worksheet.Cells[row, 1].Value = visitor.Id;
            worksheet.Cells[row, 2].Value = $"{visitor.FullName} ({visitor.Email}){(string.IsNullOrEmpty(visitor.Company) ? "" : $" - {visitor.Company}")}";
            worksheet.Cells[row, 3].Value = visitor.Email;
            worksheet.Cells[row, 4].Value = visitor.Company ?? "";
        }
        
        // Auto-fit columns
        worksheet.Cells.AutoFitColumns();
        
        // Hide the worksheet
        worksheet.Hidden = eWorkSheetHidden.Hidden;
    }
    /// <summary>
    /// Creates the metadata worksheet with template information
    /// </summary>
    private void CreateMetadataWorksheet(ExcelPackage package, bool includeMultipleVisitors)
    {
        var worksheet = package.Workbook.Worksheets.Add(MetadataWorksheetName);
        
        // Template metadata
        var metadata = new[]
        {
            new { Key = "Template Version", Value = "1.0" },
            new { Key = "Created Date", Value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") },
            new { Key = "Template Type", Value = includeMultipleVisitors ? "Multiple Visitors" : "Single Visitor" },
            new { Key = "System", Value = "Visitor Management System" },
            new { Key = "Format", Value = "XLSX with Dropdowns" },
            new { Key = "Instructions", Value = "See Instructions worksheet" }
        };
        
        // Headers
        worksheet.Cells[1, 1].Value = "Property";
        worksheet.Cells[1, 2].Value = "Value";
        worksheet.Cells[1, 1, 1, 2].Style.Font.Bold = true;
        
        // Add metadata
        for (int i = 0; i < metadata.Length; i++)
        {
            var item = metadata[i];
            var row = i + 2;
            
            worksheet.Cells[row, 1].Value = item.Key;
            worksheet.Cells[row, 2].Value = item.Value;
        }
        
        // Auto-fit columns
        worksheet.Cells.AutoFitColumns();
        
        // Hide the worksheet
        worksheet.Hidden = eWorkSheetHidden.Hidden;
    }
    /// <summary>
    /// Configures dropdowns and data validation for the main worksheet
    /// </summary>
    private void ConfigureDropdownsAndValidation(ExcelPackage package, List<UserListDto> hosts, List<VisitorListDto> visitors, bool includeMultipleVisitors)
    {
        var mainWorksheet = package.Workbook.Worksheets[MainWorksheetName];
        var hostsWorksheet = package.Workbook.Worksheets[HostsWorksheetName];
        var visitorsWorksheet = package.Workbook.Worksheets[VisitorsWorksheetName];
        
        // Configure host dropdown (row will be dynamically found)
        var hostRow = FindFieldRow(mainWorksheet, "Select Host");
        if (hostRow > 0)
        {
            var hostRange = $"'{HostsWorksheetName}'!$B$2:$B${hosts.Count + 1}";
            var validation = mainWorksheet.DataValidations.AddListValidation($"B{hostRow}");
            validation.Formula.ExcelFormula = hostRange;
            validation.ShowErrorMessage = true;
            validation.ErrorTitle = "Invalid Host";
            validation.Error = "Please select a host from the dropdown list.";
        }
        
        // Configure visitor dropdowns
        int visitorCount = includeMultipleVisitors ? 3 : 1;
        for (int i = 1; i <= visitorCount; i++)
        {
            var visitorRow = FindFieldRow(mainWorksheet, "Select Visitor", i);
            if (visitorRow > 0)
            {
                var visitorRange = $"'{VisitorsWorksheetName}'!$B$2:$B${visitors.Count + 2}"; // +2 for "Create New" option
                var validation = mainWorksheet.DataValidations.AddListValidation($"B{visitorRow}");
                validation.Formula.ExcelFormula = visitorRange;
                validation.ShowErrorMessage = true;
                validation.ErrorTitle = "Invalid Visitor";
                validation.Error = "Please select a visitor from the dropdown list or choose 'Create New Visitor'.";
            }
        }
        
        // Configure Yes/No dropdowns
        ConfigureYesNoDropdowns(mainWorksheet);
        
        // Configure date validation
        ConfigureDateValidation(mainWorksheet);
    }
    /// <summary>
    /// Finds the row number for a specific field in the worksheet
    /// </summary>
    private int FindFieldRow(ExcelWorksheet worksheet, string fieldName, int occurrence = 1)
    {
        int foundCount = 0;
        for (int row = 1; row <= (worksheet.Dimension?.End.Row ?? 0); row++)
        {
            var cellValue = worksheet.Cells[row, 1].Value?.ToString();
            if (cellValue == fieldName)
            {
                foundCount++;
                if (foundCount == occurrence)
                {
                    return row;
                }
            }
        }
        return 0; // Not found
    }

    /// <summary>
    /// Configures Yes/No dropdowns for boolean fields
    /// </summary>
    private void ConfigureYesNoDropdowns(ExcelWorksheet worksheet)
    {
        var yesNoFields = new[] { "Requires Escort", "Requires Badge" };
        
        foreach (var field in yesNoFields)
        {
            var row = FindFieldRow(worksheet, field);
            if (row > 0)
            {
                var validation = worksheet.DataValidations.AddListValidation($"B{row}");
                validation.Formula.Values.Add("Yes");
                validation.Formula.Values.Add("No");
                validation.ShowErrorMessage = true;
                validation.ErrorTitle = "Invalid Value";
                validation.Error = "Please select either 'Yes' or 'No'.";
            }
        }
    }
    /// <summary>
    /// Configures date validation for date/time fields
    /// </summary>
    private void ConfigureDateValidation(ExcelWorksheet worksheet)
    {
        var dateFields = new[] { "Start Date & Time", "End Date & Time" };
        
        foreach (var field in dateFields)
        {
            var row = FindFieldRow(worksheet, field);
            if (row > 0)
            {
                // Set number format for date/time
                worksheet.Cells[row, 2].Style.Numberformat.Format = "yyyy-mm-dd hh:mm";
                
                // Add comment with format help
                worksheet.Cells[row, 2].AddComment("Format: YYYY-MM-DD HH:MM (e.g., 2024-12-01 14:30)", "System");
            }
        }
    }

    /// <summary>
    /// Parses a filled XLSX invitation file
    /// </summary>
    public async Task<ParsedInvitationData> ParseFilledInvitationAsync(Stream xlsxStream, CancellationToken cancellationToken = default)
    {
        var parsedData = new ParsedInvitationData();

        try
        {
            using var package = new ExcelPackage(xlsxStream);
            var mainWorksheet = package.Workbook.Worksheets[MainWorksheetName];

            if (mainWorksheet == null)
            {
                parsedData.ValidationErrors.Add($"Main worksheet '{MainWorksheetName}' not found");
                return parsedData;
            }

            // Parse host information
            await ParseHostInformation(mainWorksheet, parsedData, package, cancellationToken);

            // Parse meeting information
            ParseMeetingInformation(mainWorksheet, parsedData);

            // Parse visitor information
            await ParseVisitorInformation(mainWorksheet, parsedData, package, cancellationToken);

            // Validate parsed data
            ValidateParsedData(parsedData);

            return parsedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse XLSX invitation file");
            parsedData.ValidationErrors.Add($"Failed to parse XLSX file: {ex.Message}");
            return parsedData;
        }
    }
    /// <summary>
    /// Parses host information from the XLSX file
    /// </summary>
    private async Task ParseHostInformation(ExcelWorksheet worksheet, ParsedInvitationData parsedData, ExcelPackage package, CancellationToken cancellationToken)
    {
        var hostRow = FindFieldRow(worksheet, "Select Host");
        if (hostRow > 0)
        {
            var hostValue = worksheet.Cells[hostRow, 2].Value?.ToString();
            if (!string.IsNullOrEmpty(hostValue))
            {
                // Extract host information from dropdown selection
                var hostInfo = ParseHostFromDropdownValue(hostValue, package);
                if (hostInfo != null)
                {
                    // Look up full host details from database
                    var host = await _unitOfWork.Users.GetByIdAsync(hostInfo.Value.Id, cancellationToken);
                    if (host != null)
                    {
                        parsedData.Host.FullName = host.FullName;
                        parsedData.Host.Email = host.Email.Value;
                        parsedData.Host.PhoneNumber = host.PhoneNumber?.Value ?? "";
                        parsedData.Host.Department = host.Department ?? "";
                    }
                    else
                    {
                        parsedData.ValidationErrors.Add("Selected host not found in system");
                    }
                }
                else
                {
                    parsedData.ValidationErrors.Add("Invalid host selection format");
                }
            }
            else
            {
                parsedData.ValidationErrors.Add("Host selection is required");
            }
        }
    }

    /// <summary>
    /// Parses host information from dropdown value format
    /// </summary>
    private (int Id, string DisplayName)? ParseHostFromDropdownValue(string dropdownValue, ExcelPackage package)
    {
        // Expected format: "Name (email@domain.com) - Department"
        // But we need to get the ID from the reference sheet
        var hostsWorksheet = package.Workbook.Worksheets[HostsWorksheetName];
        if (hostsWorksheet != null)
        {
            var maxRow = hostsWorksheet.Dimension?.End.Row ?? 0;
            for (int row = 2; row <= maxRow; row++)
            {
                var displayName = hostsWorksheet.Cells[row, 2].Value?.ToString();
                if (displayName == dropdownValue)
                {
                    var idValue = hostsWorksheet.Cells[row, 1].Value;
                    if (int.TryParse(idValue?.ToString(), out int id))
                    {
                        return (id, displayName);
                    }
                }
            }
        }
        return null;
    }

    private DateTime? ConvertExcelSerialToDate(object cellValue)
    {
        if (cellValue == null) return null;

        if (double.TryParse(cellValue.ToString(), out double oaDate))
        {
            // Excel's OLE Automation date
            return DateTime.FromOADate(oaDate);
        }

        if (DateTime.TryParse(cellValue.ToString(), out var dt))
        {
            return dt;
        }

        return null;
    }

    /// <summary>
    /// Parses meeting information from the XLSX file
    /// </summary>
    private void ParseMeetingInformation(ExcelWorksheet worksheet, ParsedInvitationData parsedData)
    {
        // Parse meeting fields
        parsedData.Meeting.Subject = GetFieldValue(worksheet, "Meeting Subject");
        parsedData.Meeting.Location = GetFieldValue(worksheet, "Meeting Location");
        parsedData.Meeting.Purpose = GetFieldValue(worksheet, "Visit Purpose");
        parsedData.Meeting.SpecialInstructions = GetFieldValue(worksheet, "Special Instructions");
        parsedData.Meeting.ParkingInstructions = GetFieldValue(worksheet, "Parking Instructions");

        // Parse boolean fields
        parsedData.Meeting.RequiresEscort = ParseBooleanField(GetFieldValue(worksheet, "Requires Escort"));
        parsedData.Meeting.RequiresBadge = ParseBooleanField(GetFieldValue(worksheet, "Requires Badge"));

        // Parse date/time fields
        var startTimeValue = GetFieldValue(worksheet, "Start Date & Time");
        if (DateTime.TryParse(startTimeValue, out var startTime))
        {
            parsedData.Meeting.ScheduledStartTime = startTime;
        }
        else
        {
            parsedData.Meeting.ScheduledStartTime = ConvertExcelSerialToDate(startTimeValue);
        }

        var endTimeValue = GetFieldValue(worksheet, "End Date & Time");
        if (DateTime.TryParse(endTimeValue, out var endTime))
        {
            parsedData.Meeting.ScheduledEndTime = endTime;
        }
        else
        {
            parsedData.Meeting.ScheduledEndTime = ConvertExcelSerialToDate(endTimeValue);
        }
    }

    /// <summary>
    /// Gets field value from worksheet by field name
    /// </summary>
    private string GetFieldValue(ExcelWorksheet worksheet, string fieldName)
    {
        var row = FindFieldRow(worksheet, fieldName);
        if (row > 0)
        {
            return worksheet.Cells[row, 2].Value?.ToString()?.Trim() ?? string.Empty;
        }
        return string.Empty;
    }

    /// <summary>
    /// Parses boolean field from string value
    /// </summary>
    private static bool ParseBooleanField(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        return value.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("True", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("1", StringComparison.OrdinalIgnoreCase);
    }
    /// <summary>
    /// Parses visitor information from the XLSX file
    /// </summary>
    private async Task ParseVisitorInformation(ExcelWorksheet worksheet, ParsedInvitationData parsedData, ExcelPackage package, CancellationToken cancellationToken)
    {
        // Find all visitor sections
        for (int visitorIndex = 1; visitorIndex <= 10; visitorIndex++) // Max 10 visitors
        {
            var visitorRow = FindFieldRow(worksheet, "Select Visitor", visitorIndex);
            if (visitorRow == 0) break; // No more visitors found

            var visitorValue = worksheet.Cells[visitorRow, 2].Value?.ToString();
            if (string.IsNullOrEmpty(visitorValue)) continue; // Skip empty visitor

            var visitor = new ParsedVisitorData();

            if (visitorValue == "Create New Visitor")
            {
                // Parse new visitor data from individual fields
                visitor.FirstName = GetVisitorFieldValue(worksheet, "First Name", visitorIndex);
                visitor.LastName = GetVisitorFieldValue(worksheet, "Last Name", visitorIndex);
                visitor.Email = GetVisitorFieldValue(worksheet, "Email", visitorIndex);
                visitor.PhoneNumber = GetVisitorFieldValue(worksheet, "Phone Number", visitorIndex);
                visitor.Company = GetVisitorFieldValue(worksheet, "Company", visitorIndex);
                visitor.GovernmentId = GetVisitorFieldValue(worksheet, "Government ID", visitorIndex);
                visitor.Nationality = GetVisitorFieldValue(worksheet, "Nationality", visitorIndex);
                visitor.IsExistingVisitor = false;  // Mark as new visitor
                visitor.ExistingVisitorId = null;   // No existing ID

                // Parse emergency contact
                var emergencyName = GetVisitorFieldValue(worksheet, "Emergency Contact Name", visitorIndex);
                var emergencyPhone = GetVisitorFieldValue(worksheet, "Emergency Contact Phone", visitorIndex);
                var emergencyRelationship = GetVisitorFieldValue(worksheet, "Emergency Contact Relationship", visitorIndex);

                if (!string.IsNullOrEmpty(emergencyName) || !string.IsNullOrEmpty(emergencyPhone))
                {
                    visitor.EmergencyContact = new ParsedEmergencyContact
                    {
                        FirstName = emergencyName.Split(' ').FirstOrDefault() ?? "",
                        LastName = emergencyName.Split(' ').Skip(1).FirstOrDefault() ?? "",
                        PhoneNumber = emergencyPhone,
                        Relationship = emergencyRelationship
                    };
                }
            }
            else
            {
                // Parse existing visitor from dropdown selection
                var visitorInfo = await ParseVisitorFromDropdownValue(visitorValue, package, cancellationToken);
                if (visitorInfo != null)
                {
                    visitor = visitorInfo;
                }
                else
                {
                    parsedData.ValidationErrors.Add($"Invalid visitor selection for visitor {visitorIndex}");
                    continue;
                }
            }

            parsedData.Visitors.Add(visitor);
        }
    }
    /// <summary>
    /// Gets visitor-specific field value by visitor index
    /// </summary>
    private string GetVisitorFieldValue(ExcelWorksheet worksheet, string fieldName, int visitorIndex)
    {
        // Find the field within the specific visitor section
        var visitorHeaderRow = FindFieldRow(worksheet, $"VISITOR {visitorIndex}");
        if (visitorHeaderRow == 0) return string.Empty;

        // Search for the field after the visitor header
        for (int row = visitorHeaderRow + 1; row <= (worksheet.Dimension?.End.Row ?? 0); row++)
        {
            var cellValue = worksheet.Cells[row, 1].Value?.ToString();
            if (cellValue == fieldName)
            {
                return worksheet.Cells[row, 2].Value?.ToString()?.Trim() ?? string.Empty;
            }
            // Stop searching if we hit the next visitor section
            if (cellValue != null && cellValue.StartsWith("VISITOR ") && cellValue != $"VISITOR {visitorIndex}")
            {
                break;
            }
        }
        return string.Empty;
    }

    /// <summary>
    /// Parses existing visitor from dropdown value
    /// </summary>
    private async Task<ParsedVisitorData?> ParseVisitorFromDropdownValue(string dropdownValue, ExcelPackage package, CancellationToken cancellationToken)
    {
        var visitorsWorksheet = package.Workbook.Worksheets[VisitorsWorksheetName];
        if (visitorsWorksheet != null)
        {
            var maxRow = visitorsWorksheet.Dimension?.End.Row ?? 0;
            for (int row = 2; row <= maxRow; row++)
            {
                var displayName = visitorsWorksheet.Cells[row, 2].Value?.ToString();
                if (displayName == dropdownValue)
                {
                    var idValue = visitorsWorksheet.Cells[row, 1].Value;
                    if (int.TryParse(idValue?.ToString(), out int id) && id > 0)
                    {
                        // Look up visitor details from database
                        var visitor = await _unitOfWork.Visitors.GetByIdAsync(id, cancellationToken);
                        if (visitor != null)
                        {
                            return new ParsedVisitorData
                            {
                                FirstName = visitor.FirstName,
                                LastName = visitor.LastName,
                                Email = visitor.Email.Value,
                                PhoneNumber = visitor.PhoneNumber?.Value ?? "",
                                Company = visitor.Company ?? "",
                                GovernmentId = visitor.GovernmentId ?? "",
                                Nationality = visitor.Nationality ?? "",
                                IsExistingVisitor = true,  // Mark as existing visitor
                                ExistingVisitorId = visitor.Id  // Store the existing visitor ID
                            };
                        }
                    }
                }
            }
        }
        return null;
    }
    /// <summary>
    /// Gets worksheet by name from the package
    /// </summary>
    private ExcelWorksheet? GetWorksheetByName(string worksheetName)
    {
        // This method would need access to the package, so we'll modify the parsing approach
        // For now, return null - this will be handled in the parsing context
        return null;
    }

    /// <summary>
    /// Validates the parsed invitation data
    /// </summary>
    private void ValidateParsedData(ParsedInvitationData parsedData)
    {
        // Validate host
        if (string.IsNullOrEmpty(parsedData.Host.FullName))
            parsedData.ValidationErrors.Add("Host selection is required");

        if (string.IsNullOrEmpty(parsedData.Host.Email))
            parsedData.ValidationErrors.Add("Host email is required");

        // Validate visitors
        if (!parsedData.Visitors.Any())
        {
            parsedData.ValidationErrors.Add("At least one visitor is required");
        }
        else
        {
            for (int i = 0; i < parsedData.Visitors.Count; i++)
            {
                var visitor = parsedData.Visitors[i];
                
                if (string.IsNullOrEmpty(visitor.FirstName))
                    parsedData.ValidationErrors.Add($"Visitor {i + 1}: First name is required");

                if (string.IsNullOrEmpty(visitor.LastName))
                    parsedData.ValidationErrors.Add($"Visitor {i + 1}: Last name is required");

                if (string.IsNullOrEmpty(visitor.Email))
                    parsedData.ValidationErrors.Add($"Visitor {i + 1}: Email is required");
            }
        }

        // Validate meeting
        if (string.IsNullOrEmpty(parsedData.Meeting.Subject))
            parsedData.ValidationErrors.Add("Meeting subject is required");

        if (!parsedData.Meeting.ScheduledStartTime.HasValue)
            parsedData.ValidationErrors.Add("Meeting start time is required");

        if (!parsedData.Meeting.ScheduledEndTime.HasValue)
            parsedData.ValidationErrors.Add("Meeting end time is required");

        if (parsedData.Meeting.ScheduledStartTime.HasValue && parsedData.Meeting.ScheduledEndTime.HasValue 
            && parsedData.Meeting.ScheduledEndTime <= parsedData.Meeting.ScheduledStartTime)
        {
            parsedData.ValidationErrors.Add("Meeting end time must be after start time");
        }
    }
    /// <summary>
    /// Generates a filled invitation XLSX for an existing invitation
    /// </summary>
    public async Task<byte[]> GenerateFilledInvitationXlsxAsync(Invitation invitation, CancellationToken cancellationToken = default)
    {
        try
        {
            using var package = new ExcelPackage();
            
            // Get reference data
            var hosts = await GetActiveHostsAsync(cancellationToken);
            var recentVisitors = await GetRecentVisitorsAsync(cancellationToken);
            
            // Create worksheets
            CreateInstructionsWorksheet(package);
            CreateMainDataWorksheet(package, false); // Single visitor for filled invitation
            CreateHostsReferenceWorksheet(package, hosts);
            CreateVisitorsReferenceWorksheet(package, recentVisitors);
            CreateMetadataWorksheet(package, false);
            
            // Fill in the data
            FillInvitationData(package, invitation);
            
            // Set up dropdowns and validation
            ConfigureDropdownsAndValidation(package, hosts, recentVisitors, false);
            
            // Set active worksheet to main data entry
            package.Workbook.Worksheets[MainWorksheetName].Select();
            
            return package.GetAsByteArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate filled XLSX invitation for invitation {InvitationId}", invitation.Id);
            throw;
        }
    }

    /// <summary>
    /// Fills the XLSX template with invitation data
    /// </summary>
    private void FillInvitationData(ExcelPackage package, Invitation invitation)
    {
        var worksheet = package.Workbook.Worksheets[MainWorksheetName];
        
        // Fill host information
        var hostRow = FindFieldRow(worksheet, "Select Host");
        if (hostRow > 0 && invitation.Host != null)
        {
            var hostDisplay = $"{invitation.Host.FullName} ({invitation.Host.Email.Value}){(string.IsNullOrEmpty(invitation.Host.Department) ? "" : $" - {invitation.Host.Department}")}";
            worksheet.Cells[hostRow, 2].Value = hostDisplay;
        }
        
        // Fill meeting information
        SetFieldValue(worksheet, "Meeting Subject", invitation.Subject);
        SetFieldValue(worksheet, "Start Date & Time", invitation.ScheduledStartTime.ToString("yyyy-MM-dd HH:mm"));
        SetFieldValue(worksheet, "End Date & Time", invitation.ScheduledEndTime.ToString("yyyy-MM-dd HH:mm"));
        SetFieldValue(worksheet, "Meeting Location", invitation.Location?.Name ?? "");
        SetFieldValue(worksheet, "Visit Purpose", invitation.VisitPurpose?.Name ?? "");
        SetFieldValue(worksheet, "Special Instructions", invitation.SpecialInstructions ?? "");
        SetFieldValue(worksheet, "Requires Escort", invitation.RequiresEscort ? "Yes" : "No");
        SetFieldValue(worksheet, "Requires Badge", invitation.RequiresBadge ? "Yes" : "No");
        SetFieldValue(worksheet, "Parking Instructions", invitation.ParkingInstructions ?? "");
        
        // Fill visitor information
        if (invitation.Visitor != null)
        {
            var visitorDisplay = $"{invitation.Visitor.FullName} ({invitation.Visitor.Email.Value}){(string.IsNullOrEmpty(invitation.Visitor.Company) ? "" : $" - {invitation.Visitor.Company}")}";
            var visitorRow = FindFieldRow(worksheet, "Select Visitor");
            if (visitorRow > 0)
            {
                worksheet.Cells[visitorRow, 2].Value = visitorDisplay;
            }
        }
    }
    /// <summary>
    /// Sets field value in worksheet by field name
    /// </summary>
    private void SetFieldValue(ExcelWorksheet worksheet, string fieldName, string value)
    {
        var row = FindFieldRow(worksheet, fieldName);
        if (row > 0)
        {
            worksheet.Cells[row, 2].Value = value;
        }
    }

    /// <summary>
    /// Validates XLSX structure and required fields
    /// </summary>
    public Task<XlsxValidationResult> ValidateXlsxStructureAsync(Stream xlsxStream, CancellationToken cancellationToken = default)
    {
        try
        {
            using var package = new ExcelPackage(xlsxStream);
            var errors = new List<string>();
            var worksheetNames = package.Workbook.Worksheets.Select(w => w.Name).ToList();

            // Check for required worksheets
            var hasMainWorksheet = worksheetNames.Contains(MainWorksheetName);
            if (!hasMainWorksheet)
            {
                errors.Add($"Main worksheet '{MainWorksheetName}' is missing");
            }

            // Count data rows and visitors
            int dataRowCount = 0;
            int visitorRowCount = 0;

            if (hasMainWorksheet)
            {
                var mainWorksheet = package.Workbook.Worksheets[MainWorksheetName];
                dataRowCount = mainWorksheet.Dimension?.End.Row ?? 0;

                // Count visitor sections
                for (int visitorIndex = 1; visitorIndex <= 10; visitorIndex++)
                {
                    var visitorRow = FindFieldRow(mainWorksheet, "Select Visitor", visitorIndex);
                    if (visitorRow > 0)
                    {
                        var visitorValue = mainWorksheet.Cells[visitorRow, 2].Value?.ToString();
                        if (!string.IsNullOrEmpty(visitorValue))
                        {
                            visitorRowCount++;
                        }
                    }
                    else
                    {
                        break; // No more visitors
                    }
                }
            }

            if (errors.Any())
            {
                return Task.FromResult(XlsxValidationResult.Failure(errors));
            }

            return Task.FromResult(XlsxValidationResult.Success(dataRowCount, worksheetNames, visitorRowCount, hasMainWorksheet));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate XLSX structure");
            return Task.FromResult(XlsxValidationResult.Failure(new List<string> { $"Validation failed: {ex.Message}" }));
        }
    }
}