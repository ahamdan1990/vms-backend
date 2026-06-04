using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Globalization;
using System.Text;
using VisitorManagementSystem.Api.Application.Commands.CivilDefense;
using VisitorManagementSystem.Api.Application.Commands.Invitations;
using VisitorManagementSystem.Api.Application.DTOs.CivilDefense;
using VisitorManagementSystem.Api.Application.Queries.CivilDefense;
using VisitorManagementSystem.Api.Application.Services.FaceDetection;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Hubs;

namespace VisitorManagementSystem.Api.Controllers;

[ApiController]
[Route("api/civil-defense")]
[Authorize]
public class CivilDefenseController : BaseController
{
    private readonly IMediator _mediator;
    private readonly ILogger<CivilDefenseController> _logger;
    private readonly IFaceTemplateEnrollmentService _faceEnrollment;
    private readonly IHubContext<OperatorHub> _operatorHub;

    public CivilDefenseController(
        IMediator mediator,
        ILogger<CivilDefenseController> logger,
        IFaceTemplateEnrollmentService faceEnrollment,
        IHubContext<OperatorHub> operatorHub)
    {
        _mediator = mediator;
        _logger = logger;
        _faceEnrollment = faceEnrollment;
        _operatorHub = operatorHub;
    }

    // GET /api/civil-defense/presence
    [HttpGet("presence")]
    [Authorize(Policy = Permissions.CivilDefense.ViewPresence)]
    public async Task<IActionResult> GetPresence(
        [FromQuery] int? locationId = null,
        [FromQuery] string? type = null,
        [FromQuery] string? search = null)
    {
        var query = new GetCivilDefensePresenceQuery
        {
            LocationId = locationId,
            Type = type,
            SearchTerm = search
        };
        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    // GET /api/civil-defense/presence/summary
    [HttpGet("presence/summary")]
    [Authorize(Policy = Permissions.CivilDefense.ViewPresence)]
    public async Task<IActionResult> GetPresenceSummary()
    {
        var query = new GetCivilDefensePresenceQuery();
        var result = await _mediator.Send(query);
        return SuccessResponse(result.Summary);
    }

    // GET /api/civil-defense/cameras
    [HttpGet("cameras")]
    [Authorize(Policy = Permissions.CivilDefense.ViewPresence)]
    public async Task<IActionResult> GetCameras()
    {
        var result = await _mediator.Send(new GetCivilDefenseCamerasQuery());
        return SuccessResponse(result);
    }

    // GET /api/civil-defense/staff/search
    [HttpGet("staff/search")]
    [Authorize(Policy = Permissions.CivilDefense.CheckInStaff)]
    public async Task<IActionResult> SearchStaff(
        [FromQuery] string? searchTerm = null,
        [FromQuery] int pageSize = 30)
    {
        var result = await _mediator.Send(new GetCdStaffSearchQuery
        {
            SearchTerm = searchTerm,
            PageSize = pageSize,
        });
        return SuccessResponse(result);
    }

    // POST /api/civil-defense/staff/check-in
    [HttpPost("staff/check-in")]
    [Authorize(Policy = Permissions.CivilDefense.CheckInStaff)]
    public async Task<IActionResult> StaffCheckIn([FromBody] StaffCheckInCommand command)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        command.CheckedInById = userId.Value;
        var presenceId = await _mediator.Send(command);
        await NotifyPresenceChanged();
        return SuccessResponse(new { staffPresenceId = presenceId }, "Staff checked in successfully.");
    }

    // POST /api/civil-defense/staff/{id}/check-out
    [HttpPost("staff/{id:int}/check-out")]
    [Authorize(Policy = Permissions.CivilDefense.CheckOutStaff)]
    public async Task<IActionResult> StaffCheckOut(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var command = new StaffCheckOutCommand
        {
            StaffPresenceId = id,
            CheckedOutById = userId.Value
        };
        await _mediator.Send(command);
        await NotifyPresenceChanged();
        return SuccessResponse(new { }, "Staff checked out successfully.");
    }

    // GET /api/civil-defense/visitors/lookup
    [HttpGet("visitors/lookup")]
    [Authorize(Policy = Permissions.CivilDefense.ViewPresence)]
    public async Task<IActionResult> LookupVisitor([FromQuery] string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return BadRequest("Phone number is required.");

        var result = await _mediator.Send(new LookupVisitorByPhoneQuery { Phone = phone });
        if (result == null)
            return NotFound(new { message = "No visitor found with that phone number." });

        return SuccessResponse(result);
    }

