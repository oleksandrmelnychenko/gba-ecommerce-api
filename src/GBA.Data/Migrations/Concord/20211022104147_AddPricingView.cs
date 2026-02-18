using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddPricingView : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql(@"
                IF NOT EXISTS(select * FROM sys.views where name = 'PricingView')
                BEGIN
                    EXEC('
                        CREATE VIEW views.PricingView
                            WITH SCHEMABINDING
                        AS
                        SELECT [Pricing].[ID]
                             , [Pricing].[BasePricingID]
                             , [Pricing].[Comment]
                             , [Pricing].[Created]
                             , [Pricing].[CurrencyID]
                             , [Pricing].[Deleted]
                             , [Pricing].[ExtraCharge]
                             , [Pricing].[NetUID]
                             , [Pricing].[Updated]
                             , [Pricing].[PriceTypeID]
                             , [Pricing].[Culture]
                             , [Pricing].[CalculatedExtraCharge]
                             , [Pricing].[ForShares]
                             , [Pricing].[ForVat]
                             , [Pricing].[Name] [OriginalName]
                             , [PT].[Name]
                             , [PT].[CultureCode]
                        FROM [dbo].[Pricing] [Pricing]
                            LEFT JOIN [dbo].[PricingTranslation] [PT]
                                ON [PT].PricingID = [Pricing].ID
                                AND [PT].Deleted = 0
                    ')
                END
            ");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql("DROP VIEW views.PricingView");
    }
}