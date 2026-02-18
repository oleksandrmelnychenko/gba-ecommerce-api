using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class ConsumableItemTotalPriceWithVat : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<decimal>(
            "TotalPriceWithVAT",
            "ConsumablesOrderItem",
            "money",
            nullable: false,
            defaultValue: 0m);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "TotalPriceWithVAT",
            "ConsumablesOrderItem");
    }
}