using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class UpdateUserIdSupplyPaymentTaskMap : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AlterColumn<long>(
            "UserID",
            "SupplyPaymentTask",
            "bigint",
            nullable: true,
            oldClrType: typeof(long),
            oldType: "bigint");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.AlterColumn<long>(
            "UserID",
            "SupplyPaymentTask",
            "bigint",
            nullable: false,
            defaultValue: 0L,
            oldClrType: typeof(long),
            oldType: "bigint",
            oldNullable: true);
    }
}