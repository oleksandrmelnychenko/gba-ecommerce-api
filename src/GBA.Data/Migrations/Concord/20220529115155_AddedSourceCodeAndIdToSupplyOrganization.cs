using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddedSourceCodeAndIdToSupplyOrganization : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<long>(
            "SourceCode",
            "SupplyOrganizationAgreement",
            nullable: true);

        migrationBuilder.AddColumn<byte[]>(
            "SourceID",
            "SupplyOrganizationAgreement",
            maxLength: 16,
            nullable: true);

        migrationBuilder.AddColumn<long>(
            "SourceCode",
            "SupplyOrganization",
            nullable: true);

        migrationBuilder.AddColumn<byte[]>(
            "SourceID",
            "SupplyOrganization",
            maxLength: 16,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "SourceCode",
            "SupplyOrganizationAgreement");

        migrationBuilder.DropColumn(
            "SourceID",
            "SupplyOrganizationAgreement");

        migrationBuilder.DropColumn(
            "SourceCode",
            "SupplyOrganization");

        migrationBuilder.DropColumn(
            "SourceID",
            "SupplyOrganization");
    }
}