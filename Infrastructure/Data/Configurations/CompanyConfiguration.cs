using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Company
/// </summary>
public class CompanyConfiguration : SoftDeleteEntityConfiguration<Company>
{
    protected override void ConfigureSoftDeleteEntity(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");

        // Basic properties
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Website)
            .HasMaxLength(500);

        builder.Property(c => c.Industry)
            .HasMaxLength(100);

        builder.Property(c => c.TaxId)
            .HasMaxLength(50);

        builder.Property(c => c.ContactPersonName)
            .HasMaxLength(100);

        // Email value object configuration
        builder.OwnsOne(c => c.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("ContactEmail")
                .HasMaxLength(256);
        });

        // Phone number value object configuration
        builder.OwnsOne(c => c.PhoneNumber, phone =>
        {
            phone.Property(p => p.Value)
                .HasColumnName("ContactPhoneRaw")
                .HasMaxLength(20);

            phone.Property(p => p.FormattedValue)
                .HasColumnName("ContactPhoneFormatted")
                .HasMaxLength(25);

            phone.Property(p => p.DigitsOnly)
                .HasColumnName("ContactPhoneDigitsOnly")
                .HasMaxLength(15);

            phone.Property(p => p.CountryCode)
                .HasColumnName("ContactPhoneCountryCode")
                .HasMaxLength(4);

            phone.Property(p => p.AreaCode)
                .HasColumnName("ContactPhoneAreaCode")
                .HasMaxLength(4);

            phone.Property(p => p.PhoneType)
                .HasColumnName("ContactPhoneType")
                .HasMaxLength(10);

            phone.Property(p => p.IsVerified)
                .HasColumnName("ContactPhoneIsVerified");
        });

        // Address value object configuration
        builder.OwnsOne(c => c.Address, address =>
        {
            address.Property(a => a.Street1)
                .HasColumnName("CompanyAddressStreet1")
                .HasMaxLength(100);

            address.Property(a => a.Street2)
                .HasColumnName("CompanyAddressStreet2")
                .HasMaxLength(100);

            address.Property(a => a.City)
                .HasColumnName("CompanyAddressCity")
                .HasMaxLength(50);

            address.Property(a => a.State)
                .HasColumnName("CompanyAddressState")
                .HasMaxLength(50);

            address.Property(a => a.PostalCode)
                .HasColumnName("CompanyAddressPostalCode")
                .HasMaxLength(20);

            address.Property(a => a.Country)
                .HasColumnName("CompanyAddressCountry")
                .HasMaxLength(50);

            address.Property(a => a.Latitude)
                .HasColumnName("CompanyAddressLatitude");

            address.Property(a => a.Longitude)
                .HasColumnName("CompanyAddressLongitude");

            address.Property(a => a.AddressType)
                .HasColumnName("CompanyAddressType")
                .HasMaxLength(20);

            address.Property(a => a.IsValidated)
                .HasColumnName("CompanyAddressIsValidated");
        });

        // Additional properties
        builder.Property(c => c.EmployeeCount)
            .IsRequired(false);

        builder.Property(c => c.LogoPath)
            .HasMaxLength(500);

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        // Verification properties
        builder.Property(c => c.IsVerified)
            .HasDefaultValue(false);

        builder.Property(c => c.VerifiedOn)
            .IsRequired(false);

        builder.Property(c => c.VerifiedBy)
            .IsRequired(false);

        // Blacklist properties
        builder.Property(c => c.BlacklistReason)
            .HasMaxLength(500);

        builder.Property(c => c.BlacklistedOn)
            .IsRequired(false);

        builder.Property(c => c.BlacklistedBy)
            .IsRequired(false);

        // Display and count properties
        builder.Property(c => c.DisplayOrder)
            .HasDefaultValue(0);

        builder.Property(c => c.VisitorCount)
            .HasDefaultValue(0);

        // Relationships (Visitors relationship is configured in VisitorConfiguration)
        builder.HasMany(c => c.Visitors)
            .WithOne(v => v.CompanyEntity)
            .HasForeignKey(v => v.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.VerifiedBy)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Companies_VerifiedBy_User");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.BlacklistedBy)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Companies_BlacklistedBy_User");

        // Indexes
        builder.HasIndex(c => c.Code)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Companies_Code_Unique");

        builder.HasIndex(c => c.Name)
            .HasDatabaseName("IX_Companies_Name");

        builder.HasIndex(c => c.IsVerified)
            .HasDatabaseName("IX_Companies_IsVerified");

        builder.HasIndex(c => c.IsDeleted)
            .HasDatabaseName("IX_Companies_IsDeleted");

        builder.HasIndex(c => c.DisplayOrder)
            .HasDatabaseName("IX_Companies_DisplayOrder");

        builder.HasIndex(c => new { c.IsVerified, c.IsDeleted })
            .HasDatabaseName("IX_Companies_IsVerified_IsDeleted");
    }
}
