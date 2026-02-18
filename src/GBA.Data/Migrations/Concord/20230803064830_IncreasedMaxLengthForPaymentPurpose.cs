using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class IncreasedMaxLengthForPaymentPurpose : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AlterColumn<string>(
            "PaymentPurpose",
            "OutcomePaymentOrder",
            "nvarchar(500)",
            maxLength: 500,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(150)",
            oldMaxLength: 150,
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.AlterColumn<string>(
            "PaymentPurpose",
            "OutcomePaymentOrder",
            "nvarchar(150)",
            maxLength: 150,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(500)",
            oldMaxLength: 500,
            oldNullable: true);
    }
}