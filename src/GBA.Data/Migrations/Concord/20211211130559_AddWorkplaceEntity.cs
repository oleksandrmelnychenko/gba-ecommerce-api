using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddWorkplaceEntity : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.CreateTable(
            "Workplace",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                FirstName = table.Column<string>(maxLength: 150, nullable: true),
                MiddleName = table.Column<string>(maxLength: 150, nullable: true),
                LastName = table.Column<string>(maxLength: 150, nullable: true),
                Email = table.Column<string>(maxLength: 150, nullable: true),
                PhoneNumber = table.Column<string>(maxLength: 16, nullable: true),
                IsActive = table.Column<bool>(nullable: false),
                MainClientID = table.Column<long>(nullable: false),
                ClientGroupID = table.Column<long>(nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_Workplace", x => x.ID);
                table.ForeignKey(
                    "FK_Workplace_ClientGroup_ClientGroupID",
                    x => x.ClientGroupID,
                    "ClientGroup",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_Workplace_Client_MainClientID",
                    x => x.MainClientID,
                    "Client",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            "IX_Workplace_ClientGroupID",
            "Workplace",
            "ClientGroupID");

        migrationBuilder.CreateIndex(
            "IX_Workplace_MainClientID",
            "Workplace",
            "MainClientID");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropTable(
            "Workplace");
    }
}