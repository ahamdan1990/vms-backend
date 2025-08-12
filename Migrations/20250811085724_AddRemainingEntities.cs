using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagementSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRemainingEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "VisitPurposes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "VisitPurposes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOn",
                table: "VisitPurposes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "VisitPurposes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "Locations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "Locations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedOn",
                table: "Locations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Locations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_VisitPurposes_DeletedByUserId",
                table: "VisitPurposes",
                column: "DeletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_DeletedByUserId",
                table: "Locations",
                column: "DeletedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Locations_Users_DeletedByUserId",
                table: "Locations",
                column: "DeletedByUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VisitPurposes_Users_DeletedByUserId",
                table: "VisitPurposes",
                column: "DeletedByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Locations_Users_DeletedByUserId",
                table: "Locations");

            migrationBuilder.DropForeignKey(
                name: "FK_VisitPurposes_Users_DeletedByUserId",
                table: "VisitPurposes");

            migrationBuilder.DropIndex(
                name: "IX_VisitPurposes_DeletedByUserId",
                table: "VisitPurposes");

            migrationBuilder.DropIndex(
                name: "IX_Locations_DeletedByUserId",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "VisitPurposes");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "VisitPurposes");

            migrationBuilder.DropColumn(
                name: "DeletedOn",
                table: "VisitPurposes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "VisitPurposes");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "DeletedOn",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Locations");
        }
    }
}
