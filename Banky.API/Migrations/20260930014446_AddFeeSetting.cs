using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Banky.API.Migrations
{
    /// <inheritdoc />
    public partial class AddFeeSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fee_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    operation_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    currency_code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    name_ar = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    fee_type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    percentage = table.Column<decimal>(type: "decimal(8,4)", nullable: false),
                    fixed_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    min_fee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    max_fee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fee_settings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fee_settings_operation_type_currency_code",
                table: "fee_settings",
                columns: new[] { "operation_type", "currency_code" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fee_settings");
        }
    }
}
