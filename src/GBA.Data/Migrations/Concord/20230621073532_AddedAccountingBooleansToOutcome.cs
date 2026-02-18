using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddedAccountingBooleansToOutcome : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<bool>(
            "IsAccounting",
            "OutcomePaymentOrder",
            "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            "IsManagementAccounting",
            "OutcomePaymentOrder",
            "bit",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "IsAccounting",
            "OutcomePaymentOrder");

        migrationBuilder.DropColumn(
            "IsManagementAccounting",
            "OutcomePaymentOrder");
    }
}