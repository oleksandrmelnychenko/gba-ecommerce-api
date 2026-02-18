using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddTotalPaymentAmountToReSale : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<decimal>(
            "TotalPaymentAmount",
            "ReSale",
            "decimal(30,14)",
            nullable: false,
            defaultValue: 0m);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "TotalPaymentAmount",
            "ReSale");
    }
}