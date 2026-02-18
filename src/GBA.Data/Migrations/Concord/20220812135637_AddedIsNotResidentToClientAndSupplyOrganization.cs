using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddedIsNotResidentToClientAndSupplyOrganization : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<bool>(
            "IsNotResident",
            "SupplyOrganization",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            "IsNotResident",
            "Client",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "IsNotResident",
            "SupplyOrganization");

        migrationBuilder.DropColumn(
            "IsNotResident",
            "Client");
    }
}