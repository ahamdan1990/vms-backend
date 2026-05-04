using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

public class BlacklistOverrideRequestConfiguration : IEntityTypeConfiguration<BlacklistOverrideRequest>
{
    public void Configure(EntityTypeBuilder<BlacklistOverrideRequest> builder)
    {
        builder.ToTable("BlacklistOverrideRequests");
        builder.Property(e => e.Token).IsRequired();
        builder.Property(e => e.Status).IsRequired().HasMaxLength(20);
        builder.Property(e => e.Notes).HasMaxLength(500);

        builder.HasIndex(e => e.Token).IsUnique().HasDatabaseName("IX_BlacklistOverrideRequests_Token");
        builder.HasIndex(e => e.Status).HasDatabaseName("IX_BlacklistOverrideRequests_Status");
        builder.HasIndex(e => e.CreatedOn).HasDatabaseName("IX_BlacklistOverrideRequests_CreatedOn");

        builder.HasOne(e => e.Visitor).WithMany().HasForeignKey(e => e.VisitorId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.RequestedByUser).WithMany().HasForeignKey(e => e.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.ReviewedByUser).WithMany().HasForeignKey(e => e.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);

        // Mirror the User soft-delete filter so EF Core knows both sides are consistent
        // and does not warn about a required navigation potentially being filtered out.
        builder.HasQueryFilter(b => !b.RequestedByUser.IsDeleted);
    }
}
