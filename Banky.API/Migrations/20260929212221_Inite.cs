using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Banky.API.Migrations
{
    /// <inheritdoc />
    public partial class Inite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    full_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    profile_image = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    kyc_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    kyc_id_front = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    kyc_id_back = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    kyc_rejection_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    kyc_submitted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    kyc_reviewed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    hide_full_name = table.Column<bool>(type: "bit", nullable: false),
                    hide_phone_on_pos = table.Column<bool>(type: "bit", nullable: false),
                    pos_alias_phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    is_biometric_enabled = table.Column<bool>(type: "bit", nullable: false),
                    must_change_password = table.Column<bool>(type: "bit", nullable: false),
                    reset_password_token = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    reset_password_expiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_blocked = table.Column<bool>(type: "bit", nullable: false),
                    blocked_message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "currencies",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    name_ar = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    name_en = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    symbol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    exchange_rate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_currencies", x => x.id);
                    table.UniqueConstraint("AK_currencies_code", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    client_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    fcm_token = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    device_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    device_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    main_device = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_devices", x => x.id);
                    table.ForeignKey(
                        name: "FK_devices_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pos_points",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    client_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    pos_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pos_points", x => x.id);
                    table.ForeignKey(
                        name: "FK_pos_points_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wallets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    client_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    account_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    currency_code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    balance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallets", x => x.id);
                    table.ForeignKey(
                        name: "FK_wallets_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_wallets_currencies_currency_code",
                        column: x => x.currency_code,
                        principalTable: "currencies",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    transaction_number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    sender_client_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    receiver_client_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    pos_point_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    currency_code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    fee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    total_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    sender_display_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    sender_display_phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    receiver_display_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_transactions_clients_receiver_client_id",
                        column: x => x.receiver_client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transactions_clients_sender_client_id",
                        column: x => x.sender_client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transactions_pos_points_pos_point_id",
                        column: x => x.pos_point_id,
                        principalTable: "pos_points",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_clients_email",
                table: "clients",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_clients_kyc_status",
                table: "clients",
                column: "kyc_status");

            migrationBuilder.CreateIndex(
                name: "IX_clients_phone",
                table: "clients",
                column: "phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_currencies_code",
                table: "currencies",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_devices_client_id",
                table: "devices",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "IX_pos_points_client_id",
                table: "pos_points",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "IX_pos_points_pos_code",
                table: "pos_points",
                column: "pos_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transactions_created_at",
                table: "transactions",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_pos_point_id",
                table: "transactions",
                column: "pos_point_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_receiver_client_id",
                table: "transactions",
                column: "receiver_client_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_sender_client_id",
                table: "transactions",
                column: "sender_client_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_transaction_number",
                table: "transactions",
                column: "transaction_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transactions_type",
                table: "transactions",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "IX_wallets_account_number",
                table: "wallets",
                column: "account_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wallets_client_id",
                table: "wallets",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "IX_wallets_currency_code",
                table: "wallets",
                column: "currency_code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "devices");

            migrationBuilder.DropTable(
                name: "transactions");

            migrationBuilder.DropTable(
                name: "wallets");

            migrationBuilder.DropTable(
                name: "pos_points");

            migrationBuilder.DropTable(
                name: "currencies");

            migrationBuilder.DropTable(
                name: "clients");
        }
    }
}
