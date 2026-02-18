using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddFieldIsDevelopment : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<bool>(
            "IsDevelopment",
            "UpdateDataCarrier",
            "bit",
            nullable: false,
            defaultValueSql: "0");

        migrationBuilder.AddColumn<bool>(
            "IsDevelopment",
            "HistoryInvoiceEdit",
            "bit",
            nullable: false,
            defaultValueSql: "0");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "IsDevelopment",
            "UpdateDataCarrier");

        migrationBuilder.DropColumn(
            "IsDevelopment",
            "HistoryInvoiceEdit");
    }
}