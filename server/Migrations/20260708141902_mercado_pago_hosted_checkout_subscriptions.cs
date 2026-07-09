using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class mercado_pago_hosted_checkout_subscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "mp_preapproval_id",
                table: "subscriptions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_checkout_url",
                table: "subscription_charges",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_subscription_id",
                table: "subscription_charges",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "mp_preapproval_id",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "provider_checkout_url",
                table: "subscription_charges");

            migrationBuilder.DropColumn(
                name: "provider_subscription_id",
                table: "subscription_charges");
        }
    }
}
