using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class RenameRetailPaymentStatus : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_RetailClientPaymentImage_RetailPaymentStatus_PaymentImageStatusId",
            "RetailClientPaymentImage");

        migrationBuilder.RenameColumn(
            "PaymentImageStatusId",
            "RetailClientPaymentImage",
            "RetailPaymentStatusId");

        migrationBuilder.RenameIndex(
            "IX_RetailClientPaymentImage_PaymentImageStatusId",
            table: "RetailClientPaymentImage",
            newName: "IX_RetailClientPaymentImage_RetailPaymentStatusId");

        migrationBuilder.AddForeignKey(
            "FK_RetailClientPaymentImage_RetailPaymentStatus_RetailPaymentStatusId",
            "RetailClientPaymentImage",
            "RetailPaymentStatusId",
            "RetailPaymentStatus",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_RetailClientPaymentImage_RetailPaymentStatus_RetailPaymentStatusId",
            "RetailClientPaymentImage");

        migrationBuilder.RenameColumn(
            "RetailPaymentStatusId",
            "RetailClientPaymentImage",
            "PaymentImageStatusId");

        migrationBuilder.RenameIndex(
            "IX_RetailClientPaymentImage_RetailPaymentStatusId",
            table: "RetailClientPaymentImage",
            newName: "IX_RetailClientPaymentImage_PaymentImageStatusId");

        migrationBuilder.AddForeignKey(
            "FK_RetailClientPaymentImage_RetailPaymentStatus_PaymentImageStatusId",
            "RetailClientPaymentImage",
            "PaymentImageStatusId",
            "RetailPaymentStatus",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);
    }
}