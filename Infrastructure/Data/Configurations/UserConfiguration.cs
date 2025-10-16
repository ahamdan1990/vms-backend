using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;
using VisitorManagementSystem.Api.Domain.ValueObjects;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for User
/// </summary>
public class UserConfiguration : AuditableEntityConfiguration<User>
{
    protected override void ConfigureAuditableEntity(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        // Basic properties
        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(50);

        // Email value object configuration
        builder.OwnsOne(u => u.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("Email")
                .IsRequired()
                .HasMaxLength(256);
        });

        builder.Property(u => u.NormalizedEmail)
            .IsRequired()
            .HasMaxLength(256);

        // Phone number value object configuration
        builder.OwnsOne(u => u.PhoneNumber, phone =>
        {
            phone.Property(p => p.Value)
                .HasColumnName("PhoneRaw")
                .IsRequired()
                .HasMaxLength(20);

            phone.Property(p => p.FormattedValue)
                .HasColumnName("PhoneFormatted")
                .HasMaxLength(25);

            phone.Property(p => p.DigitsOnly)
                .HasColumnName("PhoneDigitsOnly")
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
        builder.OwnsOne(u => u.Address, address =>
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
                .HasColumnName("AddressLatitude");

            address.Property(a => a.Longitude)
                .HasColumnName("AddressLongitude");

            address.Property(a => a.AddressType)
                .HasColumnName("AddressType")
                .HasMaxLength(20);

            address.Property(a => a.IsValidated)
                .HasColumnName("AddressIsValidated");
        });


        // Security properties
        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(u => u.PasswordSalt)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(u => u.SecurityStamp)
            .IsRequired()
            .HasMaxLength(100);

        // Role and status
        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(u => u.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Optional properties
        builder.Property(u => u.Department)
            .HasMaxLength(100);

        builder.Property(u => u.JobTitle)
            .HasMaxLength(100);

        builder.Property(u => u.EmployeeId)
            .HasMaxLength(50);

        builder.Property(u => u.ProfilePhotoPath)
            .HasMaxLength(500);

        builder.Property(u => u.TimeZone)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("UTC");

        builder.Property(u => u.Language)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("en-US");

        builder.Property(u => u.Theme)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("light");

        // Lockout properties
        builder.Property(u => u.FailedLoginAttempts)
            .HasDefaultValue(0);

        builder.Property(u => u.IsLockedOut)
            .HasDefaultValue(false);

        builder.Property(u => u.MustChangePassword)
            .HasDefaultValue(false);

        // Soft delete properties
        builder.Property(u => u.IsDeleted)
            .HasDefaultValue(false);

        // Relationships
        builder.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.CreatedAuditLogs)
            .WithOne(al => al.User)
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Deleted by relationship
        builder.HasOne(u => u.DeletedByUser)
            .WithMany()
            .HasForeignKey(u => u.DeletedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(u => u.NormalizedEmail)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Users_NormalizedEmail_Unique");

        builder.HasIndex(u => u.EmployeeId)
            .IsUnique()
            .HasFilter("[EmployeeId] IS NOT NULL AND [IsDeleted] = 0")
            .HasDatabaseName("IX_Users_EmployeeId_Unique");

        builder.HasIndex(u => u.Role)
            .HasDatabaseName("IX_Users_Role");

        builder.HasIndex(u => u.Status)
            .HasDatabaseName("IX_Users_Status");

        builder.HasIndex(u => u.Department)
            .HasDatabaseName("IX_Users_Department");

        builder.HasIndex(u => u.IsDeleted)
            .HasDatabaseName("IX_Users_IsDeleted");

        builder.HasIndex(u => u.LastLoginDate)
            .HasDatabaseName("IX_Users_LastLoginDate");

        builder.HasIndex(u => new { u.IsActive, u.Status, u.IsDeleted })
            .HasDatabaseName("IX_Users_IsActive_Status_IsDeleted");

        // Security indexes
        builder.HasIndex(u => u.IsLockedOut)
            .HasDatabaseName("IX_Users_IsLockedOut");

        builder.HasIndex(u => u.FailedLoginAttempts)
            .HasDatabaseName("IX_Users_FailedLoginAttempts");

        builder.HasIndex(u => u.LockoutEnd)
            .HasDatabaseName("IX_Users_LockoutEnd");

        builder.HasIndex(u => u.PasswordChangedDate)
            .HasDatabaseName("IX_Users_PasswordChangedDate");
    }
}