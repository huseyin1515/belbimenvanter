using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BelbimEnv.Migrations
{
    /// <inheritdoc />
    public partial class AddPortDetayTableWithNewStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WWPN",
                table: "PortDetaylari",
                newName: "Wwpn");

            migrationBuilder.RenameColumn(
                name: "PortID",
                table: "PortDetaylari",
                newName: "PortId");

            migrationBuilder.RenameColumn(
                name: "SW_PORT",
                table: "PortDetaylari",
                newName: "UcPort");

            migrationBuilder.RenameColumn(
                name: "SW_NAME",
                table: "PortDetaylari",
                newName: "SwdeUcMi");

            migrationBuilder.RenameColumn(
                name: "NIC_ID",
                table: "PortDetaylari",
                newName: "SwPort");

            migrationBuilder.AlterColumn<int>(
                name: "PortTipi",
                table: "PortDetaylari",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "BakirUplinkPort",
                table: "PortDetaylari",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FcUcPortSayisi",
                table: "PortDetaylari",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NicId",
                table: "PortDetaylari",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SwName",
                table: "PortDetaylari",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BakirUplinkPort",
                table: "PortDetaylari");

            migrationBuilder.DropColumn(
                name: "FcUcPortSayisi",
                table: "PortDetaylari");

            migrationBuilder.DropColumn(
                name: "NicId",
                table: "PortDetaylari");

            migrationBuilder.DropColumn(
                name: "SwName",
                table: "PortDetaylari");

            migrationBuilder.RenameColumn(
                name: "Wwpn",
                table: "PortDetaylari",
                newName: "WWPN");

            migrationBuilder.RenameColumn(
                name: "PortId",
                table: "PortDetaylari",
                newName: "PortID");

            migrationBuilder.RenameColumn(
                name: "UcPort",
                table: "PortDetaylari",
                newName: "SW_PORT");

            migrationBuilder.RenameColumn(
                name: "SwdeUcMi",
                table: "PortDetaylari",
                newName: "SW_NAME");

            migrationBuilder.RenameColumn(
                name: "SwPort",
                table: "PortDetaylari",
                newName: "NIC_ID");

            migrationBuilder.AlterColumn<string>(
                name: "PortTipi",
                table: "PortDetaylari",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
