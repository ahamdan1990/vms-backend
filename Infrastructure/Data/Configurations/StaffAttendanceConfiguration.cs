using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

public class StaffAttendanceConfiguration : IEntityTypeConfiguration<StaffAttendance>
{
    public void Configure(EntityTypeBuilder<StaffAttendance> builder)
    {
        builder.ToTable("StaffAttendances");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.CheckInTime).IsRequired();
        builder.Property(x => x.CheckOutTime);
        builder.Property(x => x.Method).IsRequired();
        builder.Property(x => x.CameraId);
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Camera)
            .WithMany()
            .HasForeignKey(x => x.CameraId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.CheckInTime);
        builder.HasIndex(x => new { x.UserId, x.CheckOutTime })
            .HasDatabaseName("IX_StaffAttendances_UserId_CheckOutTime");
    }
}
