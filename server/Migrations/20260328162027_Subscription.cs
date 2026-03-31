using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace server.Migrations
{
    /// <inheritdoc />
    public partial class Subscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "subscription_charges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<string>(type: "text", nullable: false),
                    payment_method = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    payer_cpf = table.Column<string>(type: "text", nullable: false),
                    provider_payment_id = table.Column<string>(type: "text", nullable: true),
                    provider_external_reference = table.Column<string>(type: "text", nullable: true),
                    provider_status_detail = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    charge_kind = table.Column<string>(type: "text", nullable: false),
                    billing_period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    billing_period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    due_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reminder_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    receipt_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    pix_qr_code = table.Column<string>(type: "text", nullable: true),
                    pix_qr_code_base64 = table.Column<string>(type: "text", nullable: true),
                    boleto_barcode_content = table.Column<string>(type: "text", nullable: true),
                    boleto_barcode_image_base64 = table.Column<string>(type: "text", nullable: true),
                    boleto_digitable_line = table.Column<string>(type: "text", nullable: true),
                    boleto_url = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_charges", x => x.id);
                    table.ForeignKey(
                        name: "FK_subscription_charges_subscriptions_subscription_id",
                        column: x => x.subscription_id,
                        principalTable: "subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_subscription_charges_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_charges_subscription_id",
                table: "subscription_charges",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "IX_subscription_charges_user_id",
                table: "subscription_charges",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "subscription_charges");
        }
    }
}
