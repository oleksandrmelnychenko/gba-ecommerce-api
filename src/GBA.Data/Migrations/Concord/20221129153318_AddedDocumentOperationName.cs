using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddedDocumentOperationName : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.CreateTable(
            "AccountingOperationName",
            table => new {
                ID = table.Column<long>("bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                OperationType = table.Column<int>("int", nullable: false),
                NameUK = table.Column<string>("nvarchar(120)", maxLength: 120, nullable: true),
                NamePL = table.Column<string>("nvarchar(120)", maxLength: 120, nullable: true),
                NetUID = table.Column<Guid>("uniqueidentifier", nullable: false, defaultValueSql: "newid()"),
                Created = table.Column<DateTime>("datetime2", nullable: false, defaultValueSql: "getutcdate()"),
                Updated = table.Column<DateTime>("datetime2", nullable: false),
                Deleted = table.Column<bool>("bit", nullable: false, defaultValueSql: "0")
            },
            constraints: table => {
                table.PrimaryKey("PK_AccountingOperationName", x => x.ID);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropTable(
            "AccountingOperationName");
    }
}