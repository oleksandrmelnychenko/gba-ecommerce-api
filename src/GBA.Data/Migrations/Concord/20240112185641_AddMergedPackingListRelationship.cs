using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddMergedPackingListRelationship : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<long>(
            "RootPackingListID",
            "PackingList",
            "bigint",
            nullable: true);

        migrationBuilder.CreateIndex(
            "IX_PackingList_RootPackingListID",
            "PackingList",
            "RootPackingListID");

        migrationBuilder.AddForeignKey(
            "FK_PackingList_PackingList_RootPackingListID",
            "PackingList",
            "RootPackingListID",
            "PackingList",
            principalColumn: "ID",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropForeignKey(
            "FK_PackingList_PackingList_RootPackingListID",
            "PackingList");

        migrationBuilder.DropIndex(
            "IX_PackingList_RootPackingListID",
            "PackingList");

        migrationBuilder.DropColumn(
            "RootPackingListID",
            "PackingList");
    }
}