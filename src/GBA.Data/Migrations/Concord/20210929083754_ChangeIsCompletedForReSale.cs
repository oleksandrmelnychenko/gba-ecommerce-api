using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class ChangeIsCompletedForReSale : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.RenameColumn(
            "IsConsignmentActive",
            "ReSale",
            "IsCompleted");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.RenameColumn(
            "IsCompleted",
            "ReSale",
            "IsConsignmentActive");
    }
}