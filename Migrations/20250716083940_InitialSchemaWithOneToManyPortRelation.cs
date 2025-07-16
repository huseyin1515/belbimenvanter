using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BelbimEnv.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchemaWithOneToManyPortRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetayServers");

            migrationBuilder.CreateTable(
                name: "PortDetaylari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PortTipi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Adres = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LinkStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LinkSpeed = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServerId = table.Column<int>(type: "int", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "DetayServers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    BakirMAC = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BakirPortAdet = table.Column<int>(type: "int", nullable: true),
                    FCPortAdet = table.Column<int>(type: "int", nullable: true),
                    FiberMAC = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LinkSpeed = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LinkStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NIC_ID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PortID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SANPortAdet = table.Column<int>(type: "int", nullable: true),
                    VirtualPortAdet = table.Column<int>(type: "int", nullable: true),
                    WWPN = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetayServers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetayServers_Servers_Id",
                        column: x => x.Id,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
