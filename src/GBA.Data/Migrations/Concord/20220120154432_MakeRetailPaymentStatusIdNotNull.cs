using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class MakeRetailPaymentStatusIdNotNull : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_RetailClientPaymentImage_RetailPaymentStatus_RetailPaymentStatusId",
            "RetailClientPaymentImage");

        migrationBuilder.AlterColumn<long>(
            "RetailPaymentStatusId",
            "RetailClientPaymentImage",
            nullable: false,
            oldClrType: typeof(long),
            oldNullable: true);

        migrationBuilder.AddForeignKey(
            "FK_RetailClientPaymentImage_RetailPaymentStatus_RetailPaymentStatusId",
            "RetailClientPaymentImage",
            "RetailPaymentStatusId",
            "RetailPaymentStatus",
            principalColumn: "ID",
            onDelete: ReferentialAction.Cascade);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_RetailClientPaymentImage_RetailPaymentStatus_RetailPaymentStatusId",
            "RetailClientPaymentImage");

        migrationBuilder.AlterColumn<long>(
            "RetailPaymentStatusId",
            "RetailClientPaymentImage",
            nullable: true,
            oldClrType: typeof(long));

        migrationBuilder.AddForeignKey(
            "FK_RetailClientPaymentImage_RetailPaymentStatus_RetailPaymentStatusId",
            "RetailClientPaymentImage",
            "RetailPaymentStatusId",
            "RetailPaymentStatus",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);
    }
}