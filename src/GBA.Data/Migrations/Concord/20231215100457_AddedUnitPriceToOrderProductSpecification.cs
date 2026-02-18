using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddedUnitPriceToOrderProductSpecification : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "Qty",
            "ProductSpecification");

        migrationBuilder.AddColumn<decimal>(
            "UnitPrice",
            "OrderProductSpecification",
            "decimal(18,2)",
            nullable: false,
            defaultValue: 0m);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "UnitPrice",
            "OrderProductSpecification");

        migrationBuilder.AddColumn<double>(
            "Qty",
            "ProductSpecification",
            "float",
            nullable: false,
            defaultValue: 0.0);
    }
}