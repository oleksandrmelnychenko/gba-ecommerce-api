using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddedClientToOutcomePaymentOrder : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<long>(
            "ClientID",
            "OutcomePaymentOrder",
            "bigint",
            nullable: true);

        migrationBuilder.CreateIndex(
            "IX_OutcomePaymentOrder_ClientID",
            "OutcomePaymentOrder",
            "ClientID");

        migrationBuilder.AddForeignKey(
            "FK_OutcomePaymentOrder_Client_ClientID",
            "OutcomePaymentOrder",
            "ClientID",
            "Client",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_OutcomePaymentOrder_Client_ClientID",
            "OutcomePaymentOrder");

        migrationBuilder.DropIndex(
            "IX_OutcomePaymentOrder_ClientID",
            "OutcomePaymentOrder");

        migrationBuilder.DropColumn(
            "ClientID",
            "OutcomePaymentOrder");
    }
}