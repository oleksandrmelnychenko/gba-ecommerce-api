using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddOrganizationView : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql(@"
                IF NOT EXISTS(select * FROM sys.views where name = 'OrganizationView')
                BEGIN
                    EXEC('
                        CREATE VIEW views.OrganizationView
                            WITH SCHEMABINDING
                        AS
                        SELECT [Organization].[ID]
		                    ,[Organization].[Created]
		                    ,[Organization].[Deleted]
		                    ,[Organization].[Name] AS [OriginalName]
		                    ,[Organization].[NetUID]
		                    ,[Organization].[Updated]
		                    ,[Organization].[Code]
		                    ,[Organization].[Culture]
		                    ,[Organization].[Address]
		                    ,[Organization].[FullName]
		                    ,[Organization].[IsIndividual]
		                    ,[Organization].[PFURegistrationDate]
		                    ,[Organization].[PFURegistrationNumber]
		                    ,[Organization].[PhoneNumber]
		                    ,[Organization].[RegistrationDate]
		                    ,[Organization].[RegistrationNumber]
		                    ,[Organization].[SROI]
		                    ,[Organization].[TIN]
		                    ,[Organization].[USREOU]
		                    ,[Organization].[CurrencyID]
		                    ,[Organization].[StorageID]
		                    ,[Organization].[TaxInspectionID]
		                    ,[Organization].[Manager]
		                    ,[Organization].[TypeTaxation]
		                    ,[Organization].[VatRateID]
		                    ,[Organization].[IsVatAgreements]
		                    ,[Translation].[Name]
		                    ,[Translation].[CultureCode]
	                    FROM [dbo].[Organization] AS [Organization]
		                    INNER JOIN [dbo].[OrganizationTranslation] AS [Translation]
			                    ON [Translation].OrganizationID = [Organization].ID
			                    AND [Translation].Deleted = 0
                    ')
                END
            ");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql("DROP VIEW views.OrganizationView");
    }
}