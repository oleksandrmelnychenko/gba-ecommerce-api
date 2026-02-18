using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class ProductPlacementHistory : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.CreateTable(
            "ProductPlacementHistory",
            table => new {
                ID = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Placement = table.Column<string>("nvarchar(500)", maxLength: 500, nullable: true),
                ProductId = table.Column<long>("bigint", nullable: false),
                StorageId = table.Column<long>("bigint", nullable: false),
                Qty = table.Column<double>("float", nullable: false),
                StorageLocationType = table.Column<int>("int", nullable: false),
                AdditionType = table.Column<int>("int", nullable: false),
                UserId = table.Column<long>("bigint", nullable: false),
                NetUID = table.Column<Guid>("uniqueidentifier", nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>("datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>("datetime2", nullable: false),
                Deleted = table.Column<bool>("bit", nullable: false, defaultValueSql: "0")
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductPlacementHistory", x => x.ID);
                table.ForeignKey(
                    "FK_ProductPlacementHistory_Product_ProductId",
                    x => x.ProductId,
                    "Product",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_ProductPlacementHistory_Storage_StorageId",
                    x => x.StorageId,
                    "Storage",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    "FK_ProductPlacementHistory_User_UserId",
                    x => x.UserId,
                    "User",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            "IX_ProductPlacementHistory_ProductId",
            "ProductPlacementHistory",
            "ProductId");

        migrationBuilder.CreateIndex(
            "IX_ProductPlacementHistory_StorageId",
            "ProductPlacementHistory",
            "StorageId");

        migrationBuilder.CreateIndex(
            "IX_ProductPlacementHistory_UserId",
            "ProductPlacementHistory",
            "UserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropTable(
            "ProductPlacementHistory");
    }
}