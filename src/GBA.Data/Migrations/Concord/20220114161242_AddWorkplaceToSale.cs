using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddWorkplaceToSale : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<long>(
            "WorkplaceID",
            "Sale",
            nullable: true);

        migrationBuilder.CreateIndex(
            "IX_Sale_WorkplaceID",
            "Sale",
            "WorkplaceID");

        migrationBuilder.AddForeignKey(
            "FK_Sale_Workplace_WorkplaceID",
            "Sale",
            "WorkplaceID",
            "Workplace",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_Sale_Workplace_WorkplaceID",
            "Sale");

        migrationBuilder.DropIndex(
            "IX_Sale_WorkplaceID",
            "Sale");

        migrationBuilder.DropColumn(
            "WorkplaceID",
            "Sale");
    }
}