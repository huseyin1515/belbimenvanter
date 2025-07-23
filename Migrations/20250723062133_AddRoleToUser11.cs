using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BelbimEnv.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleToUser11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Servers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HostDns = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAdress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Model = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServiceTag = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VcenterAdress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cluster = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IloIdracIp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Kabin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RearFront = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KabinU = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsttelkomEtiketId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PortDetaylari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServerId = table.Column<int>(type: "int", nullable: false),
                    PortTipi = table.Column<int>(type: "int", nullable: false),
                    LinkStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LinkSpeed = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PortId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NicId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FiberMAC = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BakirMAC = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Wwpn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SwName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SwPort = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SwdeUcMi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UcPort = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BakirUplinkPort = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FcUcPortSayisi = table.Column<int>(type: "int", nullable: true),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortDetaylari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortDetaylari_Servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PortDetaylari_ServerId",
                table: "PortDetaylari",
                column: "ServerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortDetaylari");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Servers");
        }
    }
}
