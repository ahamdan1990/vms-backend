using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagementSystem.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TimeSlotId",
                table: "Invitations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TimeSlotBookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TimeSlotId = table.Column<int>(type: "int", nullable: false),
                    BookingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InvitationId = table.Column<int>(type: "int", nullable: true),
                    VisitorCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BookedBy = table.Column<int>(type: "int", nullable: false),
                    BookedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CancelledOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledBy = table.Column<int>(type: "int", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_TimeSlotBookings", x => x.Id);
                    table.CheckConstraint("CK_TimeSlotBookings_VisitorCount", "[VisitorCount] > 0");
                    table.ForeignKey(
                        name: "FK_TimeSlotBooking_CreatedBy_User",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeSlotBooking_DeletedBy_User",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeSlotBooking_ModifiedBy_User",
                        column: x => x.ModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeSlotBookings_Invitations_InvitationId",
                        column: x => x.InvitationId,
                        principalTable: "Invitations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TimeSlotBookings_TimeSlots_TimeSlotId",
                        column: x => x.TimeSlotId,
                        principalTable: "TimeSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeSlotBookings_Users_BookedBy",
                        column: x => x.BookedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeSlotBookings_Users_CancelledBy",
                        column: x => x.CancelledBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_TimeSlotId",
                table: "Invitations",
                column: "TimeSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBooking_CreatedBy",
                table: "TimeSlotBookings",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBooking_CreatedOn",
                table: "TimeSlotBookings",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBooking_DeletedBy",
                table: "TimeSlotBookings",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBooking_IsActive",
                table: "TimeSlotBookings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBooking_IsActive_CreatedOn",
                table: "TimeSlotBookings",
                columns: new[] { "IsActive", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBooking_IsDeleted",
                table: "TimeSlotBookings",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBooking_IsDeleted_DeletedOn",
                table: "TimeSlotBookings",
                columns: new[] { "IsDeleted", "DeletedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBooking_ModifiedBy",
                table: "TimeSlotBookings",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBookings_Availability",
                table: "TimeSlotBookings",
                columns: new[] { "TimeSlotId", "BookingDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBookings_BookedBy",
                table: "TimeSlotBookings",
                column: "BookedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBookings_BookingDate",
                table: "TimeSlotBookings",
                column: "BookingDate");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBookings_CancelledBy",
                table: "TimeSlotBookings",
                column: "CancelledBy");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBookings_InvitationId",
                table: "TimeSlotBookings",
                column: "InvitationId",
                unique: true,
                filter: "[InvitationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBookings_Status",
                table: "TimeSlotBookings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlotBookings_TimeSlotId",
                table: "TimeSlotBookings",
                column: "TimeSlotId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_TimeSlots_TimeSlotId",
                table: "Invitations",
                column: "TimeSlotId",
                principalTable: "TimeSlots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_TimeSlots_TimeSlotId",
                table: "Invitations");

            migrationBuilder.DropTable(
                name: "TimeSlotBookings");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_TimeSlotId",
                table: "Invitations");

            migrationBuilder.DropColumn(
                name: "TimeSlotId",
                table: "Invitations");
        }
    }
}
