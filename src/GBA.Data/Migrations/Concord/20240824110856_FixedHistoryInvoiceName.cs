using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class FixedHistoryInvoiceName : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_HistoryinvoiceEdit_Sale_SaleID",
            "HistoryinvoiceEdit");

        migrationBuilder.DropForeignKey(
            "FK_OrderItemBaseShiftStatus_HistoryinvoiceEdit_HistoryinvoiceEditId",
            "OrderItemBaseShiftStatus");

        migrationBuilder.DropPrimaryKey(
            "PK_HistoryinvoiceEdit",
            "HistoryinvoiceEdit");

        migrationBuilder.RenameTable(
            "HistoryinvoiceEdit",
            newName: "HistoryInvoiceEdit");

        migrationBuilder.RenameColumn(
            "HistoryinvoiceEditId",
            "OrderItemBaseShiftStatus",
            "HistoryInvoiceEditID");

        migrationBuilder.RenameIndex(
            "IX_OrderItemBaseShiftStatus_HistoryinvoiceEditId",
            table: "OrderItemBaseShiftStatus",
            newName: "IX_OrderItemBaseShiftStatus_HistoryInvoiceEditID");

        migrationBuilder.RenameIndex(
            "IX_HistoryinvoiceEdit_SaleID",
            table: "HistoryInvoiceEdit",
            newName: "IX_HistoryInvoiceEdit_SaleID");

        migrationBuilder.AddPrimaryKey(
            "PK_HistoryInvoiceEdit",
            "HistoryInvoiceEdit",
            "ID");

        migrationBuilder.AddForeignKey(
            "FK_HistoryInvoiceEdit_Sale_SaleID",
            "HistoryInvoiceEdit",
            "SaleID",
            "Sale",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_OrderItemBaseShiftStatus_HistoryInvoiceEdit_HistoryInvoiceEditID",
            "OrderItemBaseShiftStatus",
            "HistoryInvoiceEditID",
            "HistoryInvoiceEdit",
            principalColumn: "ID");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_HistoryInvoiceEdit_Sale_SaleID",
            "HistoryInvoiceEdit");

        migrationBuilder.DropForeignKey(
            "FK_OrderItemBaseShiftStatus_HistoryInvoiceEdit_HistoryInvoiceEditID",
            "OrderItemBaseShiftStatus");

        migrationBuilder.DropPrimaryKey(
            "PK_HistoryInvoiceEdit",
            "HistoryInvoiceEdit");

        migrationBuilder.RenameTable(
            "HistoryInvoiceEdit",
            newName: "HistoryinvoiceEdit");

        migrationBuilder.RenameColumn(
            "HistoryInvoiceEditID",
            "OrderItemBaseShiftStatus",
            "HistoryinvoiceEditId");

        migrationBuilder.RenameIndex(
            "IX_OrderItemBaseShiftStatus_HistoryInvoiceEditID",
            table: "OrderItemBaseShiftStatus",
            newName: "IX_OrderItemBaseShiftStatus_HistoryinvoiceEditId");

        migrationBuilder.RenameIndex(
            "IX_HistoryInvoiceEdit_SaleID",
            table: "HistoryinvoiceEdit",
            newName: "IX_HistoryinvoiceEdit_SaleID");

        migrationBuilder.AddPrimaryKey(
            "PK_HistoryinvoiceEdit",
            "HistoryinvoiceEdit",
            "ID");

        migrationBuilder.AddForeignKey(
            "FK_HistoryinvoiceEdit_Sale_SaleID",
            "HistoryinvoiceEdit",
            "SaleID",
            "Sale",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_OrderItemBaseShiftStatus_HistoryinvoiceEdit_HistoryinvoiceEditId",
            "OrderItemBaseShiftStatus",
            "HistoryinvoiceEditId",
            "HistoryinvoiceEdit",
            principalColumn: "ID");
    }
}