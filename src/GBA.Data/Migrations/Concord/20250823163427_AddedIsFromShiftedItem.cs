using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddedIsFromShiftedItem : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<bool>(
            "IsFromShiftedItem",
            "OrderItem",
            "bit",
            nullable: false,
            defaultValueSql: "0");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "IsFromShiftedItem",
            "OrderItem");
    }
}