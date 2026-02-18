using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class ProductPlacementStorage : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.CreateTable(
            "ProductPlacementStorage",
            table => new {
                ID = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Qty = table.Column<double>("float", nullable: false),
                Placement = table.Column<string>("nvarchar(500)", maxLength: 500, nullable: true),
                VendorCode = table.Column<string>("nvarchar(max)", nullable: true),
                ProductPlacementId = table.Column<long>("bigint", nullable: false),
                ProductId = table.Column<long>("bigint", nullable: false),
                StorageId = table.Column<long>("bigint", nullable: false),
                NetUID = table.Column<Guid>("uniqueidentifier", nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>("datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>("datetime2", nullable: false),
                Deleted = table.Column<bool>("bit", nullable: false, defaultValueSql: "0")
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductPlacementStorage", x => x.ID);
                table.ForeignKey(
                    "FK_ProductPlacementStorage_Product_ProductId",
                    x => x.ProductId,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_ProductPlacementStorage_ProductPlacement_ProductPlacementId",
                    x => x.ProductPlacementId,
                    "ProductPlacement",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_ProductPlacementStorage_Storage_StorageId",
                    x => x.StorageId,
                    "Storage",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            "IX_ProductPlacementStorage_ProductId",
            "ProductPlacementStorage",
            "ProductId");

        migrationBuilder.CreateIndex(
            "IX_ProductPlacementStorage_ProductPlacementId",
            "ProductPlacementStorage",
            "ProductPlacementId");

        migrationBuilder.CreateIndex(
            "IX_ProductPlacementStorage_StorageId",
            "ProductPlacementStorage",
            "StorageId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropTable(
            "ProductPlacementStorage");
    }
}