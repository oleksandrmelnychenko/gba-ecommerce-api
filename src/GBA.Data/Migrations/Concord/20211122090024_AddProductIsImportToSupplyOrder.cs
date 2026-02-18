using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddProductIsImportToSupplyOrder : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<bool>(
            "ProductIsImported",
            "SupplyOrderItem",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            "ProductIsImported",
            "PackingListPackageOrderItem",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "ProductIsImported",
            "SupplyOrderItem");

        migrationBuilder.DropColumn(
            "ProductIsImported",
            "PackingListPackageOrderItem");
    }
}