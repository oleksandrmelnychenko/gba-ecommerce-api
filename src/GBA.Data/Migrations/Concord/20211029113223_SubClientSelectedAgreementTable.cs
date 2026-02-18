using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class SubClientSelectedAgreementTable : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.CreateTable(
            "SubClientSelectedAgreement",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                ClientID = table.Column<long>(nullable: false),
                ClientAgreementID = table.Column<long>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_SubClientSelectedAgreement", x => x.ID);
                table.ForeignKey(
                    "FK_SubClientSelectedAgreement_ClientAgreement_ClientAgreementID",
                    x => x.ClientAgreementID,
                    "ClientAgreement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_SubClientSelectedAgreement_Client_ClientID",
                    x => x.ClientID,
                    "Client",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            "IX_SubClientSelectedAgreement_ClientAgreementID",
            "SubClientSelectedAgreement",
            "ClientAgreementID",
            unique: true);

        migrationBuilder.CreateIndex(
            "IX_SubClientSelectedAgreement_ClientID",
            "SubClientSelectedAgreement",
            "ClientID",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropTable(
            "SubClientSelectedAgreement");
    }
}