using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartX.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSensorAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SensorAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SensorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", nullable: false),
                    StoredFileName = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    AttachmentType = table.Column<string>(type: "TEXT", nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SensorAttachments_Sensors_SensorId",
                        column: x => x.SensorId,
                        principalTable: "Sensors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SensorAttachments_SensorId",
                table: "SensorAttachments",
                column: "SensorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SensorAttachments");
        }
    }
}
