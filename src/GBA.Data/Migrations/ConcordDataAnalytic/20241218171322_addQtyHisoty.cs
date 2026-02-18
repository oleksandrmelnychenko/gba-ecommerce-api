using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.ConcordDataAnalytic;

public partial class addQtyHisoty : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<double>(
            "QtyHistory",
            "StockStateStorage",
            "float",
            nullable: false,
            defaultValue: 0.0);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "QtyHistory",
            "StockStateStorage");
    }
}