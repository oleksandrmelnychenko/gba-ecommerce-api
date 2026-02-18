using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class IncomePaymentOrderNewProps : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<string>(
            "ArrivalNumber",
            "IncomePaymentOrder",
            maxLength: 450,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            "OperationType",
            "IncomePaymentOrder",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            "PaymentPurpose",
            "IncomePaymentOrder",
            maxLength: 450,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "ArrivalNumber",
            "IncomePaymentOrder");

        migrationBuilder.DropColumn(
            "OperationType",
            "IncomePaymentOrder");

        migrationBuilder.DropColumn(
            "PaymentPurpose",
            "IncomePaymentOrder");
    }
}