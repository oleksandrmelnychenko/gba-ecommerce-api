using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddWorkplaceIdToShoppingCart : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<long>(
            "WorkplaceID",
            "ClientShoppingCart",
            nullable: true);

        migrationBuilder.CreateIndex(
            "IX_ClientShoppingCart_WorkplaceID",
            "ClientShoppingCart",
            "WorkplaceID");

        migrationBuilder.AddForeignKey(
            "FK_ClientShoppingCart_Workplace_WorkplaceID",
            "ClientShoppingCart",
            "WorkplaceID",
            "Workplace",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_ClientShoppingCart_Workplace_WorkplaceID",
            "ClientShoppingCart");

        migrationBuilder.DropIndex(
            "IX_ClientShoppingCart_WorkplaceID",
            "ClientShoppingCart");

        migrationBuilder.DropColumn(
            "WorkplaceID",
            "ClientShoppingCart");
    }
}