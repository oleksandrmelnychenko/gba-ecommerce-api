using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddedSourceIdsToProductGroup : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.RenameColumn(
            "SourceID",
            "ProductGroup",
            "SourceFenixID");

        migrationBuilder.AddColumn<byte[]>(
            "SourceAmgID",
            "ProductGroup",
            maxLength: 16,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "SourceAmgID",
            "ProductGroup");

        migrationBuilder.RenameColumn(
            "SourceFenixID",
            "ProductGroup",
            "SourceID");
    }
}