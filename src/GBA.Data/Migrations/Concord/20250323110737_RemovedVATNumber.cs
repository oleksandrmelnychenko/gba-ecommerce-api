using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class RemovedVATNumber : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "VATNumber",
            "Client");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<string>(
            "VATNumber",
            "Client",
            "nvarchar(50)",
            maxLength: 50,
            nullable: true);
    }
}