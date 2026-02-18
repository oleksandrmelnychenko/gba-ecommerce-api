using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class NullableSupplyOrganizationAgreement : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AlterColumn<long>(
            "SupplyOrganizationAgreementID",
            "CompanyCarFueling",
            "bigint",
            nullable: true,
            oldClrType: typeof(long),
            oldType: "bigint");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.AlterColumn<long>(
            "SupplyOrganizationAgreementID",
            "CompanyCarFueling",
            "bigint",
            nullable: false,
            defaultValue: 0L,
            oldClrType: typeof(long),
            oldType: "bigint",
            oldNullable: true);
    }
}