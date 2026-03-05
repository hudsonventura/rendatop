using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class _01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ipcas",
                columns: table => new
                {
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    value = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ipcas", x => x.date);
                });

            migrationBuilder.CreateTable(
                name: "selics",
                columns: table => new
                {
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    value = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_selics", x => x.date);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    salt = table.Column<string>(type: "text", nullable: false),
                    password = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "investments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ownerid = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    bank = table.Column<string>(type: "text", nullable: false),
                    date_buy = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    date_expected_sell = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    value = table.Column<decimal>(type: "numeric", nullable: false),
                    index = table.Column<int>(type: "integer", nullable: false),
                    index_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    index_value = table.Column<decimal>(type: "numeric", nullable: false),
                    taxes = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_investments", x => x.id);
                    table.ForeignKey(
                        name: "FK_investments_users_ownerid",
                        column: x => x.ownerid,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "redemptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    investmentid = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    value = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_redemptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_redemptions_investments_investmentid",
                        column: x => x.investmentid,
                        principalTable: "investments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_investments_ownerid",
                table: "investments",
                column: "ownerid");

            migrationBuilder.CreateIndex(
                name: "IX_redemptions_investmentid",
                table: "redemptions",
                column: "investmentid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ipcas");

            migrationBuilder.DropTable(
                name: "redemptions");

            migrationBuilder.DropTable(
                name: "selics");

            migrationBuilder.DropTable(
                name: "investments");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
