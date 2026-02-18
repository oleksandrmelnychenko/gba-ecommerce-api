using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class RemoveProductIsImportedFromSupplyOrderItem : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "ProductIsImported",
            "SupplyOrderItem");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<bool>(
            "ProductIsImported",
            "SupplyOrderItem",
            nullable: false,
            defaultValue: false);
    }
}