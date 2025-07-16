using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BelbimEnv.Migrations
{
    /// <inheritdoc />
    public partial class AddServerAndDetayServerTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DetayServers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    LinkStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LinkSpeed = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PortID = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NIC_ID = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FiberMAC = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BakirMAC = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WWPN = table.Column<string>(type: "nvarchar(max)", nullable: false)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetayServers");
        }
    }
}
