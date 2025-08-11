using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for VisitorDocument
/// </summary>
public class VisitorDocumentConfiguration : AuditableEntityConfiguration<VisitorDocument>
{
    protected override void ConfigureAuditableEntity(EntityTypeBuilder<VisitorDocument> builder)
    {
        builder.ToTable("VisitorDocuments");

        // Basic properties
        builder.Property(d => d.DocumentName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.Description)
            .HasMaxLength(500);

        builder.Property(d => d.DocumentType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.OriginalFileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(d => d.StoredFileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(d => d.FilePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(d => d.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.FileExtension)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(d => d.FileHash)
            .HasMaxLength(100);

        builder.Property(d => d.Tags)
            .HasMaxLength(500);

        builder.Property(d => d.AccessLevel)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Standard");

        builder.Property(d => d.Version)
            .HasDefaultValue(1);

        // Foreign key
        builder.Property(d => d.VisitorId)
            .IsRequired();

        // Indexes
        builder.HasIndex(d => d.VisitorId)
            .HasDatabaseName("IX_VisitorDocuments_VisitorId");

        builder.HasIndex(d => d.DocumentType)
            .HasDatabaseName("IX_VisitorDocuments_DocumentType");

        builder.HasIndex(d => d.ExpirationDate)
            .HasDatabaseName("IX_VisitorDocuments_ExpirationDate");

        builder.HasIndex(d => d.FileHash)
            .HasDatabaseName("IX_VisitorDocuments_FileHash");

        // Foreign key relationship
        builder.HasOne(d => d.Visitor)
            .WithMany(v => v.Documents)
            .HasForeignKey(d => d.VisitorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
