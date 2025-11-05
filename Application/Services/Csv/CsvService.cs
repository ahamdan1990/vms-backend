using System.Globalization;
using System.Text;
using System.IO.Compression;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using VisitorManagementSystem.Api.Application.Services.Pdf;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;
using VisitorManagementSystem.Api.Application.DTOs.Users;
using VisitorManagementSystem.Api.Application.DTOs.Visitors;

namespace VisitorManagementSystem.Api.Application.Services.Csv;

/// <summary>
/// CSV service implementation for invitation workflows
/// </summary>
public class CsvService : ICsvService
{
    private readonly ILogger<CsvService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    // CSV column headers for invitation template
    private static readonly string[] RequiredHeaders = {
        "Section", "Field", "Value", "Instructions"
    };

    private static readonly string[] OptionalHeaders = {
        "Required", "Example"
    };

    public CsvService(ILogger<CsvService> logger, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }
    /// <summary>
    /// Generates a blank CSV template for manual invitation creation with reference sheets
    /// </summary>
    public async Task<byte[]> GenerateInvitationTemplateAsync(bool includeMultipleVisitors = true, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get reference data from database
            var hosts = await GetActiveHostsAsync(cancellationToken);
            var recentVisitors = await GetRecentVisitorsAsync(cancellationToken);

            // Create ZIP file containing main template + reference sheets
            using var memoryStream = new MemoryStream();
            using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true);

            // Create main invitation template CSV
            await CreateMainTemplateCSV(archive, includeMultipleVisitors);

