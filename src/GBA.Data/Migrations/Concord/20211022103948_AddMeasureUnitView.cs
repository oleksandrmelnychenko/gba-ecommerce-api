using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddMeasureUnitView : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql(@"
                IF NOT EXISTS(select * FROM sys.views where name = 'MeasureUnitView')
                BEGIN
                    EXEC('
                        CREATE VIEW views.MeasureUnitView
                            WITH SCHEMABINDING
                        AS
                        SELECT [MeasureUnit].[ID]
                             , [MeasureUnit].[Created]
                             , [MeasureUnit].[Deleted]
                             , [MeasureUnit].[NetUID]
                             , [MeasureUnit].[Updated]
                             , [MeasureUnit].[CodeOneC]
                             , [MeasureUnit].[Name] [OriginalName]
                             , [MeasureUnit].[Description] [OriginalDescription]
                             , [MUT].[Name]
                             , [MUT].[Description]
                             , [MUT].[CultureCode]
                        FROM [dbo].[MeasureUnit] [MeasureUnit]
                            LEFT JOIN [dbo].[MeasureUnitTranslation] [MUT]
                                ON MeasureUnit.ID = [MUT].MeasureUnitID
                                AND [MUT].Deleted = 0
                    ')
                END
            ");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql("DROP VIEW views.MeasureUnitView");
    }
}