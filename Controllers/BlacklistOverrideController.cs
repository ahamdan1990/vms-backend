using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagementSystem.Api.Application.Services.Notifications;
using VisitorManagementSystem.Api.Domain.Constants;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;

namespace VisitorManagementSystem.Api.Controllers;

[ApiController]
[Route("api/checkin/blacklist-overrides")]
[Authorize]
public class BlacklistOverrideController : BaseController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly ILogger<BlacklistOverrideController> _logger;

    public BlacklistOverrideController(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        ILogger<BlacklistOverrideController> logger)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Receptionist requests authorization to check in a blacklisted visitor.
    /// Returns a token that can be used in the check-in command once granted.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.CheckIn.Process)]
    public async Task<IActionResult> RequestOverride([FromBody] RequestOverrideDto dto, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return UnauthorizedResponse();

        var visitors = await _unitOfWork.Repository<Visitor>()
            .GetAllAsync(predicate: v => v.Id == dto.VisitorId, cancellationToken: cancellationToken);
        var visitor = visitors.FirstOrDefault();
        if (visitor == null) return NotFoundResponse("Visitor", dto.VisitorId);
        if (!visitor.IsBlacklisted) return BadRequestResponse("Visitor is not blacklisted.");

        var requester = await _unitOfWork.Users.GetByIdAsync(userId.Value, cancellationToken);
        if (requester == null) return UnauthorizedResponse();

        var overrideRequest = new BlacklistOverrideRequest
        {
            VisitorId = dto.VisitorId,
            RequestedByUserId = userId.Value,
            Status = "Pending"
        };

        await _unitOfWork.Repository<BlacklistOverrideRequest>().AddAsync(overrideRequest, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationService.NotifyBlacklistOverrideRequestedAsync(
            overrideRequest.Token,
            visitor.Id,
            $"{visitor.FirstName} {visitor.LastName}",
            requester.FullName,
            cancellationToken);

        _logger.LogWarning("Blacklist override requested for visitor {VisitorId} by user {UserId}", dto.VisitorId, userId.Value);

        return SuccessResponse(new { Token = overrideRequest.Token, Status = "Pending" });
    }

    /// <summary>
    /// Poll the status of a blacklist override request.
    /// </summary>
    [HttpGet("{token:guid}")]
    [Authorize(Policy = Permissions.CheckIn.Process)]
    public async Task<IActionResult> GetStatus(Guid token, CancellationToken cancellationToken)
    {
        var results = await _unitOfWork.Repository<BlacklistOverrideRequest>()
            .GetAllAsync(predicate: r => r.Token == token, cancellationToken: cancellationToken);
        var req = results.FirstOrDefault();
        if (req == null) return NotFoundResponse("Override request", token);

        return SuccessResponse(new { req.Token, req.Status, req.ReviewedAt, req.Notes });
    }

    /// <summary>
    /// List all pending blacklist override requests (admin/security only).
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Policy = Permissions.CheckIn.Override)]
    public async Task<IActionResult> GetPending(CancellationToken cancellationToken)
    {
        var pending = await _unitOfWork.Repository<BlacklistOverrideRequest>()
            .GetAllAsync(
                predicate: r => r.Status == "Pending",
                include: "Visitor,RequestedByUser",
                cancellationToken: cancellationToken);

        var result = pending.Select(r => new
        {
            r.Token,
            r.VisitorId,
            VisitorName = $"{r.Visitor.FirstName} {r.Visitor.LastName}",
            BlacklistReason = r.Visitor.BlacklistReason,
            RequestedByName = r.RequestedByUser.FullName,
            r.CreatedOn,
            r.Status
        });

        return SuccessResponse(result);
    }

    /// <summary>
    /// Grant a blacklist override — receptionist may now proceed with check-in.
    /// </summary>
    [HttpPost("{token:guid}/grant")]
    [Authorize(Policy = Permissions.CheckIn.Override)]
    public async Task<IActionResult> GrantOverride(Guid token, [FromBody] ReviewOverrideDto dto, CancellationToken cancellationToken)
        => await ReviewOverride(token, "Granted", dto.Notes, cancellationToken);

    /// <summary>
    /// Deny a blacklist override request.
    /// </summary>
    [HttpPost("{token:guid}/deny")]
    [Authorize(Policy = Permissions.CheckIn.Override)]
    public async Task<IActionResult> DenyOverride(Guid token, [FromBody] ReviewOverrideDto dto, CancellationToken cancellationToken)
        => await ReviewOverride(token, "Denied", dto.Notes, cancellationToken);

    private async Task<IActionResult> ReviewOverride(Guid token, string decision, string? notes, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return UnauthorizedResponse();

        var results = await _unitOfWork.Repository<BlacklistOverrideRequest>()
            .GetAllAsync(predicate: r => r.Token == token, include: "Visitor", cancellationToken: cancellationToken);
        var req = results.FirstOrDefault();
        if (req == null) return NotFoundResponse("Override request", token);
        if (req.Status != "Pending") return BadRequestResponse($"Override request is already {req.Status}.");

        var reviewer = await _unitOfWork.Users.GetByIdAsync(userId.Value, cancellationToken);
        if (reviewer == null) return UnauthorizedResponse();

        req.Status = decision;
        req.ReviewedByUserId = userId.Value;
        req.ReviewedAt = DateTime.UtcNow;
        req.Notes = notes;

        _unitOfWork.Repository<BlacklistOverrideRequest>().Update(req);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationService.NotifyBlacklistOverrideResultAsync(
            req.Token,
            req.VisitorId,
            decision == "Granted",
            reviewer.FullName,
            notes,
            cancellationToken);

        _logger.LogWarning("Blacklist override {Decision} for visitor {VisitorId} by user {UserId} (token {Token})",
            decision, req.VisitorId, userId.Value, token);

        return SuccessResponse(new { req.Token, Status = req.Status });
    }
}

public class RequestOverrideDto
{
    public int VisitorId { get; set; }
}

public class ReviewOverrideDto
{
    public string? Notes { get; set; }
}
