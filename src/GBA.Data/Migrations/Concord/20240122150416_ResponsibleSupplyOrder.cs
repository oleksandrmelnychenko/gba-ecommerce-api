using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class ResponsibleSupplyOrder : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<decimal>(
            "TotalAccountingDeliveryExpenseAmount",
            "SupplyOrderUkraine",
            "decimal(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            "TotalDeliveryExpenseAmount",
            "SupplyOrderUkraine",
            "decimal(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<long>(
            "ResponsibleId",
            "SupplyOrder",
            "bigint",
            nullable: true);

        migrationBuilder.CreateIndex(
            "IX_SupplyOrder_ResponsibleId",
            "SupplyOrder",
            "ResponsibleId");

        migrationBuilder.AddForeignKey(
            "FK_SupplyOrder_User_ResponsibleId",
            "SupplyOrder",
            "ResponsibleId",
            "User",
            principalColumn: "ID");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_SupplyOrder_User_ResponsibleId",
            "SupplyOrder");

        migrationBuilder.DropIndex(
            "IX_SupplyOrder_ResponsibleId",
            "SupplyOrder");

        migrationBuilder.DropColumn(
            "TotalAccountingDeliveryExpenseAmount",
            "SupplyOrderUkraine");

        migrationBuilder.DropColumn(
            "TotalDeliveryExpenseAmount",
            "SupplyOrderUkraine");

        migrationBuilder.DropColumn(
            "ResponsibleId",
            "SupplyOrder");
    }
}