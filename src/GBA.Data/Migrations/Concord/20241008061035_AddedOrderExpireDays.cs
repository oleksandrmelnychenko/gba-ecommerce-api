using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddedOrderExpireDays : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<int>(
            "OrderExpireDays",
            "ClientTypeRole",
            "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            "OrderExpireDays",
            "Client",
            "int",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "OrderExpireDays",
            "ClientTypeRole");

        migrationBuilder.DropColumn(
            "OrderExpireDays",
            "Client");
    }
}