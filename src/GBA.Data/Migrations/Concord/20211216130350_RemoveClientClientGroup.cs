using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class RemoveClientClientGroup : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropTable(
            "ClientClientGroup");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.CreateTable(
            "ClientClientGroup",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                ClientGroupID = table.Column<long>(nullable: false),
                ClientID = table.Column<long>(nullable: false),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Updated = table.Column<DateTime>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ClientClientGroup", x => x.ID);
                table.ForeignKey(
                    "FK_ClientClientGroup_ClientGroup_ClientGroupID",
                    x => x.ClientGroupID,
                    "ClientGroup",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ClientClientGroup_Client_ClientID",
                    x => x.ClientID,
                    "Client",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            "IX_ClientClientGroup_ClientGroupID",
            "ClientClientGroup",
            "ClientGroupID");

        migrationBuilder.CreateIndex(
            "IX_ClientClientGroup_ClientID",
            "ClientClientGroup",
            "ClientID");
    }
}