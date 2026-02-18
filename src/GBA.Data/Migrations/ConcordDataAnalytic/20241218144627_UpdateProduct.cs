using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.ConcordDataAnalytic;

public partial class UpdateProduct : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<string>(
            "MainOriginalNumber",
            "ProductPlacementDataHistory",
            "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            "NameUA",
            "ProductPlacementDataHistory",
            "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            "VendorCode",
            "ProductPlacementDataHistory",
            "nvarchar(max)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "MainOriginalNumber",
            "ProductPlacementDataHistory");

        migrationBuilder.DropColumn(
            "NameUA",
            "ProductPlacementDataHistory");

        migrationBuilder.DropColumn(
            "VendorCode",
            "ProductPlacementDataHistory");
    }
}