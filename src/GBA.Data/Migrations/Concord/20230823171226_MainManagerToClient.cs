using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class MainManagerToClient : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<long>(
            "MainManagerID",
            "Client",
            "bigint",
            nullable: true);

        migrationBuilder.CreateIndex(
            "IX_Client_MainManagerID",
            "Client",
            "MainManagerID");

        migrationBuilder.AddForeignKey(
            "FK_Client_User_MainManagerID",
            "Client",
            "MainManagerID",
            "User",
            principalColumn: "ID");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_Client_User_MainManagerID",
            "Client");

        migrationBuilder.DropIndex(
            "IX_Client_MainManagerID",
            "Client");

        migrationBuilder.DropColumn(
            "MainManagerID",
            "Client");
    }
}