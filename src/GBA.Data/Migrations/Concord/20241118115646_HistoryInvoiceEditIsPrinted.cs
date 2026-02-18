using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class HistoryInvoiceEditIsPrinted : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<bool>(
            "IsPrinted",
            "HistoryInvoiceEdit",
            "bit",
            nullable: false,
            defaultValueSql: "0");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "IsPrinted",
            "HistoryInvoiceEdit");
    }
}