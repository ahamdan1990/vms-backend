using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagementSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCapacityManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TimeSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    MaxVisitors = table.Column<int>(type: "int", nullable: false, defaultValue: 50),
                    ActiveDays = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "1,2,3,4,5"),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    BufferMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 15),
                    AllowOverlapping = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeSlots", x => x.Id);
                    table.CheckConstraint("CK_TimeSlots_BufferMinutes", "[BufferMinutes] >= 0");
                    table.CheckConstraint("CK_TimeSlots_MaxVisitors", "[MaxVisitors] > 0");
                    table.ForeignKey(
                        name: "FK_TimeSlot_CreatedBy_User",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeSlot_DeletedBy_User",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeSlot_ModifiedBy_User",
                        column: x => x.ModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeSlots_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OccupancyLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TimeSlotId = table.Column<int>(type: "int", nullable: true),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    CurrentCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MaxCapacity = table.Column<int>(type: "int", nullable: false),
                    ReservedCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AvailableCapacity = table.Column<int>(type: "int", nullable: false, computedColumnSql: "[MaxCapacity] - [CurrentCount] - [ReservedCount]", stored: false),
                    OccupancyPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false, computedColumnSql: "CASE WHEN [MaxCapacity] > 0 THEN CAST(([CurrentCount] + [ReservedCount]) * 100.0 / [MaxCapacity] AS DECIMAL(5,2)) ELSE 0 END", stored: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OccupancyLogs", x => x.Id);
                    table.CheckConstraint("CK_OccupancyLogs_CurrentCount", "[CurrentCount] >= 0");
                    table.CheckConstraint("CK_OccupancyLogs_MaxCapacity", "[MaxCapacity] > 0");
                    table.CheckConstraint("CK_OccupancyLogs_ReservedCount", "[ReservedCount] >= 0");
                    table.ForeignKey(
                        name: "FK_OccupancyLogs_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OccupancyLogs_TimeSlots_TimeSlotId",
                        column: x => x.TimeSlotId,
                        principalTable: "TimeSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OccupancyLog_CreatedOn",
                table: "OccupancyLogs",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_OccupancyLog_IsActive",
                table: "OccupancyLogs",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_OccupancyLog_IsActive_CreatedOn",
                table: "OccupancyLogs",
                columns: new[] { "IsActive", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_OccupancyLogs_Date",
                table: "OccupancyLogs",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_OccupancyLogs_Date_Location",
                table: "OccupancyLogs",
                columns: new[] { "Date", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_OccupancyLogs_Date_TimeSlot",
                table: "OccupancyLogs",
                columns: new[] { "Date", "TimeSlotId" });

            migrationBuilder.CreateIndex(
                name: "IX_OccupancyLogs_LastUpdated",
                table: "OccupancyLogs",
                column: "LastUpdated");

            migrationBuilder.CreateIndex(
                name: "IX_OccupancyLogs_LocationId",
                table: "OccupancyLogs",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_OccupancyLogs_TimeSlotId",
                table: "OccupancyLogs",
                column: "TimeSlotId");

            migrationBuilder.CreateIndex(
                name: "UX_OccupancyLogs_Date_TimeSlot_Location",
                table: "OccupancyLogs",
                columns: new[] { "Date", "TimeSlotId", "LocationId" },
                unique: true,
                filter: "[TimeSlotId] IS NOT NULL AND [LocationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlot_CreatedBy",
                table: "TimeSlots",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlot_CreatedOn",
                table: "TimeSlots",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlot_DeletedBy",
                table: "TimeSlots",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlot_IsActive",
                table: "TimeSlots",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlot_IsActive_CreatedOn",
                table: "TimeSlots",
                columns: new[] { "IsActive", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlot_IsDeleted",
                table: "TimeSlots",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlot_IsDeleted_DeletedOn",
                table: "TimeSlots",
                columns: new[] { "IsDeleted", "DeletedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlot_ModifiedBy",
                table: "TimeSlots",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlots_Location_Time",
                table: "TimeSlots",
                columns: new[] { "LocationId", "StartTime", "EndTime" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlots_Name",
                table: "TimeSlots",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OccupancyLogs");

            migrationBuilder.DropTable(
                name: "TimeSlots");
        }
    }
}
