using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BelbimEnv.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleToUser1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VirtualMachines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SN = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VmName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Dns = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VipIp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MachineIp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Port = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MachineName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Network = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cluster = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Host = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Pool = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VirtualMachines", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VirtualMachines");
        }
    }
}
