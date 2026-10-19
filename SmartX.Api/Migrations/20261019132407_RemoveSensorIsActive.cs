using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartX.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSensorIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Sensors");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Sensors",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