    // POST /api/civil-defense/visitors/quick-checkin
    [HttpPost("visitors/quick-checkin")]
    [Authorize(Policy = Permissions.CivilDefense.RegisterVisitor)]
    public async Task<IActionResult> VisitorQuickCheckIn([FromBody] VisitorQuickCheckInRequest data)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var command = new VisitorQuickCheckInCommand
        {
            Data = data,
            RegisteredById = userId.Value
        };
        var checkIn = await _mediator.Send(command);
        await NotifyPresenceChanged();
        return SuccessResponse(new { visitorId = checkIn.VisitorId, invitationId = checkIn.InvitationId }, "Visitor checked in successfully.");
    }

    // POST /api/civil-defense/visitors/{invitationId}/check-out
    [HttpPost("visitors/{invitationId:int}/check-out")]
    [Authorize(Policy = Permissions.CivilDefense.CheckOutVisitor)]
    public async Task<IActionResult> VisitorCheckOut(int invitationId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var command = new CheckOutInvitationCommand
        {
            InvitationId = invitationId,
            CheckedOutBy = userId.Value
        };
        await _mediator.Send(command);
        await NotifyPresenceChanged();
        return SuccessResponse(new { }, "Visitor checked out successfully.");
    }

    // POST /api/civil-defense/staff/{id}/temp-out
    [HttpPost("staff/{id:int}/temp-out")]
    [Authorize(Policy = Permissions.CivilDefense.CheckOutStaff)]
    public async Task<IActionResult> StaffTempLeave(int id, [FromBody] TempLeaveRequest? body = null)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        await _mediator.Send(new StaffTempLeaveCommand
        {
            StaffPresenceId = id,
            Reason = body?.Reason,
            RecordedById = userId.Value
        });
        await NotifyPresenceChanged();
        return SuccessResponse(new { }, "Temporary leave recorded.");
    }

    // POST /api/civil-defense/staff/{id}/temp-return
    [HttpPost("staff/{id:int}/temp-return")]
    [Authorize(Policy = Permissions.CivilDefense.CheckInStaff)]
    public async Task<IActionResult> StaffTempReturn(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        await _mediator.Send(new StaffTempReturnCommand
        {
            StaffPresenceId = id,
            ReturnedById = userId.Value
        });
        await NotifyPresenceChanged();
        return SuccessResponse(new { }, "Staff marked as returned.");
    }

    // POST /api/civil-defense/visitors/{invitationId}/temp-out
    [HttpPost("visitors/{invitationId:int}/temp-out")]
    [Authorize(Policy = Permissions.CivilDefense.CheckOutVisitor)]
    public async Task<IActionResult> VisitorTempLeave(int invitationId, [FromBody] TempLeaveRequest? body = null)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        await _mediator.Send(new VisitorTempLeaveCommand
        {
            InvitationId = invitationId,
            Reason = body?.Reason,
            RecordedById = userId.Value
        });
        await NotifyPresenceChanged();
        return SuccessResponse(new { }, "Temporary leave recorded.");
    }

    // POST /api/civil-defense/visitors/{invitationId}/temp-return
    [HttpPost("visitors/{invitationId:int}/temp-return")]
    [Authorize(Policy = Permissions.CivilDefense.RegisterVisitor)]
    public async Task<IActionResult> VisitorTempReturn(int invitationId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        await _mediator.Send(new VisitorTempReturnCommand
        {
            InvitationId = invitationId,
            ReturnedById = userId.Value
        });
        await NotifyPresenceChanged();
        return SuccessResponse(new { }, "Visitor marked as returned.");
    }

    // POST /api/civil-defense/recognize-face
    [HttpPost("recognize-face")]
    [Authorize(Policy = Permissions.CivilDefense.RegisterVisitor)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> RecognizeFace(IFormFile photo, CancellationToken cancellationToken)
    {
        if (photo == null || photo.Length == 0)
            return BadRequest("No photo provided.");

        var result = await _mediator.Send(
            new RecognizeFaceForCdCommand { Photo = photo }, cancellationToken);
        return SuccessResponse(result);
    }

    // POST /api/civil-defense/visitors/{visitorId}/enroll-face
    [HttpPost("visitors/{visitorId:int}/enroll-face")]
    [Authorize(Policy = Permissions.CivilDefense.RegisterVisitor)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> EnrollVisitorFace(int visitorId, IFormFile photo)
    {
        if (photo == null || photo.Length == 0)
            return BadRequest("No photo provided.");

        using var ms = new MemoryStream();
        await photo.CopyToAsync(ms);
        var bytes = ms.ToArray();

        var result = await _faceEnrollment.EnrollVisitorPhotoAsync(
            visitorId, bytes, photo.FileName, HttpContext.RequestAborted);

        if (!result.Success && !result.Skipped)
            return Problem(result.Message ?? "Face enrollment failed.", statusCode: 422);

        return SuccessResponse(new { enrolled = result.Success, message = result.Message });
    }

    // GET /api/civil-defense/reports
    [HttpGet("reports")]
    [Authorize(Policy = Permissions.CivilDefense.ViewReports)]
    public async Task<IActionResult> GetReports(
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        [FromQuery] bool? isCivilian = null,
        [FromQuery] int? locationId = null,
        [FromQuery] string? search = null,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 50)
    {
        var query = new GetCivilDefenseReportsQuery
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            Type = type,
            Status = status,
            IsCivilian = isCivilian,
            LocationId = locationId,
            SearchTerm = search,
            PageIndex = pageIndex,
            PageSize = Math.Min(pageSize, 200)
        };
        var result = await _mediator.Send(query);
        return SuccessResponse(result);
    }

    // GET /api/civil-defense/reports/export
    [HttpGet("reports/export")]
    [Authorize(Policy = Permissions.CivilDefense.ExportReports)]
    public async Task<IActionResult> ExportReports(
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        [FromQuery] bool? isCivilian = null,
        [FromQuery] int? locationId = null,
        [FromQuery] string? search = null,
        [FromQuery] string? format = "csv")
    {
        var query = new GetCivilDefenseReportsQuery
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            Type = type,
            Status = status,
            IsCivilian = isCivilian,
            LocationId = locationId,
            SearchTerm = search,
            PageIndex = 0,
            PageSize = 10000
        };
        var result = await _mediator.Send(query);
        var dateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");

        if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Civil Defense Report");

            var headers = new[]
            {
                "Type", "Name", "Phone", "Company/Department", "Civilian",
                "Affiliated Organization", "Host", "Purpose", "Location",
                "Check-In", "Check-Out", "Duration (min)", "Status"
            };

            for (var col = 0; col < headers.Length; col++)
            {
                var cell = ws.Cells[1, col + 1];
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(200, 16, 46));
                cell.Style.Font.Color.SetColor(Color.White);
            }

            for (var row = 0; row < result.Items.Count; row++)
            {
                var item = result.Items[row];
                var r = row + 2;
                ws.Cells[r, 1].Value  = item.EntryType;
                ws.Cells[r, 2].Value  = item.Name;
                ws.Cells[r, 3].Value  = item.Phone;
                ws.Cells[r, 4].Value  = item.CompanyOrDepartment;
                ws.Cells[r, 5].Value  = item.IsCivilian.HasValue ? (item.IsCivilian.Value ? "Yes" : "No") : "";
                ws.Cells[r, 6].Value  = item.AffiliatedOrganization;
                ws.Cells[r, 7].Value  = item.HostName;
                ws.Cells[r, 8].Value  = item.Purpose;
                ws.Cells[r, 9].Value  = item.LocationName;
                ws.Cells[r, 10].Value = item.CheckedInAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                ws.Cells[r, 11].Value = item.CheckedOutAt.HasValue
                    ? item.CheckedOutAt.Value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) : "";
                ws.Cells[r, 12].Value = item.DurationMinutes;
                ws.Cells[r, 13].Value = item.Status;
            }

            ws.Cells[ws.Dimension.Address].AutoFitColumns();

            var xlsxBytes = package.GetAsByteArray();
            return File(xlsxBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"civil-defense-report-{dateStr}.xlsx");
        }

        var csv = BuildCsv(result.Items);
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"civil-defense-report-{dateStr}.csv");
    }

    private static string BuildCsv(List<CivilDefenseReportEntryDto> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Type,Name,Phone,Company/Department,Civilian,Affiliated Organization,Host,Purpose,Location,Check-In,Check-Out,Duration (min),Status");

        foreach (var item in items)
        {
            sb.AppendLine(string.Join(",",
                CsvEscape(item.EntryType),
                CsvEscape(item.Name),
                CsvEscape(item.Phone),
                CsvEscape(item.CompanyOrDepartment),
                item.IsCivilian.HasValue ? (item.IsCivilian.Value ? "Yes" : "No") : "",
                CsvEscape(item.AffiliatedOrganization),
                CsvEscape(item.HostName),
                CsvEscape(item.Purpose),
                CsvEscape(item.LocationName),
                item.CheckedInAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                item.CheckedOutAt.HasValue
                    ? item.CheckedOutAt.Value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                    : "",
                item.DurationMinutes.ToString(),
                CsvEscape(item.Status)
            ));
        }
        return sb.ToString();
    }

    private static string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private Task NotifyPresenceChanged() =>
        _operatorHub.Clients.Group("Operators")
            .SendAsync("CivilDefensePresenceUpdated", new { timestamp = DateTime.UtcNow });
}

public class TempLeaveRequest
{
    public string? Reason { get; set; }
}
