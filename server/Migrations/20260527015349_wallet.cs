using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class wallet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "wallet_id",
                table: "recurring_investments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "wallet_id",
                table: "investments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "wallets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallets", x => x.id);
                    table.ForeignKey(
                        name: "FK_wallets_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_recurring_investments_wallet_id",
                table: "recurring_investments",
                column: "wallet_id");

            migrationBuilder.CreateIndex(
                name: "IX_investments_wallet_id",
                table: "investments",
                column: "wallet_id");

            migrationBuilder.CreateIndex(
                name: "IX_wallets_owner_id",
                table: "wallets",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_wallets_owner_id_name",
                table: "wallets",
                columns: new[] { "owner_id", "name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_investments_wallets_wallet_id",
                table: "investments",
                column: "wallet_id",
                principalTable: "wallets",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_recurring_investments_wallets_wallet_id",
                table: "recurring_investments",
                column: "wallet_id",
                principalTable: "wallets",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_investments_wallets_wallet_id",
                table: "investments");

            migrationBuilder.DropForeignKey(
                name: "FK_recurring_investments_wallets_wallet_id",
                table: "recurring_investments");

            migrationBuilder.DropTable(
                name: "wallets");

            migrationBuilder.DropIndex(
                name: "IX_recurring_investments_wallet_id",
                table: "recurring_investments");

            migrationBuilder.DropIndex(
                name: "IX_investments_wallet_id",
                table: "investments");

            migrationBuilder.DropColumn(
                name: "wallet_id",
                table: "recurring_investments");

            migrationBuilder.DropColumn(
                name: "wallet_id",
                table: "investments");
        }
    }
}
