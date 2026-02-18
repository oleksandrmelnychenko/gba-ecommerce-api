using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class ChangedSaleReturnItemAmountType : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AlterColumn<decimal>(
            "Amount",
            "SaleReturnItem",
            "decimal(30,14)",
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "money");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.AlterColumn<decimal>(
            "Amount",
            "SaleReturnItem",
            "money",
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "decimal(30,14)");
    }
}