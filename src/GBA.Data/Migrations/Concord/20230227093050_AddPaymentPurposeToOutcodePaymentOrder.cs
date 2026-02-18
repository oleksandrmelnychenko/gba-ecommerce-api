using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddPaymentPurposeToOutcodePaymentOrder : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<string>(
            "PaymentPurpose",
            "OutcomePaymentOrder",
            "nvarchar(150)",
            maxLength: 150,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "PaymentPurpose",
            "OutcomePaymentOrder");
    }
}