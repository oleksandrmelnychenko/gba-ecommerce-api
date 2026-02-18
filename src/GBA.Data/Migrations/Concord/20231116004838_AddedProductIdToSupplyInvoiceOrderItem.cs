using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddedProductIdToSupplyInvoiceOrderItem : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<long>(
            "ProductID",
            "SupplyInvoiceOrderItem",
            "bigint",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.CreateIndex(
            "IX_SupplyInvoiceOrderItem_ProductID",
            "SupplyInvoiceOrderItem",
            "ProductID");

        migrationBuilder.AddForeignKey(
            "FK_SupplyInvoiceOrderItem_Product_ProductID",
            "SupplyInvoiceOrderItem",
            "ProductID",
            "Product",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_SupplyInvoiceOrderItem_Product_ProductID",
            "SupplyInvoiceOrderItem");

        migrationBuilder.DropIndex(
            "IX_SupplyInvoiceOrderItem_ProductID",
            "SupplyInvoiceOrderItem");

        migrationBuilder.DropColumn(
            "ProductID",
            "SupplyInvoiceOrderItem");
    }
}