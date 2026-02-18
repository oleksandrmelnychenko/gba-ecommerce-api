using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddedSyncSourceIdsToProduct : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "OldEcommerceID",
            "Product");

        migrationBuilder.RenameColumn(
            "SourceID",
            "Product",
            "SourceFenixID");

        migrationBuilder.RenameColumn(
            "ParentID",
            "Product",
            "SourceAmgID");

        migrationBuilder.AddColumn<byte[]>(
            "ParentAmgID",
            "Product",
            maxLength: 16,
            nullable: true);

        migrationBuilder.AddColumn<byte[]>(
            "ParentFenixID",
            "Product",
            maxLength: 16,
            nullable: true);

        migrationBuilder.AddColumn<long>(
            "SourceAmgCode",
            "Product",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            "SourceFenixCode",
            "Product",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "ParentAmgID",
            "Product");

        migrationBuilder.DropColumn(
            "ParentFenixID",
            "Product");

        migrationBuilder.DropColumn(
            "SourceAmgCode",
            "Product");

        migrationBuilder.DropColumn(
            "SourceFenixCode",
            "Product");

        migrationBuilder.RenameColumn(
            "SourceFenixID",
            "Product",
            "SourceID");

        migrationBuilder.RenameColumn(
            "SourceAmgID",
            "Product",
            "ParentID");

        migrationBuilder.AddColumn<long>(
            "OldEcommerceID",
            "Product",
            nullable: false,
            defaultValue: 0L);
    }
}