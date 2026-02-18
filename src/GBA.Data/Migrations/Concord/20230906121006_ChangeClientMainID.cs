using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class ChangeClientMainID : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_Client_Client_MainClientId",
            "Client");

        migrationBuilder.RenameColumn(
            "MainClientId",
            "Client",
            "MainClientID");

        migrationBuilder.RenameIndex(
            "IX_Client_MainClientId",
            table: "Client",
            newName: "IX_Client_MainClientID");

        migrationBuilder.AddForeignKey(
            "FK_Client_Client_MainClientID",
            "Client",
            "MainClientID",
            "Client",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_Client_Client_MainClientID",
            "Client");

        migrationBuilder.RenameColumn(
            "MainClientID",
            "Client",
            "MainClientId");

        migrationBuilder.RenameIndex(
            "IX_Client_MainClientID",
            table: "Client",
            newName: "IX_Client_MainClientId");

        migrationBuilder.AddForeignKey(
            "FK_Client_Client_MainClientId",
            "Client",
            "MainClientId",
            "Client",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);
    }
}