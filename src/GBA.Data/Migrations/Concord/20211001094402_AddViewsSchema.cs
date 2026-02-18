using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddViewsSchema : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'views')
                BEGIN
                EXEC('CREATE SCHEMA views')
                END
            ");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql("DROP SCHEMA views");
    }
}