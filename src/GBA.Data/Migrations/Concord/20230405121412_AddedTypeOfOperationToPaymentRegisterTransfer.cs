using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddedTypeOfOperationToPaymentRegisterTransfer : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<int>(
            "TypeOfOperation",
            "PaymentRegisterTransfer",
            "int",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "TypeOfOperation",
            "PaymentRegisterTransfer");
    }
}