using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

public class FaceTemplateConfiguration : SoftDeleteEntityConfiguration<FaceTemplate>
{
    protected override void ConfigureSoftDeleteEntity(EntityTypeBuilder<FaceTemplate> builder)
    {
        builder.ToTable("FaceTemplates");

        builder.Property(t => t.PersonType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(t => t.SubjectId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(t => t.Engine)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(t => t.TemplateData)
            .IsRequired();

        builder.Property(t => t.SourceImagePath)
            .HasMaxLength(500);

        builder.Property(t => t.Source)
            .HasMaxLength(64);

        builder.Property(t => t.ExternalImageId)
            .HasMaxLength(128);

        builder.Property(t => t.QualityScore)
            .HasColumnType("decimal(6,3)");

        builder.Property(t => t.Metadata)
            .HasColumnType("nvarchar(max)");

        builder.HasOne(t => t.Visitor)
            .WithMany()
            .HasForeignKey(t => t.VisitorId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_FaceTemplates_Visitors_VisitorId");

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_FaceTemplates_Users_UserId");

        builder.HasIndex(t => new { t.PersonType, t.PersonId, t.Engine, t.IsDeleted })
            .HasDatabaseName("IX_FaceTemplates_Identity_Engine");

        builder.HasIndex(t => t.VisitorId)
            .HasDatabaseName("IX_FaceTemplates_VisitorId");

        builder.HasIndex(t => t.UserId)
            .HasDatabaseName("IX_FaceTemplates_UserId");

        builder.HasIndex(t => new { t.SubjectId, t.Engine, t.IsDeleted })
            .HasDatabaseName("IX_FaceTemplates_Subject_Engine");

        builder.HasIndex(t => new { t.Engine, t.IsActive, t.IsDeleted })
            .HasDatabaseName("IX_FaceTemplates_Engine_Active");
    }
}
