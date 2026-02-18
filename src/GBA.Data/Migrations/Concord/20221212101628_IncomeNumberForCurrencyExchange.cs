using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class IncomeNumberForCurrencyExchange : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<string>(
            "IncomeNumber",
            "PaymentRegisterCurrencyExchange",
            "nvarchar(150)",
            maxLength: 150,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "IncomeNumber",
            "PaymentRegisterCurrencyExchange");
    }
}