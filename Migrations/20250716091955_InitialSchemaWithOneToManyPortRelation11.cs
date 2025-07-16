using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BelbimEnv.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchemaWithOneToManyPortRelation11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Adres",
                table: "PortDetaylari",
                newName: "WWPN");

            migrationBuilder.AddColumn<string>(
                name: "BakirMAC",
                table: "PortDetaylari",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FiberMAC",
                table: "PortDetaylari",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NIC_ID",
                table: "PortDetaylari",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PortID",
                table: "PortDetaylari",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SW_NAME",
                table: "PortDetaylari",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SW_PORT",
                table: "PortDetaylari",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BakirMAC",
                table: "PortDetaylari");

            migrationBuilder.DropColumn(
                name: "FiberMAC",
                table: "PortDetaylari");

            migrationBuilder.DropColumn(
                name: "NIC_ID",
                table: "PortDetaylari");

            migrationBuilder.DropColumn(
                name: "PortID",
                table: "PortDetaylari");

            migrationBuilder.DropColumn(
                name: "SW_NAME",
                table: "PortDetaylari");

            migrationBuilder.DropColumn(
                name: "SW_PORT",
                table: "PortDetaylari");

            migrationBuilder.RenameColumn(
                name: "WWPN",
                table: "PortDetaylari",
                newName: "Adres");
        }
    }
}
