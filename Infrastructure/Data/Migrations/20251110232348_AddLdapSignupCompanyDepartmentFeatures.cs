using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagementSystem.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLdapSignupCompanyDepartmentFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Visitors",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationToken",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationTokenExpiry",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerifiedOn",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLdapUser",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLdapSyncOn",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LdapDistinguishedName",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Website = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Industry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TaxId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContactPersonName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ContactPhoneRaw = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ContactPhoneFormatted = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    ContactPhoneDigitsOnly = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    ContactPhoneCountryCode = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    ContactPhoneAreaCode = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    ContactPhoneType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ContactPhoneIsVerified = table.Column<bool>(type: "bit", nullable: false),
                    CompanyAddressStreet1 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CompanyAddressStreet2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CompanyAddressCity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CompanyAddressState = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CompanyAddressPostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CompanyAddressCountry = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CompanyAddressLatitude = table.Column<double>(type: "float", nullable: true),
                    CompanyAddressLongitude = table.Column<double>(type: "float", nullable: true),
                    CompanyAddressType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CompanyAddressIsValidated = table.Column<bool>(type: "bit", nullable: true),
                    EmployeeCount = table.Column<int>(type: "int", nullable: true),
                    LogoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    VerifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedBy = table.Column<int>(type: "int", nullable: true),
                    BlacklistReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BlacklistedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BlacklistedBy = table.Column<int>(type: "int", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    VisitorCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Companies_BlacklistedBy_User",
                        column: x => x.BlacklistedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Companies_VerifiedBy_User",
                        column: x => x.VerifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Company_CreatedBy_User",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Company_DeletedBy_User",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Company_ModifiedBy_User",
                        column: x => x.ModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ManagerId = table.Column<int>(type: "int", nullable: true),
                    ParentDepartmentId = table.Column<int>(type: "int", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Budget = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Department_CreatedBy_User",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Department_DeletedBy_User",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Department_ModifiedBy_User",
                        column: x => x.ModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Departments_Manager_User",
                        column: x => x.ManagerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Departments_ParentDepartment",
                        column: x => x.ParentDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_CompanyId",
                table: "Visitors",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DepartmentId",
                table: "Users",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_BlacklistedBy",
                table: "Companies",
                column: "BlacklistedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Code_Unique",
                table: "Companies",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_DisplayOrder",
                table: "Companies",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_IsDeleted",
                table: "Companies",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_IsVerified",
                table: "Companies",
                column: "IsVerified");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_IsVerified_IsDeleted",
                table: "Companies",
                columns: new[] { "IsVerified", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Name",
                table: "Companies",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_VerifiedBy",
                table: "Companies",
                column: "VerifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Company_CreatedBy",
                table: "Companies",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Company_CreatedOn",
                table: "Companies",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Company_DeletedBy",
                table: "Companies",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Company_IsActive",
                table: "Companies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Company_IsActive_CreatedOn",
                table: "Companies",
                columns: new[] { "IsActive", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Company_IsDeleted_DeletedOn",
                table: "Companies",
                columns: new[] { "IsDeleted", "DeletedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Company_ModifiedBy",
                table: "Companies",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Department_CreatedBy",
                table: "Departments",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Department_CreatedOn",
                table: "Departments",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Department_DeletedBy",
                table: "Departments",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Department_IsActive",
                table: "Departments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Department_IsActive_CreatedOn",
                table: "Departments",
                columns: new[] { "IsActive", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Department_IsDeleted_DeletedOn",
                table: "Departments",
                columns: new[] { "IsDeleted", "DeletedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Department_ModifiedBy",
                table: "Departments",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Code_Unique",
                table: "Departments",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_DisplayOrder",
                table: "Departments",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_IsDeleted",
                table: "Departments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ManagerId",
                table: "Departments",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Name",
                table: "Departments",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ParentDepartmentId",
                table: "Departments",
                column: "ParentDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ParentDepartmentId_IsDeleted",
                table: "Departments",
                columns: new[] { "ParentDepartmentId", "IsDeleted" });

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Department",
                table: "Users",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Visitors_Company",
                table: "Visitors",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Department",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Visitors_Company",
                table: "Visitors");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Visitors_CompanyId",
                table: "Visitors");

            migrationBuilder.DropIndex(
                name: "IX_Users_DepartmentId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Visitors");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationTokenExpiry",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailVerifiedOn",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsLdapUser",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastLdapSyncOn",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LdapDistinguishedName",
                table: "Users");
        }
    }
}
