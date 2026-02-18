using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddedIsManagementForResale : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<bool>(
            "IsManagement",
            "SupplyReturn",
            "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<long>(
            "SupplyReturnItemId",
            "ReSaleAvailability",
            "bigint",
            nullable: true);

        migrationBuilder.CreateIndex(
            "IX_ReSaleAvailability_SupplyReturnItemId",
            "ReSaleAvailability",
            "SupplyReturnItemId");

        migrationBuilder.AddForeignKey(
            "FK_ReSaleAvailability_SupplyReturnItem_SupplyReturnItemId",
            "ReSaleAvailability",
            "SupplyReturnItemId",
            "SupplyReturnItem",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_ReSaleAvailability_SupplyReturnItem_SupplyReturnItemId",
            "ReSaleAvailability");

        migrationBuilder.DropIndex(
            "IX_ReSaleAvailability_SupplyReturnItemId",
            "ReSaleAvailability");

        migrationBuilder.DropColumn(
            "IsManagement",
            "SupplyReturn");

        migrationBuilder.DropColumn(
            "SupplyReturnItemId",
            "ReSaleAvailability");
    }
}