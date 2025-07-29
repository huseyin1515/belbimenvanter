using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BelbimEnv.Migrations
{
    /// <inheritdoc />
    public partial class portDetaylari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PortAdedi",
                table: "Servers",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PortAdedi",
                table: "Servers");
        }
    }
}
