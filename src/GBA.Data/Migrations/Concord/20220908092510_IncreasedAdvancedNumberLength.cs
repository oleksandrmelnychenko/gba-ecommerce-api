using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class IncreasedAdvancedNumberLength : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AlterColumn<string>(
            "AdvanceNumber",
            "OutcomePaymentOrder",
            "nvarchar(50)",
            maxLength: 50,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(6)",
            oldMaxLength: 6,
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.AlterColumn<string>(
            "AdvanceNumber",
            "OutcomePaymentOrder",
            "nvarchar(6)",
            maxLength: 6,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(50)",
            oldMaxLength: 50,
            oldNullable: true);
    }
}