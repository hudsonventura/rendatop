using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class recurring_investments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "recurring_investments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bank_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<decimal>(type: "numeric", nullable: false),
                    index = table.Column<int>(type: "integer", nullable: false),
                    index_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    index_value = table.Column<decimal>(type: "numeric", nullable: false),
                    taxes = table.Column<bool>(type: "boolean", nullable: false),
                    liquidity_daily = table.Column<bool>(type: "boolean", nullable: false),
                    duration_days = table.Column<int>(type: "integer", nullable: true),
                    frequency = table.Column<int>(type: "integer", nullable: false),
                    weekdays = table.Column<short[]>(type: "smallint[]", nullable: false),
                    day_of_month = table.Column<int>(type: "integer", nullable: true),
                    months_csv = table.Column<string>(type: "text", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    last_generated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recurring_investments", x => x.id);
                    table.ForeignKey(
                        name: "FK_recurring_investments_banks_bank_id",
                        column: x => x.bank_id,
                        principalTable: "banks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_recurring_investments_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_recurring_investments_bank_id",
                table: "recurring_investments",
                column: "bank_id");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_investments_owner_id",
                table: "recurring_investments",
                column: "owner_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "recurring_investments");
        }
    }
}
