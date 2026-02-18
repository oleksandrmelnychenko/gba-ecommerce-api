using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddDeliveryPerItemToPackingListPackageOrderItem : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<decimal>(
            "DeliveryPerItem",
            "PackingListPackageOrderItem",
            "decimal(30,14)",
            nullable: false,
            defaultValue: 0m);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "DeliveryPerItem",
            "PackingListPackageOrderItem");
    }
}