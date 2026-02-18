using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddClientAgreementToSaleReturn : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<long>(
            "ClientAgreementID",
            "SaleReturn",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.CreateIndex(
            "IX_SaleReturn_ClientAgreementID",
            "SaleReturn",
            "ClientAgreementID");

        migrationBuilder.AddForeignKey(
            "FK_SaleReturn_ClientAgreement_ClientAgreementID",
            "SaleReturn",
            "ClientAgreementID",
            "ClientAgreement",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_SaleReturn_ClientAgreement_ClientAgreementID",
            "SaleReturn");

        migrationBuilder.DropIndex(
            "IX_SaleReturn_ClientAgreementID",
            "SaleReturn");

        migrationBuilder.DropColumn(
            "ClientAgreementID",
            "SaleReturn");
    }
}