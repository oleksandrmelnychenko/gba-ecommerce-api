using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class ChangedIsActiveToIsBlockedInWorkplace : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.RenameColumn(
            "IsActive",
            "Workplace",
            "IsBlocked");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.RenameColumn(
            "IsBlocked",
            "Workplace",
            "IsActive");
    }
}