using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class UpdatedRetailclientPaymentImage : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_RetailClientPaymentImage_RetailClient_RetailClientId",
            "RetailClientPaymentImage");

        migrationBuilder.AddColumn<long>(
            "SaleId",
            "RetailClientPaymentImage",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.CreateIndex(
            "IX_RetailClientPaymentImage_SaleId",
            "RetailClientPaymentImage",
            "SaleId");

        migrationBuilder.AddForeignKey(
            "FK_RetailClientPaymentImage_RetailClient_RetailClientId",
            "RetailClientPaymentImage",
            "RetailClientId",
            "RetailClient",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_RetailClientPaymentImage_Sale_SaleId",
            "RetailClientPaymentImage",
            "SaleId",
            "Sale",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_RetailClientPaymentImage_RetailClient_RetailClientId",
            "RetailClientPaymentImage");

        migrationBuilder.DropForeignKey(
            "FK_RetailClientPaymentImage_Sale_SaleId",
            "RetailClientPaymentImage");

        migrationBuilder.DropIndex(
            "IX_RetailClientPaymentImage_SaleId",
            "RetailClientPaymentImage");

        migrationBuilder.DropColumn(
            "SaleId",
            "RetailClientPaymentImage");

        migrationBuilder.AddForeignKey(
            "FK_RetailClientPaymentImage_RetailClient_RetailClientId",
            "RetailClientPaymentImage",
            "RetailClientId",
            "RetailClient",
            principalColumn: "ID",
            onDelete: ReferentialAction.Cascade);
    }
}