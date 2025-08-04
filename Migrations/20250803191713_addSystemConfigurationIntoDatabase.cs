using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagementSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class addSystemConfigurationIntoDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequiresRestart = table.Column<bool>(type: "bit", nullable: false),
                    IsEncrypted = table.Column<bool>(type: "bit", nullable: false),
                    IsReadOnly = table.Column<bool>(type: "bit", nullable: false),
                    IsSensitive = table.Column<bool>(type: "bit", nullable: false),
                    DefaultValue = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ValidationRules = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    MinValue = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    MaxValue = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AllowedValues = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Group = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Environment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "All"),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemConfigurations_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SystemConfigurations_Users_ModifiedBy",
                        column: x => x.ModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConfigurationAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SystemConfigurationId = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsAutomated = table.Column<bool>(type: "bit", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedBy = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfigurationAudits_SystemConfigurations_SystemConfigurationId",
                        column: x => x.SystemConfigurationId,
                        principalTable: "SystemConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConfigurationAudits_Users_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfigurationAudits_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfigurationAudits_Users_ModifiedBy",
                        column: x => x.ModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationAudits_Action",
                table: "ConfigurationAudits",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationAudits_ApprovedBy",
                table: "ConfigurationAudits",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationAudits_Category_Key",
                table: "ConfigurationAudits",
                columns: new[] { "Category", "Key" });

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationAudits_CreatedBy",
                table: "ConfigurationAudits",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationAudits_CreatedOn",
                table: "ConfigurationAudits",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationAudits_IsApproved",
                table: "ConfigurationAudits",
                column: "IsApproved");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationAudits_IsAutomated",
                table: "ConfigurationAudits",
                column: "IsAutomated");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationAudits_ModifiedBy",
                table: "ConfigurationAudits",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationAudits_RequiresApproval",
                table: "ConfigurationAudits",
                column: "RequiresApproval");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationAudits_SystemConfigurationId",
                table: "ConfigurationAudits",
                column: "SystemConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_Category",
                table: "SystemConfigurations",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_Category_Key",
                table: "SystemConfigurations",
                columns: new[] { "Category", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_CreatedBy",
                table: "SystemConfigurations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_Environment",
                table: "SystemConfigurations",
                column: "Environment");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_Group",
                table: "SystemConfigurations",
                column: "Group");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_IsEncrypted",
                table: "SystemConfigurations",
                column: "IsEncrypted");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_ModifiedBy",
                table: "SystemConfigurations",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigurations_RequiresRestart",
                table: "SystemConfigurations",
                column: "RequiresRestart");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfigurationAudits");

            migrationBuilder.DropTable(
                name: "SystemConfigurations");
        }
    }
}
