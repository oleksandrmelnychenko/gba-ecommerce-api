using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class ChangeNumberCustomDeclarationMaxLength : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AlterColumn<string>(
            "NumberCustomDeclaration",
            "SupplyInvoice",
            "nvarchar(100)",
            maxLength: 100,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(20)",
            oldMaxLength: 20,
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.AlterColumn<string>(
            "NumberCustomDeclaration",
            "SupplyInvoice",
            "nvarchar(20)",
            maxLength: 20,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(100)",
            oldMaxLength: 100,
            oldNullable: true);
    }
}