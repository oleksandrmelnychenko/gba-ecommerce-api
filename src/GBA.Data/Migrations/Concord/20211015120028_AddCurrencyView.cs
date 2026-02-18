using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddCurrencyView : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql(@"
                IF NOT EXISTS(select * FROM sys.views where name = 'CurrencyView')
                BEGIN
                    EXEC('
                        CREATE VIEW views.CurrencyView
                            WITH SCHEMABINDING
                        AS
                        SELECT [Currency].[ID]
		                    ,[Currency].[Code]
		                    ,[Currency].[Created]
		                    ,[Currency].[Deleted]
		                    ,[Currency].[Name] AS [OriginalName]
		                    ,[Currency].[NetUID]
		                    ,[Currency].[Updated]
		                    ,[Currency].[CodeOneC]
		                    ,[Translation].[Name]
		                    ,[Translation].[CultureCode]
	                    FROM [dbo].[Currency] AS [Currency]
		                    INNER JOIN [dbo].[CurrencyTranslation] AS [Translation]
			                    ON [Translation].CurrencyID = [Currency].ID
			                    AND [Translation].Deleted = 0
                    ')
                END
            ");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql("DROP VIEW views.CurrencyView");
    }
}