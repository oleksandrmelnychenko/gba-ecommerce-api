using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class ProductLocationHistory : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.CreateTable(
            "ProductLocationHistory",
            table => new {
                ID = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Qty = table.Column<double>("float", nullable: false),
                StorageID = table.Column<long>("bigint", nullable: false),
                ProductPlacementID = table.Column<long>("bigint", nullable: false),
                OrderItemID = table.Column<long>("bigint", nullable: true),
                DepreciatedOrderItemID = table.Column<long>("bigint", nullable: true),
                TypeOfMovement = table.Column<int>("int", nullable: false),
                NetUID = table.Column<Guid>("uniqueidentifier", nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>("datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>("datetime2", nullable: false),
                Deleted = table.Column<bool>("bit", nullable: false, defaultValueSql: "0")
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductLocationHistory", x => x.ID);
                table.ForeignKey(
                    "FK_ProductLocationHistory_DepreciatedOrderItem_DepreciatedOrderItemID",
                    x => x.DepreciatedOrderItemID,
                    "DepreciatedOrderItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductLocationHistory_OrderItem_OrderItemID",
                    x => x.OrderItemID,
                    "OrderItem",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductLocationHistory_ProductPlacement_ProductPlacementID",
                    x => x.ProductPlacementID,
                    "ProductPlacement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ProductLocationHistory_Storage_StorageID",
                    x => x.StorageID,
                    "Storage",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            "IX_ProductLocationHistory_DepreciatedOrderItemID",
            "ProductLocationHistory",
            "DepreciatedOrderItemID");

        migrationBuilder.CreateIndex(
            "IX_ProductLocationHistory_OrderItemID",
            "ProductLocationHistory",
            "OrderItemID");

        migrationBuilder.CreateIndex(
            "IX_ProductLocationHistory_ProductPlacementID",
            "ProductLocationHistory",
            "ProductPlacementID");

        migrationBuilder.CreateIndex(
            "IX_ProductLocationHistory_StorageID",
            "ProductLocationHistory",
            "StorageID");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropTable(
            "ProductLocationHistory");
    }
}