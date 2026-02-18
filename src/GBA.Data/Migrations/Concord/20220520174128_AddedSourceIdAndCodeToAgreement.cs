using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddedSourceIdAndCodeToAgreement : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<long>(
            "SourceCode",
            "Agreement",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.AddColumn<byte[]>(
            "SourceID",
            "Agreement",
            maxLength: 16,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "SourceCode",
            "Agreement");

        migrationBuilder.DropColumn(
            "SourceID",
            "Agreement");
    }
}