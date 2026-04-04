using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class boxes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "money_box_id",
                table: "investments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "money_boxes",
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
                    table.PrimaryKey("PK_money_boxes", x => x.id);
                    table.ForeignKey(
                        name: "FK_money_boxes_users_owner_id",
                        column: x => x.owner_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_investments_money_box_id",
                table: "investments",
                column: "money_box_id");

            migrationBuilder.CreateIndex(
                name: "IX_money_boxes_owner_id",
                table: "money_boxes",
                column: "owner_id");

            migrationBuilder.AddForeignKey(
                name: "FK_investments_money_boxes_money_box_id",
                table: "investments",
                column: "money_box_id",
                principalTable: "money_boxes",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_investments_money_boxes_money_box_id",
                table: "investments");

            migrationBuilder.DropTable(
                name: "money_boxes");

            migrationBuilder.DropIndex(
                name: "IX_investments_money_box_id",
                table: "investments");

            migrationBuilder.DropColumn(
                name: "money_box_id",
                table: "investments");
        }
    }
}
