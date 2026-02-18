using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddedMainClientIdToClient : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<long>(
            "MainClientId",
            "Client",
            "bigint",
            nullable: true);

        migrationBuilder.CreateIndex(
            "IX_Client_MainClientId",
            "Client",
            "MainClientId");

        migrationBuilder.AddForeignKey(
            "FK_Client_Client_MainClientId",
            "Client",
            "MainClientId",
            "Client",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_Client_Client_MainClientId",
            "Client");

        migrationBuilder.DropIndex(
            "IX_Client_MainClientId",
            "Client");

        migrationBuilder.DropColumn(
            "MainClientId",
            "Client");
    }
}