            // Create reference sheets
            await CreateHostsReferenceCSV(archive, hosts);
            await CreateVisitorsReferenceCSV(archive, recentVisitors);
            await CreateInstructionsCSV(archive, includeMultipleVisitors);

            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate CSV invitation template with reference sheets");
            throw;
        }
    }

    /// <summary>
    /// Gets active hosts from the database for reference data
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
    /// Gets recent visitors from the database for reference data
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
    /// Creates the main invitation template CSV in the archive
    /// </summary>
    private async Task CreateMainTemplateCSV(ZipArchive archive, bool includeMultipleVisitors)
    {
        var entry = archive.CreateEntry("invitation-template.csv");
        using var entryStream = entry.Open();
        using var writer = new StreamWriter(entryStream, Encoding.UTF8);
        using var csvWriter = new CsvWriter(writer, GetCsvConfiguration());

        var templateRows = GenerateEnhancedTemplateRows(includeMultipleVisitors);

        // Write headers
        csvWriter.WriteField("Section");
        csvWriter.WriteField("Field");
        csvWriter.WriteField("Value");
        csvWriter.WriteField("Instructions");
        csvWriter.WriteField("Required");
        csvWriter.WriteField("Example");
        csvWriter.NextRecord();

        // Write template rows
        foreach (var row in templateRows)
        {
            csvWriter.WriteField(row.Section);
            csvWriter.WriteField(row.Field);
            csvWriter.WriteField(row.Value);
            csvWriter.WriteField(row.Instructions);
            csvWriter.WriteField(row.Required);
            csvWriter.WriteField(row.Example);
            csvWriter.NextRecord();
        }

        await csvWriter.FlushAsync();
        await writer.FlushAsync();
    }

    /// <summary>
    /// Creates the hosts reference CSV in the archive
    /// </summary>
    private async Task CreateHostsReferenceCSV(ZipArchive archive, List<UserListDto> hosts)
    {
        var entry = archive.CreateEntry("available-hosts.csv");
        using var entryStream = entry.Open();
        using var writer = new StreamWriter(entryStream, Encoding.UTF8);
        using var csvWriter = new CsvWriter(writer, GetCsvConfiguration());

        // Write headers
        csvWriter.WriteField("ID");
        csvWriter.WriteField("Full Name");
        csvWriter.WriteField("Email");
        csvWriter.WriteField("Department");
        csvWriter.WriteField("Role");
        csvWriter.WriteField("Display Format");
        csvWriter.NextRecord();

        // Write host data
        foreach (var host in hosts)
        {
            csvWriter.WriteField(host.Id);
            csvWriter.WriteField(host.FullName);
            csvWriter.WriteField(host.Email);
            csvWriter.WriteField(host.Department ?? "");
            csvWriter.WriteField(host.Role);
            csvWriter.WriteField($"{host.FullName} ({host.Email}){(string.IsNullOrEmpty(host.Department) ? "" : $" - {host.Department}")}");
            csvWriter.NextRecord();
        }

        await csvWriter.FlushAsync();
        await writer.FlushAsync();
    }
    /// <summary>
    /// Creates the visitors reference CSV in the archive
    /// </summary>
    private async Task CreateVisitorsReferenceCSV(ZipArchive archive, List<VisitorListDto> visitors)
    {
        var entry = archive.CreateEntry("available-visitors.csv");
        using var entryStream = entry.Open();
        using var writer = new StreamWriter(entryStream, Encoding.UTF8);
        using var csvWriter = new CsvWriter(writer, GetCsvConfiguration());

        // Write headers
        csvWriter.WriteField("ID");
        csvWriter.WriteField("Full Name");
        csvWriter.WriteField("Email");
        csvWriter.WriteField("Company");
        csvWriter.WriteField("Phone Number");
        csvWriter.WriteField("Display Format");
        csvWriter.NextRecord();

        // Add "Create New Visitor" option
        csvWriter.WriteField("0");
        csvWriter.WriteField("Create New Visitor");
        csvWriter.WriteField("");
        csvWriter.WriteField("");
        csvWriter.WriteField("");
        csvWriter.WriteField("Create New Visitor");
        csvWriter.NextRecord();

        // Write visitor data
        foreach (var visitor in visitors)
        {
            csvWriter.WriteField(visitor.Id);
            csvWriter.WriteField(visitor.FullName);
            csvWriter.WriteField(visitor.Email);
            csvWriter.WriteField(visitor.Company ?? "");
            csvWriter.WriteField(visitor.PhoneNumber ?? "");
            csvWriter.WriteField($"{visitor.FullName} ({visitor.Email}){(string.IsNullOrEmpty(visitor.Company) ? "" : $" - {visitor.Company}")}");
            csvWriter.NextRecord();
        }

        await csvWriter.FlushAsync();
        await writer.FlushAsync();
    }

    /// <summary>
    /// Creates the instructions CSV in the archive
    /// </summary>
    private async Task CreateInstructionsCSV(ZipArchive archive, bool includeMultipleVisitors)
    {
        var entry = archive.CreateEntry("instructions.csv");
        using var entryStream = entry.Open();
        using var writer = new StreamWriter(entryStream, Encoding.UTF8);
        using var csvWriter = new CsvWriter(writer, GetCsvConfiguration());

        var instructions = new[]
        {
            new { Section = "OVERVIEW", Content = "Enhanced CSV Invitation Template with Reference Data" },
            new { Section = "FILES INCLUDED", Content = "invitation-template.csv - Main template to fill out" },
            new { Section = "FILES INCLUDED", Content = "available-hosts.csv - List of active system hosts" },
            new { Section = "FILES INCLUDED", Content = "available-visitors.csv - List of recent visitors" },
            new { Section = "FILES INCLUDED", Content = "instructions.csv - This file" },
            new { Section = "HOW TO USE", Content = "1. Open invitation-template.csv in Excel/Google Sheets" },
            new { Section = "HOW TO USE", Content = "2. Reference available-hosts.csv to select the correct Host ID" },
            new { Section = "HOW TO USE", Content = "3. Reference available-visitors.csv to select Visitor ID or use 0 for new" },
            new { Section = "HOW TO USE", Content = "4. Fill in all required fields marked with 'Yes'" },
            new { Section = "HOW TO USE", Content = "5. Use the exact Display Format values for host/visitor selection" },
            new { Section = "HOW TO USE", Content = "6. Save and upload only the invitation-template.csv file" },
            new { Section = "IMPORTANT", Content = "Host Selection: Use exact text from 'Display Format' column in available-hosts.csv" },
            new { Section = "IMPORTANT", Content = "Visitor Selection: Use exact text from 'Display Format' column in available-visitors.csv" },
            new { Section = "IMPORTANT", Content = "Date Format: YYYY-MM-DD HH:MM (e.g., 2024-12-01 14:30)" },
            new { Section = "IMPORTANT", Content = "Boolean Fields: Use exactly 'Yes' or 'No'" },
            new { Section = "SUPPORT", Content = "Contact IT support if you need assistance with this template" }
        };

        // Write headers
        csvWriter.WriteField("Section");
        csvWriter.WriteField("Instructions");
        csvWriter.NextRecord();

        // Write instructions
        foreach (var instruction in instructions)
        {
            csvWriter.WriteField(instruction.Section);
            csvWriter.WriteField(instruction.Content);
            csvWriter.NextRecord();
        }

        await csvWriter.FlushAsync();
        await writer.FlushAsync();
    }
    /// <summary>
    /// Generates enhanced template rows with reference to IDs
    /// </summary>
    private List<CsvInvitationRow> GenerateEnhancedTemplateRows(bool includeMultipleVisitors)
    {
        var rows = new List<CsvInvitationRow>();

        // Host section - enhanced with reference to available-hosts.csv
        rows.AddRange(new[]
        {
            new CsvInvitationRow("Host", "SelectHost", "", "Select host from available-hosts.csv Display Format column", "Yes", "John Smith (john.smith@company.com) - Engineering"),
        });

        // Visitor sections - enhanced with reference to available-visitors.csv
        var visitorCount = includeMultipleVisitors ? 3 : 1;
        for (int i = 1; i <= visitorCount; i++)
        {
            var sectionName = $"Visitor{i}";
            rows.AddRange(new[]
            {
                new CsvInvitationRow(sectionName, "SelectVisitor", "", "Select from available-visitors.csv Display Format or 'Create New Visitor'", "Yes", "Jane Doe (jane.doe@email.com) - ABC Corp"),
                new CsvInvitationRow(sectionName, "FirstName", "", "Required only if creating new visitor", "No*", "Jane"),
                new CsvInvitationRow(sectionName, "LastName", "", "Required only if creating new visitor", "No*", "Doe"),
                new CsvInvitationRow(sectionName, "Email", "", "Required only if creating new visitor", "No*", "jane.doe@email.com"),
                new CsvInvitationRow(sectionName, "PhoneNumber", "", "Phone number (optional)", "No", "+1-555-987-6543"),
                new CsvInvitationRow(sectionName, "Company", "", "Company name (optional)", "No", "ABC Corp"),
                new CsvInvitationRow(sectionName, "GovernmentId", "", "Government ID (optional)", "No", "123456789"),
                new CsvInvitationRow(sectionName, "Nationality", "", "Nationality (optional)", "No", "American"),
                new CsvInvitationRow(sectionName, "EmergencyContactFirstName", "", "Emergency contact first name", "No", "John"),
                new CsvInvitationRow(sectionName, "EmergencyContactLastName", "", "Emergency contact last name", "No", "Doe"),
                new CsvInvitationRow(sectionName, "EmergencyContactPhone", "", "Emergency contact phone", "No", "+1-555-111-2222"),
                new CsvInvitationRow(sectionName, "EmergencyContactRelationship", "", "Relationship to emergency contact", "No", "Spouse")
            });
        }

        // Meeting section - same as before
        rows.AddRange(new[]
        {
            new CsvInvitationRow("Meeting", "Subject", "", "Meeting subject or title", "Yes", "Project Discussion"),
            new CsvInvitationRow("Meeting", "ScheduledStartTime", "", "Start date and time (YYYY-MM-DD HH:mm)", "Yes", "2024-12-01 14:00"),
            new CsvInvitationRow("Meeting", "ScheduledEndTime", "", "End date and time (YYYY-MM-DD HH:mm)", "Yes", "2024-12-01 15:30"),
            new CsvInvitationRow("Meeting", "Purpose", "", "Purpose of visit", "No", "Business meeting"),
            new CsvInvitationRow("Meeting", "Location", "", "Meeting location", "No", "Conference Room A"),
            new CsvInvitationRow("Meeting", "SpecialInstructions", "", "Special instructions", "No", "Please bring ID"),
            new CsvInvitationRow("Meeting", "RequiresEscort", "", "Requires escort (Yes/No)", "No", "No"),
            new CsvInvitationRow("Meeting", "RequiresBadge", "", "Requires visitor badge (Yes/No)", "No", "Yes"),
            new CsvInvitationRow("Meeting", "ParkingInstructions", "", "Parking instructions", "No", "Visitor parking in lot B")
        });

        return rows;
    }

    /// <summary>
    /// Parses a filled CSV invitation file
    /// </summary>
    public Task<ParsedInvitationData> ParseFilledInvitationAsync(Stream csvStream, CancellationToken cancellationToken = default)
    {
        var parsedData = new ParsedInvitationData();

        try
        {
            using var reader = new StreamReader(csvStream);
            using var csvReader = new CsvReader(reader, GetCsvConfiguration());

            var records = csvReader.GetRecords<CsvInvitationRow>().ToList();

            if (!records.Any())
            {
                parsedData.ValidationErrors.Add("CSV file is empty or has no valid records");
                return Task.FromResult(parsedData);
            }

            // Parse sections
            ParseHostSection(records, parsedData);
            ParseVisitorSections(records, parsedData);
            ParseMeetingSection(records, parsedData);

            // Validate required data
            ValidateParsedData(parsedData);

            return Task.FromResult(parsedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse CSV invitation file");
            parsedData.ValidationErrors.Add($"Failed to parse CSV file: {ex.Message}");
            return Task.FromResult(parsedData);
        }
    }
    /// <summary>
    /// Generates a filled invitation CSV for an existing invitation
    /// </summary>
    public async Task<byte[]> GenerateFilledInvitationCsvAsync(Invitation invitation, CancellationToken cancellationToken = default)
    {
        try
        {
            var filledRows = GenerateFilledRows(invitation);
            
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
            using var csvWriter = new CsvWriter(writer, GetCsvConfiguration());

            // Write headers
            csvWriter.WriteField("Section");
            csvWriter.WriteField("Field");
            csvWriter.WriteField("Value");
            csvWriter.WriteField("Instructions");
            csvWriter.WriteField("Required");
            csvWriter.WriteField("Example");
            csvWriter.NextRecord();

            // Write filled rows
            foreach (var row in filledRows)
            {
                csvWriter.WriteField(row.Section);
                csvWriter.WriteField(row.Field);
                csvWriter.WriteField(row.Value);
                csvWriter.WriteField(row.Instructions);
                csvWriter.WriteField(row.Required);
                csvWriter.WriteField(row.Example);
                csvWriter.NextRecord();
            }

            await csvWriter.FlushAsync();
            await writer.FlushAsync();

            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate filled CSV invitation for invitation {InvitationId}", invitation.Id);
            throw;
        }
    }

    /// <summary>
    /// Validates CSV structure and required fields
    /// </summary>
    public Task<CsvValidationResult> ValidateCsvStructureAsync(Stream csvStream, CancellationToken cancellationToken = default)
    {
        try
        {
            using var reader = new StreamReader(csvStream);
            using var csvReader = new CsvReader(reader, GetCsvConfiguration());

            // Read headers
            if (!csvReader.Read() || !csvReader.ReadHeader())
            {
                return Task.FromResult(CsvValidationResult.Failure(new List<string> { "CSV file has no headers" }));
            }

            var headers = csvReader.HeaderRecord?.ToList() ?? new List<string>();
            var errors = new List<string>();

            // Validate required headers
            foreach (var requiredHeader in RequiredHeaders)
            {
                if (!headers.Contains(requiredHeader, StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add($"Missing required header: {requiredHeader}");
                }
            }

            if (errors.Any())
            {
                return Task.FromResult(CsvValidationResult.Failure(errors));
            }

            // Count rows and visitor sections
            var rowCount = 0;
            var visitorSectionCount = 0;

            while (csvReader.Read())
            {
                rowCount++;
                var section = csvReader.GetField("Section")?.Trim();

                if (!string.IsNullOrEmpty(section) && section.StartsWith("Visitor", StringComparison.OrdinalIgnoreCase))
                {
                    visitorSectionCount++;
                }
            }

            return Task.FromResult(CsvValidationResult.Success(rowCount, headers, visitorSectionCount));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate CSV structure");
            return Task.FromResult(CsvValidationResult.Failure(new List<string> { $"Validation failed: {ex.Message}" }));
        }
    }
    /// <summary>
    /// Generates filled rows for an existing invitation
    /// </summary>
    private List<CsvInvitationRow> GenerateFilledRows(Invitation invitation)
    {
        var rows = new List<CsvInvitationRow>();

        // Host section
        rows.AddRange(new[]
        {
            new CsvInvitationRow("Host", "FullName", invitation.Host?.FullName ?? "", "Full name of the host", "Yes", "John Smith"),
            new CsvInvitationRow("Host", "Email", invitation.Host?.Email?.Value ?? "", "Email address of the host", "Yes", "john.smith@company.com"),
            new CsvInvitationRow("Host", "PhoneNumber", invitation.Host?.PhoneNumber?.Value ?? "", "Phone number of the host", "No", "+1-555-123-4567"),
            new CsvInvitationRow("Host", "Department", invitation.Host?.Department ?? "", "Department or team", "No", "Engineering")
        });

        // Visitor section
        var visitor = invitation.Visitor;
        rows.AddRange(new[]
        {
            new CsvInvitationRow("Visitor1", "FirstName", visitor?.FirstName ?? "", "First name of visitor", "Yes", "Jane"),
            new CsvInvitationRow("Visitor1", "LastName", visitor?.LastName ?? "", "Last name of visitor", "Yes", "Doe"),
            new CsvInvitationRow("Visitor1", "Email", visitor?.Email?.Value ?? "", "Email address of visitor", "Yes", "jane.doe@email.com"),
            new CsvInvitationRow("Visitor1", "PhoneNumber", visitor?.PhoneNumber?.Value ?? "", "Phone number of visitor", "No", "+1-555-987-6543"),
            new CsvInvitationRow("Visitor1", "Company", visitor?.Company ?? "", "Company or organization", "No", "ABC Corp"),
            new CsvInvitationRow("Visitor1", "GovernmentId", visitor?.GovernmentId ?? "", "Government ID number", "No", "123456789"),
            new CsvInvitationRow("Visitor1", "Nationality", visitor?.Nationality ?? "", "Nationality", "No", "American")
        });

        // Emergency contact if available
        var emergencyContact = visitor?.EmergencyContacts?.FirstOrDefault();
        if (emergencyContact != null)
        {
            rows.AddRange(new[]
            {
                new CsvInvitationRow("Visitor1", "EmergencyContactFirstName", emergencyContact.FirstName, "Emergency contact first name", "No", "John"),
                new CsvInvitationRow("Visitor1", "EmergencyContactLastName", emergencyContact.LastName, "Emergency contact last name", "No", "Doe"),
                new CsvInvitationRow("Visitor1", "EmergencyContactPhone", emergencyContact.PhoneNumber?.Value ?? "", "Emergency contact phone", "No", "+1-555-111-2222"),
                new CsvInvitationRow("Visitor1", "EmergencyContactRelationship", emergencyContact.Relationship, "Relationship to emergency contact", "No", "Spouse")
            });
        }

        // Meeting section
        rows.AddRange(new[]
        {
            new CsvInvitationRow("Meeting", "Subject", invitation.Subject, "Meeting subject or title", "Yes", "Project Discussion"),
            new CsvInvitationRow("Meeting", "ScheduledStartTime", invitation.ScheduledStartTime.ToString("yyyy-MM-dd HH:mm"), "Start date and time (YYYY-MM-DD HH:mm)", "Yes", "2024-12-01 14:00"),
            new CsvInvitationRow("Meeting", "ScheduledEndTime", invitation.ScheduledEndTime.ToString("yyyy-MM-dd HH:mm"), "End date and time (YYYY-MM-DD HH:mm)", "Yes", "2024-12-01 15:30"),
            new CsvInvitationRow("Meeting", "Purpose", invitation.VisitPurpose?.Name ?? "", "Purpose of visit", "No", "Business meeting"),
            new CsvInvitationRow("Meeting", "Location", invitation.Location?.Name ?? "", "Meeting location", "No", "Conference Room A"),
            new CsvInvitationRow("Meeting", "SpecialInstructions", invitation.SpecialInstructions ?? "", "Special instructions", "No", "Please bring ID"),
            new CsvInvitationRow("Meeting", "RequiresEscort", invitation.RequiresEscort ? "Yes" : "No", "Requires escort (Yes/No)", "No", "No"),
            new CsvInvitationRow("Meeting", "RequiresBadge", invitation.RequiresBadge ? "Yes" : "No", "Requires visitor badge (Yes/No)", "No", "Yes"),
            new CsvInvitationRow("Meeting", "ParkingInstructions", invitation.ParkingInstructions ?? "", "Parking instructions", "No", "Visitor parking in lot B")
        });

        return rows;
    }

    /// <summary>
    /// Parses host section from CSV records
    /// </summary>
    private void ParseHostSection(List<CsvInvitationRow> records, ParsedInvitationData parsedData)
    {
        var hostRecords = records.Where(r => r.Section.Equals("Host", StringComparison.OrdinalIgnoreCase)).ToList();

        parsedData.Host.FullName = GetFieldValue(hostRecords, "FullName");
        parsedData.Host.Email = GetFieldValue(hostRecords, "Email");
        parsedData.Host.PhoneNumber = GetFieldValue(hostRecords, "PhoneNumber");
        parsedData.Host.Department = GetFieldValue(hostRecords, "Department");
    }
    /// <summary>
    /// Parses visitor sections from CSV records
    /// </summary>
    private void ParseVisitorSections(List<CsvInvitationRow> records, ParsedInvitationData parsedData)
    {
        var visitorSections = records
            .Where(r => r.Section.StartsWith("Visitor", StringComparison.OrdinalIgnoreCase))
            .GroupBy(r => r.Section)
            .OrderBy(g => g.Key);

        foreach (var visitorGroup in visitorSections)
        {
            var visitorRecords = visitorGroup.ToList();
            var visitor = new ParsedVisitorData
            {
                FirstName = GetFieldValue(visitorRecords, "FirstName"),
                LastName = GetFieldValue(visitorRecords, "LastName"),
                Email = GetFieldValue(visitorRecords, "Email"),
                PhoneNumber = GetFieldValue(visitorRecords, "PhoneNumber"),
                Company = GetFieldValue(visitorRecords, "Company"),
                GovernmentId = GetFieldValue(visitorRecords, "GovernmentId"),
                Nationality = GetFieldValue(visitorRecords, "Nationality")
            };

            // Parse emergency contact if available
            var emergencyFirstName = GetFieldValue(visitorRecords, "EmergencyContactFirstName");
            var emergencyLastName = GetFieldValue(visitorRecords, "EmergencyContactLastName");
            var emergencyPhone = GetFieldValue(visitorRecords, "EmergencyContactPhone");
            var emergencyRelationship = GetFieldValue(visitorRecords, "EmergencyContactRelationship");

            if (!string.IsNullOrEmpty(emergencyFirstName) || !string.IsNullOrEmpty(emergencyLastName))
            {
                visitor.EmergencyContact = new ParsedEmergencyContact
                {
                    FirstName = emergencyFirstName,
                    LastName = emergencyLastName,
                    PhoneNumber = emergencyPhone,
                    Relationship = emergencyRelationship
                };
            }

            parsedData.Visitors.Add(visitor);
        }
    }

    /// <summary>
    /// Parses meeting section from CSV records
    /// </summary>
    private void ParseMeetingSection(List<CsvInvitationRow> records, ParsedInvitationData parsedData)
    {
        var meetingRecords = records.Where(r => r.Section.Equals("Meeting", StringComparison.OrdinalIgnoreCase)).ToList();

        parsedData.Meeting.Subject = GetFieldValue(meetingRecords, "Subject");
        parsedData.Meeting.Purpose = GetFieldValue(meetingRecords, "Purpose");
        parsedData.Meeting.Location = GetFieldValue(meetingRecords, "Location");
        parsedData.Meeting.SpecialInstructions = GetFieldValue(meetingRecords, "SpecialInstructions");
        parsedData.Meeting.ParkingInstructions = GetFieldValue(meetingRecords, "ParkingInstructions");

        // Parse boolean fields
        parsedData.Meeting.RequiresEscort = ParseBooleanField(GetFieldValue(meetingRecords, "RequiresEscort"));
        parsedData.Meeting.RequiresBadge = ParseBooleanField(GetFieldValue(meetingRecords, "RequiresBadge"));

        // Parse date/time fields
        if (DateTime.TryParse(GetFieldValue(meetingRecords, "ScheduledStartTime"), out var startTime))
        {
            parsedData.Meeting.ScheduledStartTime = startTime;
        }

        if (DateTime.TryParse(GetFieldValue(meetingRecords, "ScheduledEndTime"), out var endTime))
        {
            parsedData.Meeting.ScheduledEndTime = endTime;
        }
    }
    /// <summary>
    /// Validates parsed invitation data
    /// </summary>
    private void ValidateParsedData(ParsedInvitationData parsedData)
    {
        // Validate host
        if (string.IsNullOrEmpty(parsedData.Host.FullName))
            parsedData.ValidationErrors.Add("Host full name is required");

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
    /// Gets field value from CSV records
    /// </summary>
    private static string GetFieldValue(List<CsvInvitationRow> records, string fieldName)
    {
        return records.FirstOrDefault(r => r.Field.Equals(fieldName, StringComparison.OrdinalIgnoreCase))?.Value?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Parses boolean field from string
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
    /// Gets CSV configuration
    /// </summary>
    private static CsvConfiguration GetCsvConfiguration()
    {
        return new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            IgnoreBlankLines = true,
            BadDataFound = null // Ignore bad data rather than throwing
        };
    }
}

/// <summary>
/// CSV invitation row model
/// </summary>
public class CsvInvitationRow
{
    public string Section { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public string Required { get; set; } = string.Empty;
    public string Example { get; set; } = string.Empty;

    public CsvInvitationRow() { }

    public CsvInvitationRow(string section, string field, string value, string instructions, string required, string example)
    {
        Section = section;
        Field = field;
        Value = value;
        Instructions = instructions;
        Required = required;
        Example = example;
    }
}