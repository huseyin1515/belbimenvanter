using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BelbimEnv.Migrations
{
    /// <inheritdoc />
    public partial class AddPortCountsToDetayServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BakirPortAdet",
                table: "DetayServers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FCPortAdet",
                table: "DetayServers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SANPortAdet",
                table: "DetayServers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VirtualPortAdet",
                table: "DetayServers",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BakirPortAdet",
                table: "DetayServers");

            migrationBuilder.DropColumn(
                name: "FCPortAdet",
                table: "DetayServers");

            migrationBuilder.DropColumn(
                name: "SANPortAdet",
                table: "DetayServers");

            migrationBuilder.DropColumn(
                name: "VirtualPortAdet",
                table: "DetayServers");
        }
    }
}
