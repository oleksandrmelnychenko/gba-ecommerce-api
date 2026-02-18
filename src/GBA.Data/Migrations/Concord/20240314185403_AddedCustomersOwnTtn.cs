using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddedCustomersOwnTtn : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            "ActuallyTTN",
            "Sale");

        migrationBuilder.DropColumn(
            "IsActuallyTTN",
            "Sale");

        migrationBuilder.AddColumn<long>(
            "CustomersOwnTtnID",
            "Sale",
            "bigint",
            nullable: true);

        migrationBuilder.CreateTable(
            "CustomersOwnTtn",
            table => new {
                ID = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Number = table.Column<string>("nvarchar(150)", maxLength: 150, nullable: true),
                TtnPDFPath = table.Column<string>("nvarchar(max)", nullable: true),
                NetUID = table.Column<Guid>("uniqueidentifier", nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>("datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>("datetime2", nullable: false),
                Deleted = table.Column<bool>("bit", nullable: false, defaultValueSql: "0")
            },
            constraints: table => {
                table.PrimaryKey("PK_CustomersOwnTtn", x => x.ID);
            });

        migrationBuilder.CreateIndex(
            "IX_Sale_CustomersOwnTtnID",
            "Sale",
            "CustomersOwnTtnID");

        migrationBuilder.AddForeignKey(
            "FK_Sale_CustomersOwnTtn_CustomersOwnTtnID",
            "Sale",
            "CustomersOwnTtnID",
            "CustomersOwnTtn",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_Sale_CustomersOwnTtn_CustomersOwnTtnID",
            "Sale");

        migrationBuilder.DropTable(
            "CustomersOwnTtn");

        migrationBuilder.DropIndex(
            "IX_Sale_CustomersOwnTtnID",
            "Sale");

        migrationBuilder.DropColumn(
            "CustomersOwnTtnID",
            "Sale");

        migrationBuilder.AddColumn<string>(
            "ActuallyTTN",
            "Sale",
            "nvarchar(450)",
            maxLength: 450,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            "IsActuallyTTN",
            "Sale",
            "bit",
            nullable: false,
            defaultValue: false);
    }
}