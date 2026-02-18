using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class UpdateDataCarrieField : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "IsPrintedPaymentInvoice",
            "UpdateDataCarrie");

        migrationBuilder.AddColumn<DateTime>(
            "ShipmentDate",
            "UpdateDataCarrie",
            "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            "TTN",
            "UpdateDataCarrie",
            "nvarchar(max)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "ShipmentDate",
            "UpdateDataCarrie");

        migrationBuilder.DropColumn(
            "TTN",
            "UpdateDataCarrie");

        migrationBuilder.AddColumn<bool>(
            "IsPrintedPaymentInvoice",
            "UpdateDataCarrie",
            "bit",
            nullable: false,
            defaultValue: false);
    }
}