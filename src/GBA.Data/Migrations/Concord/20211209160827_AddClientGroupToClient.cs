using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddClientGroupToClient : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<long>(
            "ClientGroupId",
            "ClientWorkplace",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            "ClientGroupId",
            "Client",
            nullable: true);

        migrationBuilder.CreateIndex(
            "IX_ClientWorkplace_ClientGroupId",
            "ClientWorkplace",
            "ClientGroupId");

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

        migrationBuilder.AddForeignKey(
            "FK_ClientWorkplace_ClientGroup_ClientGroupId",
            "ClientWorkplace",
            "ClientGroupId",
            "ClientGroup",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_Client_ClientGroup_ClientGroupId",
            "Client");

        migrationBuilder.DropForeignKey(
            "FK_ClientWorkplace_ClientGroup_ClientGroupId",
            "ClientWorkplace");

        migrationBuilder.DropIndex(
            "IX_ClientWorkplace_ClientGroupId",
            "ClientWorkplace");

        migrationBuilder.DropIndex(
            "IX_Client_ClientGroupId",
            "Client");

        migrationBuilder.DropColumn(
            "ClientGroupId",
            "ClientWorkplace");

        migrationBuilder.DropColumn(
            "ClientGroupId",
            "Client");
    }
}