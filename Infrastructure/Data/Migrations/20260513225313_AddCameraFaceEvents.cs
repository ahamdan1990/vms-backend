using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagementSystem.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCameraFaceEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CameraFaceEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CameraId = table.Column<int>(type: "int", nullable: false),
                    CameraName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    CapturedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsKnown = table.Column<bool>(type: "bit", nullable: false),
                    PersonType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PersonId = table.Column<int>(type: "int", nullable: true),
                    SubjectId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Similarity = table.Column<float>(type: "real", nullable: true),
                    Confidence = table.Column<float>(type: "real", nullable: true),
                    SnapshotPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BoundingBoxX = table.Column<int>(type: "int", nullable: true),
                    BoundingBoxY = table.Column<int>(type: "int", nullable: true),
                    BoundingBoxWidth = table.Column<int>(type: "int", nullable: true),
                    BoundingBoxHeight = table.Column<int>(type: "int", nullable: true),
                    SuggestedAction = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReviewStatus = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ReviewedById = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsKnownCandidate = table.Column<bool>(type: "bit", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AlertId = table.Column<int>(type: "int", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CameraFaceEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CameraFaceEvents_Cameras_CameraId",
                        column: x => x.CameraId,
                        principalTable: "Cameras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CameraFaceEvents_CameraId_CapturedAt",
                table: "CameraFaceEvents",
                columns: new[] { "CameraId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CameraFaceEvents_EventId",
                table: "CameraFaceEvents",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CameraFaceEvents_ExpiresAt",
                table: "CameraFaceEvents",
                column: "ExpiresAt",
                filter: "[ExpiresAt] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CameraFaceEvents_KnownCandidates",
                table: "CameraFaceEvents",
                columns: new[] { "PersonId", "PersonType", "IsKnownCandidate" },
                filter: "[PersonId] IS NOT NULL AND [IsKnownCandidate] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_CameraFaceEvents_ReviewStatus_CapturedAt",
                table: "CameraFaceEvents",
                columns: new[] { "ReviewStatus", "CapturedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CameraFaceEvents");
        }
    }
}
