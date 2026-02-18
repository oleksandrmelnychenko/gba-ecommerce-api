using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddedDocumentToSupplyOrganizationAgreement : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_SupplyOrganizationDocument_SupplyOrganization_SupplyOrganizationID",
            "SupplyOrganizationDocument");

        migrationBuilder.RenameColumn(
            "SupplyOrganizationID",
            "SupplyOrganizationDocument",
            "SupplyOrganizationAgreementID");

        migrationBuilder.RenameIndex(
            "IX_SupplyOrganizationDocument_SupplyOrganizationID",
            table: "SupplyOrganizationDocument",
            newName: "IX_SupplyOrganizationDocument_SupplyOrganizationAgreementID");

        migrationBuilder.AddForeignKey(
            "FK_SupplyOrganizationDocument_SupplyOrganizationAgreement_SupplyOrganizationAgreementID",
            "SupplyOrganizationDocument",
            "SupplyOrganizationAgreementID",
            "SupplyOrganizationAgreement",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_SupplyOrganizationDocument_SupplyOrganizationAgreement_SupplyOrganizationAgreementID",
            "SupplyOrganizationDocument");

        migrationBuilder.RenameColumn(
            "SupplyOrganizationAgreementID",
            "SupplyOrganizationDocument",
            "SupplyOrganizationID");

        migrationBuilder.RenameIndex(
            "IX_SupplyOrganizationDocument_SupplyOrganizationAgreementID",
            table: "SupplyOrganizationDocument",
            newName: "IX_SupplyOrganizationDocument_SupplyOrganizationID");

        migrationBuilder.AddForeignKey(
            "FK_SupplyOrganizationDocument_SupplyOrganization_SupplyOrganizationID",
            "SupplyOrganizationDocument",
            "SupplyOrganizationID",
            "SupplyOrganization",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);
    }
}