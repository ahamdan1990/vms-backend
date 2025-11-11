using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VisitorManagementSystem.Api.Domain.Entities;

namespace VisitorManagementSystem.Api.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Department
/// </summary>
public class DepartmentConfiguration : SoftDeleteEntityConfiguration<Department>
{
    protected override void ConfigureSoftDeleteEntity(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");

        // Basic properties
        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.Description)
            .HasMaxLength(500);

        builder.Property(d => d.Email)
            .HasMaxLength(256);

        builder.Property(d => d.Phone)
            .HasMaxLength(20);

        builder.Property(d => d.Location)
            .HasMaxLength(100);

        builder.Property(d => d.Budget)
            .HasColumnType("decimal(18,2)")
            .IsRequired(false);

        builder.Property(d => d.DisplayOrder)
            .HasDefaultValue(0);

        // Manager relationship
        builder.HasOne(d => d.Manager)
            .WithMany()
            .HasForeignKey(d => d.ManagerId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("FK_Departments_Manager_User");

        builder.Property(d => d.ManagerId)
            .IsRequired(false);

        // Parent department relationship (self-referencing for hierarchy)
        builder.HasOne(d => d.ParentDepartment)
            .WithMany(d => d.ChildDepartments)
            .HasForeignKey(d => d.ParentDepartmentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Departments_ParentDepartment");

        builder.Property(d => d.ParentDepartmentId)
            .IsRequired(false);

        // Users relationship
        builder.HasMany(d => d.Users)
            .WithOne(u => u.DepartmentEntity)
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("FK_Users_Department");

        // Indexes
        builder.HasIndex(d => d.Code)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_Departments_Code_Unique");

        builder.HasIndex(d => d.Name)
            .HasDatabaseName("IX_Departments_Name");

        builder.HasIndex(d => d.ManagerId)
            .HasDatabaseName("IX_Departments_ManagerId");

        builder.HasIndex(d => d.ParentDepartmentId)
            .HasDatabaseName("IX_Departments_ParentDepartmentId");

        builder.HasIndex(d => d.IsDeleted)
            .HasDatabaseName("IX_Departments_IsDeleted");

        builder.HasIndex(d => d.DisplayOrder)
            .HasDatabaseName("IX_Departments_DisplayOrder");

        builder.HasIndex(d => new { d.ParentDepartmentId, d.IsDeleted })
            .HasDatabaseName("IX_Departments_ParentDepartmentId_IsDeleted");
    }
}
