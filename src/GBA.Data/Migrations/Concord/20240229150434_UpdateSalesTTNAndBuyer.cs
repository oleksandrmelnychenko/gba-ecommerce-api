using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class UpdateSalesTTNAndBuyer : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<string>(
            "ActuallyTTN",
            "Sale",
            "nvarchar(450)",
            maxLength: 450,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            "RecommendedBuyer",
            "Sale",
            "nvarchar(450)",
            maxLength: 450,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "ActuallyTTN",
            "Sale");

        migrationBuilder.DropColumn(
            "RecommendedBuyer",
            "Sale");
    }
}