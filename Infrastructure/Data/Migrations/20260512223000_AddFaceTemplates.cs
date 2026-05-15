using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VisitorManagementSystem.Api.Infrastructure.Data;

#nullable disable

namespace VisitorManagementSystem.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260512223000_AddFaceTemplates")]
    public partial class AddFaceTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FaceTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PersonId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Engine = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TemplateData = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    TemplateSize = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    QualityScore = table.Column<decimal>(type: "decimal(6,3)", nullable: true),
                    SourceImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ExternalImageId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LastMatchedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MatchCount = table.Column<int>(type: "int", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaceTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaceTemplate_CreatedBy_User",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FaceTemplate_DeletedBy_User",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FaceTemplate_ModifiedBy_User",
                        column: x => x.ModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FaceTemplate_CreatedBy",
                table: "FaceTemplates",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FaceTemplate_CreatedOn",
                table: "FaceTemplates",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_FaceTemplate_DeletedBy",
                table: "FaceTemplates",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FaceTemplate_IsActive",
                table: "FaceTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_FaceTemplate_IsActive_CreatedOn",
                table: "FaceTemplates",
                columns: new[] { "IsActive", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_FaceTemplate_IsDeleted",
                table: "FaceTemplates",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_FaceTemplate_IsDeleted_DeletedOn",
                table: "FaceTemplates",
                columns: new[] { "IsDeleted", "DeletedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_FaceTemplate_ModifiedBy",
                table: "FaceTemplates",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FaceTemplates_Engine_Active",
                table: "FaceTemplates",
                columns: new[] { "Engine", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_FaceTemplates_Identity_Engine",
                table: "FaceTemplates",
                columns: new[] { "PersonType", "PersonId", "Engine", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_FaceTemplates_Subject_Engine",
                table: "FaceTemplates",
                columns: new[] { "SubjectId", "Engine", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FaceTemplates");
        }
    }
}
