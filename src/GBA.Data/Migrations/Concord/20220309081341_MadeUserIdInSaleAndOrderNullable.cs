using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class MadeUserIdInSaleAndOrderNullable : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AlterColumn<long>(
            "UserID",
            "Sale",
            nullable: true,
            oldClrType: typeof(long));

        migrationBuilder.AlterColumn<long>(
            "UserID",
            "Order",
            nullable: true,
            oldClrType: typeof(long));
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.AlterColumn<long>(
            "UserID",
            "Sale",
            nullable: false,
            oldClrType: typeof(long),
            oldNullable: true);

        migrationBuilder.AlterColumn<long>(
            "UserID",
            "Order",
            nullable: false,
            oldClrType: typeof(long),
            oldNullable: true);
    }
}