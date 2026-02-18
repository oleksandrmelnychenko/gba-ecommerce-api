using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class SaleReturnItemProductPlacement : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_SaleReturnItem_ProductPlacement_ProductPlacementID",
            "SaleReturnItem");

        migrationBuilder.DropIndex(
            "IX_SaleReturnItem_ProductPlacementID",
            "SaleReturnItem");

        migrationBuilder.DropColumn(
            "ProductPlacementID",
            "SaleReturnItem");

        migrationBuilder.CreateTable(
            "SaleReturnItemProductPlacement",
            table => new {
                ID = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ProductPlacementID = table.Column<long>("bigint", nullable: true),
                SaleReturnItemId = table.Column<long>("bigint", nullable: false),
                NetUID = table.Column<Guid>("uniqueidentifier", nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>("datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>("datetime2", nullable: false),
                Deleted = table.Column<bool>("bit", nullable: false, defaultValueSql: "0")
            },
            constraints: table => {
                table.PrimaryKey("PK_SaleReturnItemProductPlacement", x => x.ID);
                table.ForeignKey(
                    "FK_SaleReturnItemProductPlacement_ProductPlacement_ProductPlacementID",
                    x => x.ProductPlacementID,
                    "ProductPlacement",
                    "ID");
                table.ForeignKey(
                    "FK_SaleReturnItemProductPlacement_SaleReturnItem_SaleReturnItemId",
                    x => x.SaleReturnItemId,
                    "SaleReturnItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            "IX_SaleReturnItemProductPlacement_ProductPlacementID",
            "SaleReturnItemProductPlacement",
            "ProductPlacementID");

        migrationBuilder.CreateIndex(
            "IX_SaleReturnItemProductPlacement_SaleReturnItemId",
            "SaleReturnItemProductPlacement",
            "SaleReturnItemId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropTable(
            "SaleReturnItemProductPlacement");

        migrationBuilder.AddColumn<long>(
            "ProductPlacementID",
            "SaleReturnItem",
            "bigint",
            nullable: true);

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
}