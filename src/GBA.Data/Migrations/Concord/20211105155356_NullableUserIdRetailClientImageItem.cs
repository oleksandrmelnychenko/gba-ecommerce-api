using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class NullableUserIdRetailClientImageItem : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "IsCorrectPayment",
            "RetailClientPaymentImageItem");

        migrationBuilder.AlterColumn<long>(
            "UserID",
            "RetailClientPaymentImageItem",
            nullable: true,
            oldClrType: typeof(long));
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.AlterColumn<long>(
            "UserID",
            "RetailClientPaymentImageItem",
            nullable: false,
            oldClrType: typeof(long),
            oldNullable: true);

        migrationBuilder.AddColumn<bool>(
            "IsCorrectPayment",
            "RetailClientPaymentImageItem",
            nullable: false,
            defaultValue: false);
    }
}