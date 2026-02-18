using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddedWithAgreementLine : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<bool>(
            "WithAgreementLine",
            "Agreement",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "WithAgreementLine",
            "Agreement");
    }
}