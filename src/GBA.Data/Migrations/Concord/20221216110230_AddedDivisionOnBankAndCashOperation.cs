using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddedDivisionOnBankAndCashOperation : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.RenameColumn(
            "NameUK",
            "AccountingOperationName",
            "CashNameUK");

        migrationBuilder.RenameColumn(
            "NamePL",
            "AccountingOperationName",
            "CashNamePL");

        migrationBuilder.AddColumn<string>(
            "BankNamePL",
            "AccountingOperationName",
            "nvarchar(120)",
            maxLength: 120,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            "BankNameUK",
            "AccountingOperationName",
            "nvarchar(120)",
            maxLength: 120,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "BankNamePL",
            "AccountingOperationName");

        migrationBuilder.DropColumn(
            "BankNameUK",
            "AccountingOperationName");

        migrationBuilder.RenameColumn(
            "CashNameUK",
            "AccountingOperationName",
            "NameUK");

        migrationBuilder.RenameColumn(
            "CashNamePL",
            "AccountingOperationName",
            "NamePL");
    }
}