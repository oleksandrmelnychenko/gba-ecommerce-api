using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class IsHistorySet : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<bool>(
            "IsHistorySet",
            "ProductPlacement",
            "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<long>(
            "StorageId",
            "OrderItem",
            "bigint",
            nullable: true);

        migrationBuilder.CreateIndex(
            "IX_OrderItem_StorageId",
            "OrderItem",
            "StorageId");

        migrationBuilder.AddForeignKey(
            "FK_OrderItem_Storage_StorageId",
            "OrderItem",
            "StorageId",
            "Storage",
            principalColumn: "ID");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_OrderItem_Storage_StorageId",
            "OrderItem");

        migrationBuilder.DropIndex(
            "IX_OrderItem_StorageId",
            "OrderItem");

        migrationBuilder.DropColumn(
            "IsHistorySet",
            "ProductPlacement");

        migrationBuilder.DropColumn(
            "StorageId",
            "OrderItem");
    }
}