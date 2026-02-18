using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GBA.Data.Migrations.Concord
{
    public partial class UpdateSupplyInvoice : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SupplyOrganizationAgreementID",
                table: "SupplyInvoice",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SupplyOrganizationID",
                table: "SupplyInvoice",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplyInvoice_SupplyOrganizationAgreementID",
                table: "SupplyInvoice",
                column: "SupplyOrganizationAgreementID");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyInvoice_SupplyOrganizationID",
                table: "SupplyInvoice",
                column: "SupplyOrganizationID");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplyInvoice_SupplyOrganization_SupplyOrganizationID",
                table: "SupplyInvoice",
                column: "SupplyOrganizationID",
                principalTable: "SupplyOrganization",
                principalColumn: "ID");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplyInvoice_SupplyOrganizationAgreement_SupplyOrganizationAgreementID",
                table: "SupplyInvoice",
                column: "SupplyOrganizationAgreementID",
                principalTable: "SupplyOrganizationAgreement",
                principalColumn: "ID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplyInvoice_SupplyOrganization_SupplyOrganizationID",
                table: "SupplyInvoice");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplyInvoice_SupplyOrganizationAgreement_SupplyOrganizationAgreementID",
                table: "SupplyInvoice");

            migrationBuilder.DropIndex(
                name: "IX_SupplyInvoice_SupplyOrganizationAgreementID",
                table: "SupplyInvoice");

            migrationBuilder.DropIndex(
                name: "IX_SupplyInvoice_SupplyOrganizationID",
                table: "SupplyInvoice");

            migrationBuilder.DropColumn(
                name: "SupplyOrganizationAgreementID",
                table: "SupplyInvoice");

            migrationBuilder.DropColumn(
                name: "SupplyOrganizationID",
                table: "SupplyInvoice");
        }
    }
}
