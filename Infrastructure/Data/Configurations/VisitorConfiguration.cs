using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Visitor
/// </summary>
public class VisitorConfiguration : AuditableEntityConfiguration<Visitor>
{
    protected override void ConfigureAuditableEntity(EntityTypeBuilder<Visitor> builder)
    {
        builder.ToTable("Visitors");

        // Basic properties
        builder.Property(v => v.FirstName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(v => v.LastName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(v => v.NormalizedEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(v => v.Company)
            .HasMaxLength(100);

        builder.Property(v => v.JobTitle)
            .HasMaxLength(100);

        builder.Property(v => v.GovernmentId)
            .HasMaxLength(50);

        builder.Property(v => v.GovernmentIdType)
            .HasMaxLength(30);

        builder.Property(v => v.Nationality)
            .HasMaxLength(50);

        builder.Property(v => v.Language)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("en-US");

        builder.Property(v => v.ProfilePhotoPath)
            .HasMaxLength(500);

        builder.Property(v => v.DietaryRequirements)
            .HasMaxLength(500);

        builder.Property(v => v.AccessibilityRequirements)
            .HasMaxLength(500);

        builder.Property(v => v.SecurityClearance)
            .HasMaxLength(50);

        builder.Property(v => v.BlacklistReason)
            .HasMaxLength(500);

        builder.Property(v => v.Notes)
            .HasMaxLength(1000);

        builder.Property(v => v.ExternalId)
            .HasMaxLength(100);

        // Email value object configuration
        builder.OwnsOne(v => v.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("Email")
                .IsRequired()
                .HasMaxLength(256);
        });

        // Phone number value object configuration
        builder.OwnsOne(v => v.PhoneNumber, phone =>
        {
            phone.Property(p => p.Value)
                .HasColumnName("PhoneNumber")
                .HasMaxLength(20);

            phone.Property(p => p.FormattedValue)
                .HasColumnName("PhoneNumberFormatted")
                .HasMaxLength(25);

            phone.Property(p => p.DigitsOnly)
                .HasColumnName("PhoneNumberDigitsOnly")
                .HasMaxLength(15);

            phone.Property(p => p.CountryCode)
                .HasColumnName("PhoneCountryCode")
                .HasMaxLength(4);

            phone.Property(p => p.AreaCode)
                .HasColumnName("PhoneAreaCode")
                .HasMaxLength(4);

            phone.Property(p => p.PhoneType)
                .HasColumnName("PhoneType")
                .HasMaxLength(10);

            phone.Property(p => p.IsVerified)
                .HasColumnName("PhoneIsVerified");
        });

        // Address value object configuration
        builder.OwnsOne(v => v.Address, address =>
        {
            address.Property(a => a.Street1)
                .HasColumnName("AddressStreet1")
                .HasMaxLength(100);

            address.Property(a => a.Street2)
                .HasColumnName("AddressStreet2")
                .HasMaxLength(100);

            address.Property(a => a.City)
                .HasColumnName("AddressCity")
                .HasMaxLength(50);

            address.Property(a => a.State)
                .HasColumnName("AddressState")
                .HasMaxLength(50);

            address.Property(a => a.PostalCode)
                .HasColumnName("AddressPostalCode")
                .HasMaxLength(20);

            address.Property(a => a.Country)
                .HasColumnName("AddressCountry")
                .HasMaxLength(50);

            address.Property(a => a.Latitude)
                .HasColumnName("AddressLatitude")
                .HasPrecision(18, 6);

            address.Property(a => a.Longitude)
                .HasColumnName("AddressLongitude")
                .HasPrecision(18, 6);

            address.Property(a => a.AddressType)
                .HasColumnName("AddressType")
                .HasMaxLength(20);

            address.Property(a => a.IsValidated)
                .HasColumnName("AddressIsValidated");
        });

        // Indexes
        builder.HasIndex(v => v.NormalizedEmail)
            .HasDatabaseName("IX_Visitors_NormalizedEmail");

        builder.HasIndex(v => v.GovernmentId)
            .HasDatabaseName("IX_Visitors_GovernmentId");

        builder.HasIndex(v => v.Company)
            .HasDatabaseName("IX_Visitors_Company");

        builder.HasIndex(v => v.IsVip)
            .HasDatabaseName("IX_Visitors_IsVip");

        builder.HasIndex(v => v.IsBlacklisted)
            .HasDatabaseName("IX_Visitors_IsBlacklisted");

        builder.HasIndex(v => v.LastVisitDate)
            .HasDatabaseName("IX_Visitors_LastVisitDate");

        // Foreign key relationships
        builder.HasOne(v => v.BlacklistedByUser)
            .WithMany()
            .HasForeignKey(v => v.BlacklistedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Navigation properties
        builder.HasMany(v => v.Documents)
            .WithOne(d => d.Visitor)
            .HasForeignKey(d => d.VisitorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.VisitorNotes)
            .WithOne(n => n.Visitor)
            .HasForeignKey(n => n.VisitorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.EmergencyContacts)
            .WithOne(c => c.Visitor)
            .HasForeignKey(c => c.VisitorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
