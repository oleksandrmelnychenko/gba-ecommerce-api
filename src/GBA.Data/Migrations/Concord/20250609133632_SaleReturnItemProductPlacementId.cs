using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class SaleReturnItemProductPlacementId : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<long>(
            "ProductPlacementID",
            "SaleReturnItem",
            "bigint",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            "HasPromotionalPricing",
            "Agreement",
            "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateIndex(
            "IX_SaleReturnItem_ProductPlacementID",
            "SaleReturnItem",
            "ProductPlacementID");

        migrationBuilder.AddForeignKey(
            "FK_SaleReturnItem_ProductPlacement_ProductPlacementID",
            "SaleReturnItem",
            "ProductPlacementID",
            "ProductPlacement",
            principalColumn: "ID");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_SaleReturnItem_ProductPlacement_ProductPlacementID",
            "SaleReturnItem");

        migrationBuilder.DropIndex(
            "IX_SaleReturnItem_ProductPlacementID",
            "SaleReturnItem");

        migrationBuilder.DropColumn(
            "ProductPlacementID",
            "SaleReturnItem");

        migrationBuilder.DropColumn(
            "HasPromotionalPricing",
            "Agreement");
    }
}