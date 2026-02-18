using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class FixedSyncSourceForClientAndSupplier : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "OriginalClientCode",
            "ClientAgreement");

        migrationBuilder.DropColumn(
            "OldEcommerceID",
            "Client");

        migrationBuilder.DropColumn(
            "SourceCode",
            "Agreement");

        migrationBuilder.RenameColumn(
            "SourceID",
            "SupplyOrganizationAgreement",
            "SourceFenixID");

        migrationBuilder.RenameColumn(
            "SourceCode",
            "SupplyOrganizationAgreement",
            "SourceFenixCode");

        migrationBuilder.RenameColumn(
            "SourceID",
            "SupplyOrganization",
            "SourceFenixID");

        migrationBuilder.RenameColumn(
            "SourceCode",
            "SupplyOrganization",
            "SourceFenixCode");

        migrationBuilder.RenameColumn(
            "SourceID",
            "Client",
            "SourceFenixID");

        migrationBuilder.RenameColumn(
            "SourceID",
            "Agreement",
            "SourceFenixID");

        migrationBuilder.AddColumn<long>(
            "SourceAmgCode",
            "SupplyOrganizationAgreement",
            nullable: true);

        migrationBuilder.AddColumn<byte[]>(
            "SourceAmgID",
            "SupplyOrganizationAgreement",
            maxLength: 16,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            "OriginalRegionCode",
            "SupplyOrganization",
            maxLength: 10,
            nullable: true);

        migrationBuilder.AddColumn<long>(
            "SourceAmgCode",
            "SupplyOrganization",
            nullable: true);

        migrationBuilder.AddColumn<byte[]>(
            "SourceAmgID",
            "SupplyOrganization",
            maxLength: 16,
            nullable: true);

        migrationBuilder.AddColumn<long>(
            "OriginalClientAmgCode",
            "ClientAgreement",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            "OriginalClientFenixCode",
            "ClientAgreement",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            "OriginalRegionCode",
            "Client",
            maxLength: 10,
            nullable: true);

        migrationBuilder.AddColumn<long>(
            "SourceAmgCode",
            "Client",
            nullable: true);

        migrationBuilder.AddColumn<byte[]>(
            "SourceAmgID",
            "Client",
            maxLength: 16,
            nullable: true);

        migrationBuilder.AddColumn<long>(
            "SourceFenixCode",
            "Client",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            "SourceAmgCode",
            "Agreement",
            nullable: true);

        migrationBuilder.AddColumn<byte[]>(
            "SourceAmgID",
            "Agreement",
            maxLength: 16,
            nullable: true);

        migrationBuilder.AddColumn<long>(
            "SourceFenixCode",
            "Agreement",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "SourceAmgCode",
            "SupplyOrganizationAgreement");

        migrationBuilder.DropColumn(
            "SourceAmgID",
            "SupplyOrganizationAgreement");

        migrationBuilder.DropColumn(
            "OriginalRegionCode",
            "SupplyOrganization");

        migrationBuilder.DropColumn(
            "SourceAmgCode",
            "SupplyOrganization");

        migrationBuilder.DropColumn(
            "SourceAmgID",
            "SupplyOrganization");

        migrationBuilder.DropColumn(
            "OriginalClientAmgCode",
            "ClientAgreement");

        migrationBuilder.DropColumn(
            "OriginalClientFenixCode",
            "ClientAgreement");

        migrationBuilder.DropColumn(
            "OriginalRegionCode",
            "Client");

        migrationBuilder.DropColumn(
            "SourceAmgCode",
            "Client");

        migrationBuilder.DropColumn(
            "SourceAmgID",
            "Client");

        migrationBuilder.DropColumn(
            "SourceFenixCode",
            "Client");

        migrationBuilder.DropColumn(
            "SourceAmgCode",
            "Agreement");

        migrationBuilder.DropColumn(
            "SourceAmgID",
            "Agreement");

        migrationBuilder.DropColumn(
            "SourceFenixCode",
            "Agreement");

        migrationBuilder.RenameColumn(
            "SourceFenixID",
            "SupplyOrganizationAgreement",
            "SourceID");

        migrationBuilder.RenameColumn(
            "SourceFenixCode",
            "SupplyOrganizationAgreement",
            "SourceCode");

        migrationBuilder.RenameColumn(
            "SourceFenixID",
            "SupplyOrganization",
            "SourceID");

        migrationBuilder.RenameColumn(
            "SourceFenixCode",
            "SupplyOrganization",
            "SourceCode");

        migrationBuilder.RenameColumn(
            "SourceFenixID",
            "Client",
            "SourceID");

        migrationBuilder.RenameColumn(
            "SourceFenixID",
            "Agreement",
            "SourceID");

        migrationBuilder.AddColumn<long>(
            "OriginalClientCode",
            "ClientAgreement",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.AddColumn<long>(
            "OldEcommerceID",
            "Client",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.AddColumn<long>(
            "SourceCode",
            "Agreement",
            nullable: false,
            defaultValue: 0L);
    }
}