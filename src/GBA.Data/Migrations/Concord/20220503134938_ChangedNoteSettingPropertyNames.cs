using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class ChangedNoteSettingPropertyNames : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.RenameColumn(
            "TrailerWeight",
            "ConsignmentNoteSetting",
            "TrailerWidth");

        migrationBuilder.RenameColumn(
            "CarWeight",
            "ConsignmentNoteSetting",
            "CarWidth");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.RenameColumn(
            "TrailerWidth",
            "ConsignmentNoteSetting",
            "TrailerWeight");

        migrationBuilder.RenameColumn(
            "CarWidth",
            "ConsignmentNoteSetting",
            "CarWeight");
    }
}