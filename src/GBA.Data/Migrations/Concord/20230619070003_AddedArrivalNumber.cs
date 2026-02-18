using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddedArrivalNumber : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<string>(
            "ArrivalNumber",
            "OutcomePaymentOrder",
            "nvarchar(100)",
            maxLength: 100,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "ArrivalNumber",
            "OutcomePaymentOrder");
    }
}