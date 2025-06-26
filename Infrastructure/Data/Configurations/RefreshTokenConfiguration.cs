using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for RefreshToken
/// </summary>
public class RefreshTokenConfiguration : BaseEntityConfiguration<RefreshToken>
{
    protected override void ConfigureDerivedEntity(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        // Token properties
        builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(rt => rt.JwtId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(rt => rt.ExpiryDate)
            .IsRequired();

        // Status properties
        builder.Property(rt => rt.IsUsed)
            .HasDefaultValue(false);

        builder.Property(rt => rt.IsRevoked)
            .HasDefaultValue(false);

        builder.Property(rt => rt.RevocationReason)
            .HasMaxLength(200);

        // IP and device properties
        builder.Property(rt => rt.CreatedByIp)
            .HasMaxLength(45);

        builder.Property(rt => rt.RevokedByIp)
            .HasMaxLength(45);

        builder.Property(rt => rt.UserAgent)
            .HasMaxLength(500);

        builder.Property(rt => rt.DeviceFingerprint)
            .HasMaxLength(100);

        // Relationships
        builder.Property(rt => rt.UserId)
            .IsRequired();

        builder.HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Token replacement relationship
        builder.HasOne(rt => rt.ReplacedByToken)
            .WithOne(rt => rt.ReplacesToken)
            .HasForeignKey<RefreshToken>(rt => rt.ReplacedByTokenId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(rt => rt.Token)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_Token_Unique");

        builder.HasIndex(rt => rt.JwtId)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_JwtId_Unique");

        builder.HasIndex(rt => rt.UserId)
            .HasDatabaseName("IX_RefreshTokens_UserId");

        builder.HasIndex(rt => rt.ExpiryDate)
            .HasDatabaseName("IX_RefreshTokens_ExpiryDate");

        builder.HasIndex(rt => rt.IsUsed)
            .HasDatabaseName("IX_RefreshTokens_IsUsed");

        builder.HasIndex(rt => rt.IsRevoked)
            .HasDatabaseName("IX_RefreshTokens_IsRevoked");

        builder.HasIndex(rt => rt.DeviceFingerprint)
            .HasDatabaseName("IX_RefreshTokens_DeviceFingerprint");

        builder.HasIndex(rt => rt.CreatedByIp)
            .HasDatabaseName("IX_RefreshTokens_CreatedByIp");

        builder.HasIndex(rt => new { rt.UserId, rt.IsActive, rt.ExpiryDate })
            .HasDatabaseName("IX_RefreshTokens_UserId_IsActive_ExpiryDate");

        builder.HasIndex(rt => new { rt.IsActive, rt.IsUsed, rt.IsRevoked, rt.ExpiryDate })
            .HasDatabaseName("IX_RefreshTokens_Status_ExpiryDate");
    }
}