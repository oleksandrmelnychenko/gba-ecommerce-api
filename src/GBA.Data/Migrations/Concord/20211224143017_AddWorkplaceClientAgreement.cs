using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddWorkplaceClientAgreement : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.CreateTable(
            "WorkplaceClientAgreement",
            table => new {
                ID = table.Column<long>(nullable: false)
                    .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                NetUID = table.Column<Guid>(nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>(nullable: false),
                Deleted = table.Column<bool>(nullable: false, defaultValueSql: "0"),
                WorkplaceID = table.Column<long>(nullable: false),
                ClientAgreementID = table.Column<long>(nullable: false),
                IsSelected = table.Column<bool>(nullable: false)
            },
            constraints: table => {
                table.PrimaryKey("PK_WorkplaceClientAgreement", x => x.ID);
                table.ForeignKey(
                    "FK_WorkplaceClientAgreement_ClientAgreement_ClientAgreementID",
                    x => x.ClientAgreementID,
                    "ClientAgreement",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    "FK_WorkplaceClientAgreement_Workplace_WorkplaceID",
                    x => x.WorkplaceID,
                    "Workplace",
                    "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            "IX_WorkplaceClientAgreement_ClientAgreementID",
            "WorkplaceClientAgreement",
            "ClientAgreementID");

        migrationBuilder.CreateIndex(
            "IX_WorkplaceClientAgreement_WorkplaceID",
            "WorkplaceClientAgreement",
            "WorkplaceID");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropTable(
            "WorkplaceClientAgreement");
    }
}