using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddMergedSupplyInvoiceRelationship : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<long>(
            "RootSupplyInvoiceID",
            "SupplyInvoice",
            "bigint",
            nullable: true);

        migrationBuilder.CreateIndex(
            "IX_SupplyInvoice_RootSupplyInvoiceID",
            "SupplyInvoice",
            "RootSupplyInvoiceID");

        migrationBuilder.AddForeignKey(
            "FK_SupplyInvoice_SupplyInvoice_RootSupplyInvoiceID",
            "SupplyInvoice",
            "RootSupplyInvoiceID",
            "SupplyInvoice",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_SupplyInvoice_SupplyInvoice_RootSupplyInvoiceID",
            "SupplyInvoice");

        migrationBuilder.DropIndex(
            "IX_SupplyInvoice_RootSupplyInvoiceID",
            "SupplyInvoice");

        migrationBuilder.DropColumn(
            "RootSupplyInvoiceID",
            "SupplyInvoice");
    }
}