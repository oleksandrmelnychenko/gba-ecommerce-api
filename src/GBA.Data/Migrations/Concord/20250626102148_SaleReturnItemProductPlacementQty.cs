using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class SaleReturnItemProductPlacementQty : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<double>(
            "Qty",
            "SaleReturnItemProductPlacement",
            "float",
            nullable: false,
            defaultValue: 0.0);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "Qty",
            "SaleReturnItemProductPlacement");
    }
}