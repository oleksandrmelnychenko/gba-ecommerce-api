using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class DefaultvalueAmount : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AlterColumn<decimal>(
            "Amount",
            "SaleReturnItem",
            "decimal(30,14)",
            nullable: false,
            defaultValueSql: "0",
            oldClrType: typeof(decimal),
            oldType: "decimal(30,14)");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.AlterColumn<decimal>(
            "Amount",
            "SaleReturnItem",
            "decimal(30,14)",
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "decimal(30,14)",
            oldDefaultValueSql: "0");
    }
}