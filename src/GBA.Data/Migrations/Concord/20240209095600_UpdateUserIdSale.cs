using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class UpdateUserIdSale : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<long>(
            "UpdateUserID",
            "Sale",
            "bigint",
            nullable: true);

        migrationBuilder.CreateIndex(
            "IX_Sale_UpdateUserID",
            "Sale",
            "UpdateUserID");

        migrationBuilder.AddForeignKey(
            "FK_Sale_User_UpdateUserID",
            "Sale",
            "UpdateUserID",
            "User",
            principalColumn: "ID");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_Sale_User_UpdateUserID",
            "Sale");

        migrationBuilder.DropIndex(
            "IX_Sale_UpdateUserID",
            "Sale");

        migrationBuilder.DropColumn(
            "UpdateUserID",
            "Sale");
    }
}