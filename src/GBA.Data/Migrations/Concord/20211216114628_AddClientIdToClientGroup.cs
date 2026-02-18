using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddClientIdToClientGroup : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<long>(
            "ClientID",
            "ClientGroup",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.CreateIndex(
            "IX_ClientGroup_ClientID",
            "ClientGroup",
            "ClientID");

        migrationBuilder.AddForeignKey(
            "FK_ClientGroup_Client_ClientID",
            "ClientGroup",
            "ClientID",
            "Client",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_ClientGroup_Client_ClientID",
            "ClientGroup");

        migrationBuilder.DropIndex(
            "IX_ClientGroup_ClientID",
            "ClientGroup");

        migrationBuilder.DropColumn(
            "ClientID",
            "ClientGroup");
    }
}