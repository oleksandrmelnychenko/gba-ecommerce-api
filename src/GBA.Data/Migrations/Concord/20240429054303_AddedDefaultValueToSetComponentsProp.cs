using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddedDefaultValueToSetComponentsProp : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AlterColumn<int>(
            "SetComponentsQty",
            "ProductSet",
            "int",
            nullable: false,
            defaultValue: 1,
            oldClrType: typeof(int),
            oldType: "int");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.AlterColumn<int>(
            "SetComponentsQty",
            "ProductSet",
            "int",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int",
            oldDefaultValue: 1);
    }
}