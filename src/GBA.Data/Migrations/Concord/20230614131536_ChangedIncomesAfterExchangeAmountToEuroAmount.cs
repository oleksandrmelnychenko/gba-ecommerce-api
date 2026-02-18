using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class ChangedIncomesAfterExchangeAmountToEuroAmount : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.RenameColumn(
            "AfterExchangeAmount",
            "IncomePaymentOrder",
            "EuroAmount");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.RenameColumn(
            "EuroAmount",
            "IncomePaymentOrder",
            "AfterExchangeAmount");
    }
}