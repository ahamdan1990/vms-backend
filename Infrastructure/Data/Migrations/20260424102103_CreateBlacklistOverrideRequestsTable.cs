using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagementSystem.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateBlacklistOverrideRequestsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlacklistOverrideRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VisitorId = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlacklistOverrideRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlacklistOverrideRequests_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BlacklistOverrideRequests_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BlacklistOverrideRequests_Visitors_VisitorId",
                        column: x => x.VisitorId,
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistOverrideRequests_CreatedOn",
                table: "BlacklistOverrideRequests",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistOverrideRequests_RequestedByUserId",
                table: "BlacklistOverrideRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistOverrideRequests_ReviewedByUserId",
                table: "BlacklistOverrideRequests",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistOverrideRequests_Status",
                table: "BlacklistOverrideRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistOverrideRequests_Token",
                table: "BlacklistOverrideRequests",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistOverrideRequests_VisitorId",
                table: "BlacklistOverrideRequests",
                column: "VisitorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlacklistOverrideRequests");
        }
    }
}
