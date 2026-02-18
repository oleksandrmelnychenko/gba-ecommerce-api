using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddProductIsImportToSupplyInvoiceOrderItem : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<bool>(
            "ProductIsImported",
            "SupplyInvoiceOrderItem",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "ProductIsImported",
            "SupplyInvoiceOrderItem");
    }
}