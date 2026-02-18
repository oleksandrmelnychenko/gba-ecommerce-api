using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class ConsignmentNoteSettingCarAndTrailerInfo : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<decimal>(
            "CarGrossWeight",
            "ConsignmentNoteSetting",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<int>(
            "CarHeight",
            "ConsignmentNoteSetting",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            "CarLabel",
            "ConsignmentNoteSetting",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            "CarLength",
            "ConsignmentNoteSetting",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<decimal>(
            "CarNetWeight",
            "ConsignmentNoteSetting",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<int>(
            "CarWeight",
            "ConsignmentNoteSetting",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<decimal>(
            "TrailerGrossWeight",
            "ConsignmentNoteSetting",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<int>(
            "TrailerHeight",
            "ConsignmentNoteSetting",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            "TrailerLabel",
            "ConsignmentNoteSetting",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            "TrailerLength",
            "ConsignmentNoteSetting",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<decimal>(
            "TrailerNetWeight",
            "ConsignmentNoteSetting",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<int>(
            "TrailerWeight",
            "ConsignmentNoteSetting",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "CarGrossWeight",
            "ConsignmentNoteSetting");

        migrationBuilder.DropColumn(
            "CarHeight",
            "ConsignmentNoteSetting");

        migrationBuilder.DropColumn(
            "CarLabel",
            "ConsignmentNoteSetting");

        migrationBuilder.DropColumn(
            "CarLength",
            "ConsignmentNoteSetting");

        migrationBuilder.DropColumn(
            "CarNetWeight",
            "ConsignmentNoteSetting");

        migrationBuilder.DropColumn(
            "CarWeight",
            "ConsignmentNoteSetting");

        migrationBuilder.DropColumn(
            "TrailerGrossWeight",
            "ConsignmentNoteSetting");

        migrationBuilder.DropColumn(
            "TrailerHeight",
            "ConsignmentNoteSetting");

        migrationBuilder.DropColumn(
            "TrailerLabel",
            "ConsignmentNoteSetting");

        migrationBuilder.DropColumn(
            "TrailerLength",
            "ConsignmentNoteSetting");

        migrationBuilder.DropColumn(
            "TrailerNetWeight",
            "ConsignmentNoteSetting");

        migrationBuilder.DropColumn(
            "TrailerWeight",
            "ConsignmentNoteSetting");
    }
}