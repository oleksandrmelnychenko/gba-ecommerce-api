using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddUnitPriceEurWithVarToPackingListItem : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<decimal>(
            "UnitPriceEurWithVat",
            "PackingListPackageOrderItem",
            "decimal(30,14)",
            nullable: false,
            defaultValue: 0m);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "UnitPriceEurWithVat",
            "PackingListPackageOrderItem");
    }
}