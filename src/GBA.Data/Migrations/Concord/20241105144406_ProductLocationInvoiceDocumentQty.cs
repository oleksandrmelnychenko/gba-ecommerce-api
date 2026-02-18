using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class ProductLocationInvoiceDocumentQty : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<double>(
            "InvoiceDocumentQty",
            "ProductLocation",
            "float",
            nullable: false,
            defaultValue: 0.0);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "InvoiceDocumentQty",
            "ProductLocation");
    }
}