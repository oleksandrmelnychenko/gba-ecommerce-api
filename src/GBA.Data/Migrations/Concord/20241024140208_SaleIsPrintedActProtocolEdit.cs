using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class SaleIsPrintedActProtocolEdit : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<bool>(
            "IsPrintedActProtocolEdit",
            "Sale",
            "bit",
            nullable: false,
            defaultValueSql: "0");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "IsPrintedActProtocolEdit",
            "Sale");
    }
}