using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class SupplyOrganizationTinSroiUsreou : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<string>(
            "SROI",
            "SupplyOrganization",
            "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            "TIN",
            "SupplyOrganization",
            "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            "USREOU",
            "SupplyOrganization",
            "nvarchar(max)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "SROI",
            "SupplyOrganization");

        migrationBuilder.DropColumn(
            "TIN",
            "SupplyOrganization");

        migrationBuilder.DropColumn(
            "USREOU",
            "SupplyOrganization");
    }
}