using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class ClientWorkplace : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.CreateTable(
            "ClientWorkplace",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                MainClientID = table.Column<long>(nullable: false),
                WorkplaceID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_ClientWorkplace", x => x.ID);
                table.ForeignKey(
                    "FK_ClientWorkplace_Client_MainClientID",
                    x => x.MainClientID,
                    "Client",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_ClientWorkplace_Client_WorkplaceID",
                    x => x.WorkplaceID,
                    "Client",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            "IX_ClientWorkplace_MainClientID",
            "ClientWorkplace",
            "MainClientID");

        migrationBuilder.CreateIndex(
            "IX_ClientWorkplace_WorkplaceID",
            "ClientWorkplace",
            "WorkplaceID");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropTable(
            "ClientWorkplace");
    }
}