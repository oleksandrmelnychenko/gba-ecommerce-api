using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class Historyinvoice : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<double>(
            "CurrentQty",
            "OrderItemBaseShiftStatus",
            "float",
            nullable: false,
            defaultValue: 0.0);

        migrationBuilder.AddColumn<long>(
            "HistoryinvoiceEditId",
            "OrderItemBaseShiftStatus",
            "bigint",
            nullable: true);

        migrationBuilder.CreateTable(
            "HistoryinvoiceEdit",
            table => new {
                ID = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                SaleID = table.Column<long>("bigint", nullable: false),
                NetUID = table.Column<Guid>("uniqueidentifier", nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>("datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>("datetime2", nullable: false),
                Deleted = table.Column<bool>("bit", nullable: false, defaultValueSql: "0")
            },
            constraints: table => {
                table.PrimaryKey("PK_HistoryinvoiceEdit", x => x.ID);
                table.ForeignKey(
                    "FK_HistoryinvoiceEdit_Sale_SaleID",
                    x => x.SaleID,
                    "Sale",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            "IX_OrderItemBaseShiftStatus_HistoryinvoiceEditId",
            "OrderItemBaseShiftStatus",
            "HistoryinvoiceEditId");

        migrationBuilder.CreateIndex(
            "IX_HistoryinvoiceEdit_SaleID",
            "HistoryinvoiceEdit",
            "SaleID");

        migrationBuilder.AddForeignKey(
            "FK_OrderItemBaseShiftStatus_HistoryinvoiceEdit_HistoryinvoiceEditId",
            "OrderItemBaseShiftStatus",
            "HistoryinvoiceEditId",
            "HistoryinvoiceEdit",
            principalColumn: "ID");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_OrderItemBaseShiftStatus_HistoryinvoiceEdit_HistoryinvoiceEditId",
            "OrderItemBaseShiftStatus");

        migrationBuilder.DropTable(
            "HistoryinvoiceEdit");

        migrationBuilder.DropIndex(
            "IX_OrderItemBaseShiftStatus_HistoryinvoiceEditId",
            "OrderItemBaseShiftStatus");

        migrationBuilder.DropColumn(
            "CurrentQty",
            "OrderItemBaseShiftStatus");

        migrationBuilder.DropColumn(
            "HistoryinvoiceEditId",
            "OrderItemBaseShiftStatus");
    }
}