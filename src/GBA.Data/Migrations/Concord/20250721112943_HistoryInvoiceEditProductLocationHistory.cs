using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class HistoryInvoiceEditProductLocationHistory : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<long>(
            "HistoryInvoiceEditID",
            "ProductLocationHistory",
            "bigint",
            nullable: true);

        migrationBuilder.CreateIndex(
            "IX_ProductLocationHistory_HistoryInvoiceEditID",
            "ProductLocationHistory",
            "HistoryInvoiceEditID");

        migrationBuilder.AddForeignKey(
            "FK_ProductLocationHistory_HistoryInvoiceEdit_HistoryInvoiceEditID",
            "ProductLocationHistory",
            "HistoryInvoiceEditID",
            "HistoryInvoiceEdit",
            principalColumn: "ID");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_ProductLocationHistory_HistoryInvoiceEdit_HistoryInvoiceEditID",
            "ProductLocationHistory");

        migrationBuilder.DropIndex(
            "IX_ProductLocationHistory_HistoryInvoiceEditID",
            "ProductLocationHistory");

        migrationBuilder.DropColumn(
            "HistoryInvoiceEditID",
            "ProductLocationHistory");
    }
}