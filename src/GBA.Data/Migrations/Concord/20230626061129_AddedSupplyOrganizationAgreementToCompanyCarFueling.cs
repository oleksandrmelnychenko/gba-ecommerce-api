using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddedSupplyOrganizationAgreementToCompanyCarFueling : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<long>(
            "SupplyOrganizationAgreementID",
            "CompanyCarFueling",
            "bigint",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.CreateIndex(
            "IX_CompanyCarFueling_SupplyOrganizationAgreementID",
            "CompanyCarFueling",
            "SupplyOrganizationAgreementID");

        migrationBuilder.AddForeignKey(
            "FK_CompanyCarFueling_SupplyOrganizationAgreement_SupplyOrganizationAgreementID",
            "CompanyCarFueling",
            "SupplyOrganizationAgreementID",
            "SupplyOrganizationAgreement",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_CompanyCarFueling_SupplyOrganizationAgreement_SupplyOrganizationAgreementID",
            "CompanyCarFueling");

        migrationBuilder.DropIndex(
            "IX_CompanyCarFueling_SupplyOrganizationAgreementID",
            "CompanyCarFueling");

        migrationBuilder.DropColumn(
            "SupplyOrganizationAgreementID",
            "CompanyCarFueling");
    }
}