using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddedTypeOfOperationToOutcomePaymentOrder : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<int>(
            "OperationType",
            "OutcomePaymentOrder",
            "int",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "OperationType",
            "OutcomePaymentOrder");
    }
}