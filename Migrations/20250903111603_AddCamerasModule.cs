using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VisitorManagementSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCamerasModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cameras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CameraType = table.Column<int>(type: "int", nullable: false),
                    ConnectionString = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Password = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    ConfigurationJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastHealthCheck = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastOnlineTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FailureCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    EnableFacialRecognition = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                    Manufacturer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FirmwareVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_Cameras", x => x.Id);
                    table.CheckConstraint("CK_Cameras_FailureCount", "[FailureCount] >= 0");
                    table.CheckConstraint("CK_Cameras_Priority", "[Priority] >= 1 AND [Priority] <= 10");
                    table.ForeignKey(
                        name: "FK_Camera_CreatedBy_User",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Camera_DeletedBy_User",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Camera_ModifiedBy_User",
                        column: x => x.ModifiedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cameras_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Camera_CreatedBy",
                table: "Cameras",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Camera_CreatedOn",
                table: "Cameras",
                column: "CreatedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Camera_DeletedBy",
                table: "Cameras",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Camera_IsActive_CreatedOn",
                table: "Cameras",
                columns: new[] { "IsActive", "CreatedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Camera_IsDeleted_DeletedOn",
                table: "Cameras",
                columns: new[] { "IsDeleted", "DeletedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Camera_ModifiedBy",
                table: "Cameras",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_CameraType",
                table: "Cameras",
                column: "CameraType");

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_CameraType_IsActive_IsDeleted",
                table: "Cameras",
                columns: new[] { "CameraType", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_EnableFacialRecognition",
                table: "Cameras",
                column: "EnableFacialRecognition");

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_EnableFacialRecognition_IsActive_IsDeleted",
                table: "Cameras",
                columns: new[] { "EnableFacialRecognition", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_FailureCount",
                table: "Cameras",
                column: "FailureCount");

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_HealthMonitoring",
                table: "Cameras",
                columns: new[] { "IsActive", "IsDeleted", "LastHealthCheck", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_IsActive",
                table: "Cameras",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_IsActive_IsDeleted_Status",
                table: "Cameras",
                columns: new[] { "IsActive", "IsDeleted", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_IsDeleted",
                table: "Cameras",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_LastHealthCheck",
                table: "Cameras",
                column: "LastHealthCheck");

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_LastOnlineTime",
                table: "Cameras",
                column: "LastOnlineTime");

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_LocationId",
                table: "Cameras",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_LocationId_Name_Unique",
                table: "Cameras",
                columns: new[] { "LocationId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_Name",
                table: "Cameras",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_Operational",
                table: "Cameras",
                columns: new[] { "IsActive", "IsDeleted", "Status", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_Priority",
                table: "Cameras",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_Status",
                table: "Cameras",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cameras");
        }
    }
}
