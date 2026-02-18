using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.ConcordDataAnalytic;

public partial class Create : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.CreateTable(
            "StockStateStorage",
            table => new {
                ID = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ChangeTypeOrderItem = table.Column<int>("int", nullable: false),
                TotalReservedUK = table.Column<double>("float", nullable: false),
                TotalCartReservedUK = table.Column<double>("float", nullable: false),
                ProductId = table.Column<long>("bigint", nullable: true),
                SaleId = table.Column<long>("bigint", nullable: true),
                UserID = table.Column<long>("bigint", nullable: true),
                SaleNumberId = table.Column<long>("bigint", nullable: true),
                NetUID = table.Column<Guid>("uniqueidentifier", nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>("datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>("datetime2", nullable: false),
                Deleted = table.Column<bool>("bit", nullable: false, defaultValueSql: "0")
            },
            constraints: table => {
                table.PrimaryKey("PK_StockStateStorage", x => x.ID);
            });

        migrationBuilder.CreateTable(
            "ProductAvailabilityDataHistory",
            table => new {
                ID = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Amount = table.Column<double>("float", nullable: false),
                StorageId = table.Column<long>("bigint", nullable: true),
                StockStateStorageID = table.Column<long>("bigint", nullable: false),
                NetUID = table.Column<Guid>("uniqueidentifier", nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>("datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>("datetime2", nullable: false),
                Deleted = table.Column<bool>("bit", nullable: false, defaultValueSql: "0")
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductAvailabilityDataHistory", x => x.ID);
                table.ForeignKey(
                    "FK_ProductAvailabilityDataHistory_StockStateStorage_StockStateStorageID",
                    x => x.StockStateStorageID,
                    "StockStateStorage",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "ProductPlacementDataHistory",
            table => new {
                ID = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Qty = table.Column<double>("float", nullable: false),
                StorageNumber = table.Column<string>("nvarchar(5)", maxLength: 5, nullable: true),
                RowNumber = table.Column<string>("nvarchar(5)", maxLength: 5, nullable: true),
                CellNumber = table.Column<string>("nvarchar(5)", maxLength: 5, nullable: true),
                ConsignmentNumber = table.Column<string>("nvarchar(max)", nullable: true),
                ProductAvailabilityDataHistoryID = table.Column<long>("bigint", nullable: true),
                ProductId = table.Column<long>("bigint", nullable: true),
                StorageId = table.Column<long>("bigint", nullable: true),
                ConsignmentItemId = table.Column<long>("bigint", nullable: true),
                NetUID = table.Column<Guid>("uniqueidentifier", nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>("datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>("datetime2", nullable: false),
                Deleted = table.Column<bool>("bit", nullable: false, defaultValueSql: "0")
            },
            constraints: table => {
                table.PrimaryKey("PK_ProductPlacementDataHistory", x => x.ID);
                table.ForeignKey(
                    "FK_ProductPlacementDataHistory_ProductAvailabilityDataHistory_ProductAvailabilityDataHistoryID",
                    x => x.ProductAvailabilityDataHistoryID,
                    "ProductAvailabilityDataHistory",
                    "ID",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            "IX_ProductAvailabilityDataHistory_StockStateStorageID",
            "ProductAvailabilityDataHistory",
            "StockStateStorageID");

        migrationBuilder.CreateIndex(
            "IX_ProductPlacementDataHistory_ProductAvailabilityDataHistoryID",
            "ProductPlacementDataHistory",
            "ProductAvailabilityDataHistoryID");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropTable(
            "ProductPlacementDataHistory");

        migrationBuilder.DropTable(
            "ProductAvailabilityDataHistory");

        migrationBuilder.DropTable(
            "StockStateStorage");
    }
}