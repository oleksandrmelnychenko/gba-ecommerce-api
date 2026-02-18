using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GBA.Data.Migrations.ConcordDataAnalytic
{
    public partial class ReportsSale : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportSale",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceType = table.Column<int>(type: "int", nullable: false),
                    ChangeTypeOrderItem = table.Column<int>(type: "int", nullable: false),
                    SaleId = table.Column<long>(type: "bigint", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    NetUID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "newid()"),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    Updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Deleted = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "0")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportSale", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "OrderHistory",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportSaleId = table.Column<long>(type: "bigint", nullable: true),
                    ClientAgreementId = table.Column<long>(type: "bigint", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmountLocal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalQty = table.Column<double>(type: "float", nullable: false),
                    NetUID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "newid()"),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    Updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Deleted = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "0")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderHistory", x => x.ID);
                    table.ForeignKey(
                        name: "FK_OrderHistory_ReportSale_ReportSaleId",
                        column: x => x.ReportSaleId,
                        principalTable: "ReportSale",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderItemHistory",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<long>(type: "bigint", nullable: true),
                    OrderHistoryId = table.Column<long>(type: "bigint", nullable: false),
                    Qty = table.Column<double>(type: "float", nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmountLocal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PricePerItem = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrentPriceEurTo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetUID = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "newid()"),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                    Updated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Deleted = table.Column<bool>(type: "bit", nullable: false, defaultValueSql: "0")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItemHistory", x => x.ID);
                    table.ForeignKey(
                        name: "FK_OrderItemHistory_OrderHistory_OrderHistoryId",
                        column: x => x.OrderHistoryId,
                        principalTable: "OrderHistory",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderHistory_ReportSaleId",
                table: "OrderHistory",
                column: "ReportSaleId",
                unique: true,
                filter: "[ReportSaleId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemHistory_OrderHistoryId",
                table: "OrderItemHistory",
                column: "OrderHistoryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItemHistory");

            migrationBuilder.DropTable(
                name: "OrderHistory");

            migrationBuilder.DropTable(
                name: "ReportSale");
        }
    }
}
