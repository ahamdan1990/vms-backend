using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Infrastructure.Data;
using Xunit;

namespace VisitorManagementSystem.Api.Tests;

public sealed class HistoricalRelationshipTests
{
    [Fact]
    public void Historical_principal_relationships_are_optional_and_restrictive()
    {
        using var context = CreateContext();

        AssertOptionalRestrictRelationship<CameraFaceEvent>(context, nameof(CameraFaceEvent.Camera));
        AssertOptionalRestrictRelationship<StaffPresence>(context, nameof(StaffPresence.User));
        AssertOptionalRestrictRelationship<StaffPresence>(context, nameof(StaffPresence.CheckedInByUser));
        AssertOptionalRestrictRelationship<TemporaryLeave>(context, nameof(TemporaryLeave.RecordedByUser));
        AssertOptionalRestrictRelationship<AlertEscalationLog>(context, nameof(AlertEscalationLog.NotificationAlert));
    }

    [Fact]
    public void Historical_records_remain_queryable_without_soft_deleted_principals()
    {
        using var context = CreateContext();
        var now = DateTime.UtcNow;

        context.CameraFaceEvents.Add(new CameraFaceEvent
        {
            CameraId = 101,
            CameraReferenceId = null,
            CameraName = "Removed camera",
            CapturedAt = now
        });
        context.StaffPresences.Add(new StaffPresence
        {
            UserId = 202,
            UserReferenceId = null,
            UserDisplayName = "Removed staff member",
            CheckedInAt = now,
            CheckedInById = 303,
            CheckedInByReferenceId = null
        });
        context.TemporaryLeaves.Add(new TemporaryLeave
        {
            PersonType = TemporaryLeavePersonType.Staff,
            LeftAt = now,
            RecordedById = 404,
            RecordedByReferenceId = null
        });

        context.SaveChanges();
        context.ChangeTracker.Clear();

        Assert.Equal("Removed camera", context.CameraFaceEvents.Single().CameraName);
        Assert.Equal("Removed staff member", context.StaffPresences.Single().UserDisplayName);
        Assert.Equal(404, context.TemporaryLeaves.Single().RecordedById);
    }

    [Fact]
    public void Inactive_escalation_rules_do_not_hide_escalation_log_history()
    {
        using var context = CreateContext();

        context.AlertEscalationLogs.Add(new AlertEscalationLog
        {
            NotificationAlertId = 505,
            AttemptNumber = 1,
            AlertEscalation = new AlertEscalation
            {
                RuleName = "Retired rule",
                IsActive = false
            }
        });

        context.SaveChanges();
        context.ChangeTracker.Clear();

        var log = context.AlertEscalationLogs
            .Include(x => x.AlertEscalation)
            .Single();

        Assert.Equal("Retired rule", log.AlertEscalation.RuleName);
        Assert.False(log.AlertEscalation.IsActive);
        Assert.Null(context.Model.FindEntityType(typeof(AlertEscalation))!.GetQueryFilter());
    }

    private static ApplicationDbContext CreateContext()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new ApplicationDbContext(options, services);
    }

    private static void AssertOptionalRestrictRelationship<TEntity>(
        ApplicationDbContext context,
        string navigationName)
        where TEntity : class
    {
        var entityType = context.Model.FindEntityType(typeof(TEntity));
        var navigation = entityType!.FindNavigation(navigationName);

        Assert.NotNull(navigation);
        Assert.False(navigation!.ForeignKey.IsRequired);
        Assert.Equal(DeleteBehavior.Restrict, navigation.ForeignKey.DeleteBehavior);
    }
}
