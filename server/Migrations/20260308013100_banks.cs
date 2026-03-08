using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class banks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "banks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<int>(type: "integer", nullable: false),
                    cnpj = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    company_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_banks", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "banks");
        }
    }
}
