using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagementSystem.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFaceEventRejectionMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "AppliedThreshold",
                table: "CameraFaceEvents",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BestCandidatePersonId",
                table: "CameraFaceEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BestCandidatePersonType",
                table: "CameraFaceEvents",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "BestCandidateSimilarity",
                table: "CameraFaceEvents",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "CameraFaceEvents",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppliedThreshold",
                table: "CameraFaceEvents");

            migrationBuilder.DropColumn(
                name: "BestCandidatePersonId",
                table: "CameraFaceEvents");

            migrationBuilder.DropColumn(
                name: "BestCandidatePersonType",
                table: "CameraFaceEvents");

            migrationBuilder.DropColumn(
                name: "BestCandidateSimilarity",
                table: "CameraFaceEvents");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "CameraFaceEvents");
        }
    }
}
