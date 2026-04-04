using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class investment_type : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "investment_type",
                table: "recurring_investments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "investment_type",
                table: "investments",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "investment_type",
                table: "recurring_investments");

            migrationBuilder.DropColumn(
                name: "investment_type",
                table: "investments");
        }
    }
}
