using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class RemovedTypeOfOperationFromIncomePaymentOrder : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_IncomePaymentOrder_TypeOfOperation_TypeOfOperationID",
            "IncomePaymentOrder");

        migrationBuilder.DropForeignKey(
            "FK_OutcomePaymentOrder_TypeOfOperation_TypeOfOperationID",
            "OutcomePaymentOrder");

        migrationBuilder.DropTable(
            "TypeOfOperation");

        migrationBuilder.DropIndex(
            "IX_OutcomePaymentOrder_TypeOfOperationID",
            "OutcomePaymentOrder");

        migrationBuilder.DropIndex(
            "IX_IncomePaymentOrder_TypeOfOperationID",
            "IncomePaymentOrder");

        migrationBuilder.DropColumn(
            "TypeOfOperationID",
            "OutcomePaymentOrder");

        migrationBuilder.DropColumn(
            "TypeOfOperationID",
            "IncomePaymentOrder");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<long>(
            "TypeOfOperationID",
            "OutcomePaymentOrder",
            "bigint",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            "TypeOfOperationID",
            "IncomePaymentOrder",
            "bigint",
            nullable: true);

        migrationBuilder.CreateTable(
            "TypeOfOperation",
            table => new {
                ID = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Created = table.Column<DateTime>("datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                Deleted = table.Column<bool>("bit", nullable: false, defaultValueSql: "0"),
                NetUID = table.Column<Guid>("uniqueidentifier", nullable: false, defaultValueSql: "newid()"),
                OperationName = table.Column<string>("nvarchar(max)", nullable: true),
                Updated = table.Column<DateTime>("datetime2", nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_TypeOfOperation", x => x.ID);
            });

        migrationBuilder.CreateIndex(
            "IX_OutcomePaymentOrder_TypeOfOperationID",
            "OutcomePaymentOrder",
            "TypeOfOperationID",
            unique: true,
            filter: "[TypeOfOperationID] IS NOT NULL");

        migrationBuilder.CreateIndex(
            "IX_IncomePaymentOrder_TypeOfOperationID",
            "IncomePaymentOrder",
            "TypeOfOperationID",
            unique: true,
            filter: "[TypeOfOperationID] IS NOT NULL");

        migrationBuilder.AddForeignKey(
            "FK_IncomePaymentOrder_TypeOfOperation_TypeOfOperationID",
            "IncomePaymentOrder",
            "TypeOfOperationID",
            "TypeOfOperation",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            "FK_OutcomePaymentOrder_TypeOfOperation_TypeOfOperationID",
            "OutcomePaymentOrder",
            "TypeOfOperationID",
            "TypeOfOperation",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);
    }
}