using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class bank_fk_relation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bank",
                table: "investments");

            migrationBuilder.AddColumn<Guid>(
                name: "bank_id",
                table: "investments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_investments_bank_id",
                table: "investments",
                column: "bank_id");

            migrationBuilder.AddForeignKey(
                name: "FK_investments_banks_bank_id",
                table: "investments",
                column: "bank_id",
                principalTable: "banks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_investments_banks_bank_id",
                table: "investments");

            migrationBuilder.DropIndex(
                name: "IX_investments_bank_id",
                table: "investments");

            migrationBuilder.DropColumn(
                name: "bank_id",
                table: "investments");

            migrationBuilder.AddColumn<string>(
                name: "bank",
                table: "investments",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
