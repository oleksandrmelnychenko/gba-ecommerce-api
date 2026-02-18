using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddedSupplyOrganizationToIncomePaymentOrder : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<int>(
            "OperationType",
            "IncomePaymentOrder",
            "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<long>(
            "SupplyOrganizationAgreementID",
            "IncomePaymentOrder",
            "bigint",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            "SupplyOrganizationID",
            "IncomePaymentOrder",
            "bigint",
            nullable: true);

        migrationBuilder.CreateIndex(
            "IX_IncomePaymentOrder_SupplyOrganizationAgreementID",
            "IncomePaymentOrder",
            "SupplyOrganizationAgreementID");

        migrationBuilder.CreateIndex(
            "IX_IncomePaymentOrder_SupplyOrganizationID",
            "IncomePaymentOrder",
            "SupplyOrganizationID");

        migrationBuilder.AddForeignKey(
            "FK_IncomePaymentOrder_SupplyOrganization_SupplyOrganizationID",
            "IncomePaymentOrder",
            "SupplyOrganizationID",
            "SupplyOrganization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_IncomePaymentOrder_SupplyOrganizationAgreement_SupplyOrganizationAgreementID",
            "IncomePaymentOrder",
            "SupplyOrganizationAgreementID",
            "SupplyOrganizationAgreement",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_IncomePaymentOrder_SupplyOrganization_SupplyOrganizationID",
            "IncomePaymentOrder");

        migrationBuilder.DropForeignKey(
            "FK_IncomePaymentOrder_SupplyOrganizationAgreement_SupplyOrganizationAgreementID",
            "IncomePaymentOrder");

        migrationBuilder.DropIndex(
            "IX_IncomePaymentOrder_SupplyOrganizationAgreementID",
            "IncomePaymentOrder");

        migrationBuilder.DropIndex(
            "IX_IncomePaymentOrder_SupplyOrganizationID",
            "IncomePaymentOrder");

        migrationBuilder.DropColumn(
            "OperationType",
            "IncomePaymentOrder");

        migrationBuilder.DropColumn(
            "SupplyOrganizationAgreementID",
            "IncomePaymentOrder");

        migrationBuilder.DropColumn(
            "SupplyOrganizationID",
            "IncomePaymentOrder");
    }
}