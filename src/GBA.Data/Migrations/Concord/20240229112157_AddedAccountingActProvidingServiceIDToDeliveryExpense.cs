using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddedAccountingActProvidingServiceIDToDeliveryExpense : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<long>(
            "AccountingActProvidingServiceID",
            "DeliveryExpense",
            "bigint",
            nullable: true);

        migrationBuilder.CreateIndex(
            "IX_DeliveryExpense_AccountingActProvidingServiceID",
            "DeliveryExpense",
            "AccountingActProvidingServiceID",
            unique: true,
            filter: "[AccountingActProvidingServiceID] IS NOT NULL");

        migrationBuilder.AddForeignKey(
            "FK_DeliveryExpense_ActProvidingService_AccountingActProvidingServiceID",
            "DeliveryExpense",
            "AccountingActProvidingServiceID",
            "ActProvidingService",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_DeliveryExpense_ActProvidingService_AccountingActProvidingServiceID",
            "DeliveryExpense");

        migrationBuilder.DropIndex(
            "IX_DeliveryExpense_AccountingActProvidingServiceID",
            "DeliveryExpense");

        migrationBuilder.DropColumn(
            "AccountingActProvidingServiceID",
            "DeliveryExpense");
    }
}