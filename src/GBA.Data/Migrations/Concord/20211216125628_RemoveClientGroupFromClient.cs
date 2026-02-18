using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class RemoveClientGroupFromClient : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_Client_ClientGroup_ClientGroupId",
            "Client");

        migrationBuilder.DropIndex(
            "IX_Client_ClientGroupId",
            "Client");

        migrationBuilder.DropColumn(
            "ClientGroupId",
            "Client");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<long>(
            "ClientGroupId",
            "Client",
            nullable: true);

        migrationBuilder.CreateIndex(
            "IX_Client_ClientGroupId",
            "Client",
            "ClientGroupId");

        migrationBuilder.AddForeignKey(
            "FK_Client_ClientGroup_ClientGroupId",
            "Client",
            "ClientGroupId",
            "ClientGroup",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);
    }
}