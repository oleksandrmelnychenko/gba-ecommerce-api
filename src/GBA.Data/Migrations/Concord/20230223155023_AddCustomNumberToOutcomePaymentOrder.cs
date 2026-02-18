using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddCustomNumberToOutcomePaymentOrder : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<string>(
            "CustomNumber",
            "OutcomePaymentOrder",
            "nvarchar(50)",
            maxLength: 50,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "CustomNumber",
            "OutcomePaymentOrder");
    }
}