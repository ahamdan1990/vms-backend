using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for EmergencyContact
/// </summary>
public class EmergencyContactConfiguration : AuditableEntityConfiguration<EmergencyContact>
{
    protected override void ConfigureAuditableEntity(EntityTypeBuilder<EmergencyContact> builder)
    {
        builder.ToTable("EmergencyContacts");

        // Basic properties
        builder.Property(c => c.FirstName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.LastName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Relationship)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Priority)
            .HasDefaultValue(1);

        builder.Property(c => c.Notes)
            .HasMaxLength(500);

        // Foreign key
        builder.Property(c => c.VisitorId)
            .IsRequired();

        // Primary phone number value object configuration
        builder.OwnsOne(c => c.PhoneNumber, phone =>
        {
            phone.Property(p => p.Value)
                .HasColumnName("PhoneNumber")
                .IsRequired()
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

        // Alternate phone number value object configuration
        builder.OwnsOne(c => c.AlternatePhoneNumber, phone =>
        {
            phone.Property(p => p.Value)
                .HasColumnName("AlternatePhoneNumber")
                .HasMaxLength(20);

            phone.Property(p => p.FormattedValue)
                .HasColumnName("AlternatePhoneNumberFormatted")
                .HasMaxLength(25);

            phone.Property(p => p.DigitsOnly)
                .HasColumnName("AlternatePhoneNumberDigitsOnly")
                .HasMaxLength(15);

            phone.Property(p => p.CountryCode)
                .HasColumnName("AlternatePhoneCountryCode")
                .HasMaxLength(4);

            phone.Property(p => p.AreaCode)
                .HasColumnName("AlternatePhoneAreaCode")
                .HasMaxLength(4);

            phone.Property(p => p.PhoneType)
                .HasColumnName("AlternatePhoneType")
                .HasMaxLength(10);

            phone.Property(p => p.IsVerified)
                .HasColumnName("AlternatePhoneIsVerified");
        });

        // Email value object configuration
        builder.OwnsOne(c => c.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("Email")
                .HasMaxLength(256);
        });

        // Address value object configuration
        builder.OwnsOne(c => c.Address, address =>
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
        builder.HasIndex(c => c.VisitorId)
            .HasDatabaseName("IX_EmergencyContacts_VisitorId");

        builder.HasIndex(c => c.Priority)
            .HasDatabaseName("IX_EmergencyContacts_Priority");

        builder.HasIndex(c => c.IsPrimary)
            .HasDatabaseName("IX_EmergencyContacts_IsPrimary");

        // Foreign key relationship
        builder.HasOne(c => c.Visitor)
            .WithMany(v => v.EmergencyContacts)
            .HasForeignKey(c => c.VisitorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
