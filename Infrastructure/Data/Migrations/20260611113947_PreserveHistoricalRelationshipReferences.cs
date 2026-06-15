using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagementSystem.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PreserveHistoricalRelationshipReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CameraFaceEvents_Cameras_CameraId",
                table: "CameraFaceEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffPresences_Users_CheckedInById",
                table: "StaffPresences");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffPresences_Users_UserId",
                table: "StaffPresences");

            migrationBuilder.DropForeignKey(
                name: "FK_TemporaryLeaves_Users_RecordedById",
                table: "TemporaryLeaves");

            migrationBuilder.DropIndex(
                name: "IX_TemporaryLeaves_RecordedById",
                table: "TemporaryLeaves");

            migrationBuilder.DropIndex(
                name: "IX_StaffPresences_CheckedInById",
                table: "StaffPresences");

            migrationBuilder.AddColumn<int>(
                name: "RecordedByReferenceId",
                table: "TemporaryLeaves",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CheckedInByReferenceId",
                table: "StaffPresences",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserReferenceId",
                table: "StaffPresences",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CameraReferenceId",
                table: "CameraFaceEvents",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("UPDATE [TemporaryLeaves] SET [RecordedByReferenceId] = [RecordedById] WHERE [RecordedByReferenceId] IS NULL;");
            migrationBuilder.Sql("UPDATE [StaffPresences] SET [CheckedInByReferenceId] = [CheckedInById], [UserReferenceId] = [UserId] WHERE [CheckedInByReferenceId] IS NULL OR [UserReferenceId] IS NULL;");
            migrationBuilder.Sql("UPDATE [CameraFaceEvents] SET [CameraReferenceId] = [CameraId] WHERE [CameraReferenceId] IS NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_TemporaryLeaves_RecordedByReferenceId",
                table: "TemporaryLeaves",
                column: "RecordedByReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffPresences_CheckedInByReferenceId",
                table: "StaffPresences",
                column: "CheckedInByReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffPresences_UserReferenceId",
                table: "StaffPresences",
                column: "UserReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_CameraFaceEvents_CameraReferenceId",
                table: "CameraFaceEvents",
                column: "CameraReferenceId");

            migrationBuilder.AddForeignKey(
                name: "FK_CameraFaceEvents_Cameras_CameraReferenceId",
                table: "CameraFaceEvents",
                column: "CameraReferenceId",
                principalTable: "Cameras",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffPresences_Users_CheckedInByReferenceId",
                table: "StaffPresences",
                column: "CheckedInByReferenceId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffPresences_Users_UserReferenceId",
                table: "StaffPresences",
                column: "UserReferenceId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TemporaryLeaves_Users_RecordedByReferenceId",
                table: "TemporaryLeaves",
                column: "RecordedByReferenceId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CameraFaceEvents_Cameras_CameraReferenceId",
                table: "CameraFaceEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffPresences_Users_CheckedInByReferenceId",
                table: "StaffPresences");

            migrationBuilder.DropForeignKey(
                name: "FK_StaffPresences_Users_UserReferenceId",
                table: "StaffPresences");

            migrationBuilder.DropForeignKey(
                name: "FK_TemporaryLeaves_Users_RecordedByReferenceId",
                table: "TemporaryLeaves");

            migrationBuilder.DropIndex(
                name: "IX_TemporaryLeaves_RecordedByReferenceId",
                table: "TemporaryLeaves");

            migrationBuilder.DropIndex(
                name: "IX_StaffPresences_CheckedInByReferenceId",
                table: "StaffPresences");

            migrationBuilder.DropIndex(
                name: "IX_StaffPresences_UserReferenceId",
                table: "StaffPresences");

            migrationBuilder.DropIndex(
                name: "IX_CameraFaceEvents_CameraReferenceId",
                table: "CameraFaceEvents");

            migrationBuilder.DropColumn(
                name: "RecordedByReferenceId",
                table: "TemporaryLeaves");

            migrationBuilder.DropColumn(
                name: "CheckedInByReferenceId",
                table: "StaffPresences");

            migrationBuilder.DropColumn(
                name: "UserReferenceId",
                table: "StaffPresences");

            migrationBuilder.DropColumn(
                name: "CameraReferenceId",
                table: "CameraFaceEvents");

            migrationBuilder.CreateIndex(
                name: "IX_TemporaryLeaves_RecordedById",
                table: "TemporaryLeaves",
                column: "RecordedById");

            migrationBuilder.CreateIndex(
                name: "IX_StaffPresences_CheckedInById",
                table: "StaffPresences",
                column: "CheckedInById");

            migrationBuilder.AddForeignKey(
                name: "FK_CameraFaceEvents_Cameras_CameraId",
                table: "CameraFaceEvents",
                column: "CameraId",
                principalTable: "Cameras",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffPresences_Users_CheckedInById",
                table: "StaffPresences",
                column: "CheckedInById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StaffPresences_Users_UserId",
                table: "StaffPresences",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TemporaryLeaves_Users_RecordedById",
                table: "TemporaryLeaves",
                column: "RecordedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
