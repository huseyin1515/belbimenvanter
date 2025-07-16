using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BelbimEnv.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
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
                    KabinU = table.Column<int>(type: "int", nullable: true),
                    IsttelkomEtiketId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servers", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Servers");
        }
    }
}
