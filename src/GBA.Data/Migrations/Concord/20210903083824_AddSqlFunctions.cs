using Microsoft.EntityFrameworkCore.Migrations;

namespace GBA.Data.Migrations.Concord;

public partial class AddSqlFunctions : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql(@"
                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[GetTopRootConsignmentItemByConsignmentItemId]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[GetTopRootConsignmentItemByConsignmentItemId]

                GO 

                CREATE FUNCTION [dbo].[GetTopRootConsignmentItemByConsignmentItemId] 
                (	
	                -- Add the parameters for the function here
	                @ConsignmentItemId bigint
                )
                RETURNS TABLE 
                AS
                RETURN 
                (
	                -- Add the SELECT statement with parameter references here
	                WITH [SearchRootItem_CTE]
	                AS (
		                SELECT ID, RootConsignmentItemID
		                FROM [ConsignmentItem]
		                WHERE [ConsignmentItem].ID = @ConsignmentItemId
		                AND [ConsignmentItem].RootConsignmentItemID IS NOT NULL

		                UNION ALL 

		                SELECT [ConsignmentItem].ID, [ConsignmentItem].RootConsignmentItemID
		                FROM [ConsignmentItem]
			                INNER JOIN [SearchRootItem_CTE] AS [Child]
				                ON [Child].RootConsignmentItemID = [ConsignmentItem].ID
	                )

	                SELECT *
	                FROM [ConsignmentItem]
	                WHERE ID = (
		                SELECT TOP(1) [RootItem].ID 
		                FROM [SearchRootItem_CTE] AS [RootItem]
		                WHERE [RootItem].RootConsignmentItemID IS NULL
	                )
                )
                GO

                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[fn_LevenshteinDistance]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[fn_LevenshteinDistance]

                GO 

                CREATE FUNCTION [dbo].[fn_LevenshteinDistance](@firstString [nvarchar](4000), @secondString [nvarchar](4000), @ingoreCase [bit] = True)
                RETURNS [int] WITH EXECUTE AS CALLER
                AS 
                EXTERNAL NAME [SqlAssembly].[FuzzyStrings].[LevenshteinDistance]
                GO

                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[GetCalculatedProductLocalPriceWithShares_ReSale]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[GetCalculatedProductLocalPriceWithShares_ReSale]

                GO 

                CREATE FUNCTION [dbo].[GetCalculatedProductLocalPriceWithShares_ReSale]
                (
	                -- Add the parameters for the function here
	                @ProductNetId uniqueidentifier,
	                @ClientAgreementNetId uniqueidentifier,
	                @Culture nvarchar(2),
	                @OrderItemId bigint
                )
                RETURNS money
                AS
                BEGIN
	                -- Declare the return variable here
	                DECLARE @Price decimal(30,14);

	                -- Declare temporary variables here
	                DECLARE @ProductGroupId bigint;
	                DECLARE @ProductPrice money;
	                DECLARE @ProductId bigint;
	                DECLARE @ExtraCharge money;
	                DECLARE @PricingId bigint;
	                DECLARE @DiscountRate money;
	                DECLARE @PromotionalProduct bit;
	                DECLARE @PromotionalPricingId bigint;
	                DECLARE @OneTimeDiscount money;
	                DECLARE @ExchangeRate money;
	                DECLARE @IsPrintedPaymentInvoice bit;
	                DECLARE @CurrencyId bigint;

	                SET @CurrencyId = (
		                SELECT [Currency].[ID] FROM [ClientAgreement]
		                LEFT JOIN [Agreement]
		                ON [Agreement].[ID] = [ClientAgreement].[AgreementID]
		                LEFT JOIN [Currency]
		                ON [Currency].[ID] = [Agreement].[CurrencyID]
		                WHERE [ClientAgreement].[NetUID] = @ClientAgreementNetId
	                );

	                SET @OneTimeDiscount = 0;

	                SET @PromotionalPricingId = (
		                SELECT [Agreement].[PromotionalPricingID] 
			                FROM [Agreement]
				                LEFT JOIN [ClientAgreement]
				                ON [ClientAgreement].[AgreementID] = [Agreement].[ID]
		                WHERE [ClientAgreement].[NetUID] = @ClientAgreementNetId
	                );

	                SET @ProductId = (
		                SELECT TOP(1) [Product].ID
		                FROM [Product]
		                WHERE [Product].NetUID = @ProductNetId
	                );

	                SET @PromotionalProduct = (
		                SELECT TOP(1) 
			                ISNULL(
				                (
					                CASE 
						                WHEN [Product].ID IS NULL 
							                THEN 0
						                WHEN [Product].IsForSale = 1
							                THEN 1
						                WHEN [Product].IsForZeroSale = 1
							                THEN 1
						                WHEN [Product].[Top] = N'X9'
							                THEN 1
						                WHEN [Product].[Top] = N'Х9'
							                THEN 1
						                ELSE 0
					                END
				                )
			                , 0)
		                FROM [Product]
		                WHERE [Product].NetUID = @ProductNetId
	                );

	                SET @ProductGroupId = (
		                SELECT TOP(1) [ProductProductGroup].ProductGroupID
		                FROM [ProductProductGroup]
		                WHERE [ProductProductGroup].Deleted = 0
		                AND [ProductProductGroup].ProductID = @ProductId
	                );

	                SET @PricingId = (
		                SELECT TOP(1) ISNULL([Agreement].PricingID, 0)
		                FROM [ClientAgreement]
			                LEFT JOIN [Agreement]
				                ON [Agreement].ID = [ClientAgreement].AgreementID
		                WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
	                )
	                
	                SET @PricingId = dbo.GetBasePricingId(@PricingId)

	                SET @ProductPrice = (
		                SELECT TOP(1) [ProductPricing].Price
		                FROM [ProductPricing]
		                WHERE [ProductPricing].Deleted = 0
		                AND [ProductPricing].ProductID = @ProductId
		                AND [ProductPricing].PricingID = @PricingId
	                );

	                SET @DiscountRate = 0.00;

	                IF(@PromotionalProduct = 1)
		                BEGIN
			                IF (@ClientAgreementNetId <> '00000000-0000-0000-0000-000000000000')
				                BEGIN
					                IF(@PromotionalPricingId IS NULL)
						                BEGIN
							                SET @ExtraCharge = ISNULL(
							                (
								                SELECT TOP(1) [VatPricing].CalculatedExtraCharge
								                FROM [ClientAgreement]
									                LEFT JOIN [Agreement]
										                ON [ClientAgreement].AgreementID = [Agreement].ID
									                LEFT JOIN [Pricing]
										                ON [Agreement].PricingID = [Pricing].ID
									                LEFT JOIN [Pricing] AS [VatPricing]
										                ON [VatPricing].ID = (
											                SELECT TOP(1) [JoinPricing].ID
											                FROM [Pricing] AS [JoinPricing]
												                LEFT JOIN [PricingTranslation]
													                ON [PricingTranslation].PricingID = [JoinPricing].ID
													                AND [PricingTranslation].CultureCode = N'uk'
											                WHERE ISNULL([PricingTranslation].[Name], [JoinPricing].[Name]) LIKE [Pricing].[Name] + N'_%'
											                AND [JoinPricing].Deleted = 0
											                AND [JoinPricing].ForVat = 1
										                )
								                WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
								                ),
								                0.00
							                );
	                
							                SET @PricingId = (
									                SELECT TOP(1) [VatPricing].[ID]
									                FROM [ClientAgreement]
										                LEFT JOIN [Agreement]
											                ON [ClientAgreement].AgreementID = [Agreement].ID
										                LEFT JOIN [Pricing]
											                ON [Agreement].PricingID = [Pricing].ID
										                LEFT JOIN [Pricing] AS [VatPricing]
											                ON [VatPricing].ID = (
												                SELECT TOP(1) [JoinPricing].ID
												                FROM [Pricing] AS [JoinPricing]
													                LEFT JOIN [PricingTranslation]
														                ON [PricingTranslation].PricingID = [JoinPricing].ID
														                AND [PricingTranslation].CultureCode = N'uk'
												                WHERE ISNULL([PricingTranslation].[Name], [JoinPricing].[Name]) LIKE [Pricing].[Name] + N'_%'
												                AND [JoinPricing].Deleted = 0
												                AND [JoinPricing].ForVat = 1
											                )
									                WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
							                )
	                
							                SET @PricingId = dbo.GetBasePricingId(@PricingId)

							                SET @DiscountRate = ISNULL(
								                (
									                SELECT TOP(1) [ProductGroupDiscount].DiscountRate
									                FROM [ProductGroupDiscount]
										                LEFT JOIN [ClientAgreement]
											                ON [ProductGroupDiscount].ClientAgreementID = [ClientAgreement].ID
									                WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
									                AND [ProductGroupDiscount].ProductGroupID = @ProductGroupId
									                AND [ProductGroupDiscount].IsActive = 1
								                )
							                , 0.00);
						                END
					                ELSE
						                BEGIN
							                SET @PricingId = @PromotionalPricingId;
	                
							                SET @PricingId = (
								                SELECT TOP(1) [VatPricing].[ID]
								                FROM [ClientAgreement]
									                LEFT JOIN [Agreement]
										                ON [ClientAgreement].AgreementID = [Agreement].ID
									                LEFT JOIN [Pricing]
										                ON [Agreement].[PromotionalPricingID] = [Pricing].ID
									                LEFT JOIN [Pricing] AS [VatPricing]
										                ON [VatPricing].ID = (
											                SELECT TOP(1) [JoinPricing].ID
											                FROM [Pricing] AS [JoinPricing]
												                LEFT JOIN [PricingTranslation]
													                ON [PricingTranslation].PricingID = [JoinPricing].ID
													                AND [PricingTranslation].CultureCode = N'uk'
											                WHERE ISNULL([PricingTranslation].[Name], [JoinPricing].[Name]) LIKE [Pricing].[Name] + N'_%'
											                AND [JoinPricing].Deleted = 0
											                AND [JoinPricing].ForVat = 1
										                )
								                WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
							                );
	                
							                SET @ExtraCharge = ISNULL(
								                (
									                SELECT 
									                [Pricing].[CalculatedExtraCharge] 
									                FROM [Pricing]
									                WHERE [ID] = @PricingId
								                ),
								                (0.00)
							                );
			                
							                SET @PricingId = dbo.GetBasePricingId(@PricingId);

							                SET @DiscountRate = 0.00;
						                END
				                END
			                ELSE
				                BEGIN
					                SET @DiscountRate = 0.00;
	                
					                SET @ExtraCharge = (
						                SELECT TOP(1) 
							                CASE 
								                WHEN [PricingProductGroupDiscount].CalculatedExtraCharge IS NOT NULL
									                THEN [PricingProductGroupDiscount].CalculatedExtraCharge
								                ELSE [Pricing].CalculatedExtraCharge
							                END
						                FROM [Pricing]
							                LEFT JOIN [PricingProductGroupDiscount]
								                ON [PricingProductGroupDiscount].PricingID = [Pricing].ID
								                AND [PricingProductGroupDiscount].ProductGroupID = @ProductGroupId
								                AND [PricingProductGroupDiscount].Deleted = 0
						                WHERE [Pricing].Deleted = 0
						                AND [Pricing].Culture = @Culture
						                AND [Pricing].ForVat = 0
						                ORDER BY [Pricing].CalculatedExtraCharge DESC
					                );
				                END
		                END
	                ELSE
		                BEGIN
			                IF (@ClientAgreementNetId <> '00000000-0000-0000-0000-000000000000')
				                BEGIN
					                SET @ExtraCharge = ISNULL(
						                (
							                SELECT TOP(1) [VatPricing].CalculatedExtraCharge
							                FROM [ClientAgreement]
								                LEFT JOIN [Agreement]
									                ON [ClientAgreement].AgreementID = [Agreement].ID
								                LEFT JOIN [Pricing]
									                ON [Agreement].PricingID = [Pricing].ID
								                LEFT JOIN [Pricing] AS [VatPricing]
									                ON [VatPricing].ID = (
										                SELECT TOP(1) [JoinPricing].ID
										                FROM [Pricing] AS [JoinPricing]
											                LEFT JOIN [PricingTranslation]
												                ON [PricingTranslation].PricingID = [JoinPricing].ID
												                AND [PricingTranslation].CultureCode = N'uk'
										                WHERE ISNULL([PricingTranslation].[Name], [JoinPricing].[Name]) LIKE [Pricing].[Name] + N'_%'
										                AND [JoinPricing].Deleted = 0
										                AND [JoinPricing].ForVat = 1
									                )
							                WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
						                ),
						                0.00
					                );
	                
					                SET @PricingId = (
							                SELECT TOP(1) [VatPricing].[ID]
							                FROM [ClientAgreement]
								                LEFT JOIN [Agreement]
									                ON [ClientAgreement].AgreementID = [Agreement].ID
								                LEFT JOIN [Pricing]
									                ON [Agreement].PricingID = [Pricing].ID
								                LEFT JOIN [Pricing] AS [VatPricing]
									                ON [VatPricing].ID = (
										                SELECT TOP(1) [JoinPricing].ID
										                FROM [Pricing] AS [JoinPricing]
											                LEFT JOIN [PricingTranslation]
												                ON [PricingTranslation].PricingID = [JoinPricing].ID
												                AND [PricingTranslation].CultureCode = N'uk'
										                WHERE ISNULL([PricingTranslation].[Name], [JoinPricing].[Name]) LIKE [Pricing].[Name] + N'_%'
										                AND [JoinPricing].Deleted = 0
										                AND [JoinPricing].ForVat = 1
									                )
							                WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
					                )
	                
					                SET @PricingId = dbo.GetBasePricingId(@PricingId)
	                
					                SET @DiscountRate = ISNULL(
						                (
							                SELECT TOP(1) [ProductGroupDiscount].DiscountRate
							                FROM [ProductGroupDiscount]
								                LEFT JOIN [ClientAgreement]
									                ON [ProductGroupDiscount].ClientAgreementID = [ClientAgreement].ID
							                WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
							                AND [ProductGroupDiscount].ProductGroupID = @ProductGroupId
							                AND [ProductGroupDiscount].IsActive = 1
						                )
					                , 0.00);
				                END
			                ELSE
				                BEGIN
					                SET @DiscountRate = 0.00;
	                
					                SET @ExtraCharge = (
						                SELECT TOP(1) 
							                CASE 
								                WHEN [PricingProductGroupDiscount].CalculatedExtraCharge IS NOT NULL
									                THEN [PricingProductGroupDiscount].CalculatedExtraCharge
								                ELSE [Pricing].CalculatedExtraCharge
							                END
						                FROM [Pricing]
							                LEFT JOIN [PricingProductGroupDiscount]
								                ON [PricingProductGroupDiscount].PricingID = [Pricing].ID
								                AND [PricingProductGroupDiscount].ProductGroupID = @ProductGroupId
								                AND [PricingProductGroupDiscount].Deleted = 0
						                WHERE [Pricing].Deleted = 0
						                AND [Pricing].Culture = @Culture
						                AND [Pricing].ForVat = 0
						                ORDER BY [Pricing].CalculatedExtraCharge DESC
					                );
				                END
		                END


	                SET @ProductPrice = (
		                SELECT TOP(1) [ProductPricing].Price
		                FROM [ProductPricing]
		                WHERE [ProductPricing].Deleted = 0
		                AND [ProductPricing].ProductID = @ProductId
		                AND [ProductPricing].PricingID = @PricingId
	                );

	                IF(@OrderItemId IS NOT NULL)
		                BEGIN
			                SET @OneTimeDiscount = (
				                SELECT [OrderItem].[OneTimeDiscount] FROM [OrderItem]
				                WHERE [OrderItem].[ID] = @OrderItemId
			                );

			                SET @IsPrintedPaymentInvoice = (
				                SELECT TOP 1 [Sale].[IsPrintedPaymentInvoice] 
					                FROM [OrderItem]
						                LEFT JOIN [Order]
							                ON [Order].[ID] = [OrderItem].[OrderID]
						                LEFT JOIN [Sale]
							                ON [Sale].[OrderID] = [Order].[ID]
				                WHERE [OrderItem].[ID] = @OrderItemId
			                );

			                IF(@IsPrintedPaymentInvoice = 1)
				                SET @ExchangeRate = (
					                SELECT [OrderItem].[ExchangeRateAmount] FROM [OrderItem]
					                WHERE [OrderItem].[ID] = @OrderItemId
				                );
			                ELSE
				                SET @ExchangeRate = ISNULL(
					                dbo.GetCurrentEuroExchangeRateFiltered(@ProductNetId, @CurrencyId, 0, 1),
					                1
				                );
		                END
	                ELSE 
		                SET @ExchangeRate = ISNULL(
			                dbo.GetCurrentEuroExchangeRateFiltered(@ProductNetId, @CurrencyId, 0, 1),
			                1
		                );


	                -- Compute the result value

	                SET @Price = ISNULL(
		                (
			                ROUND(
				                ROUND(@ProductPrice + (@ProductPrice * (@ExtraCharge / 100)), 14)
				                -
				                ROUND(@ProductPrice + (@ProductPrice * (@ExtraCharge / 100)), 14) * (@DiscountRate / 100)
			                , 14)
			                -
			                (
				                ROUND(
					                ROUND(@ProductPrice + (@ProductPrice * @ExtraCharge / 100), 14)
					                -
					                ROUND(@ProductPrice + (@ProductPrice * @ExtraCharge / 100), 14) * (@DiscountRate / 100)
				                , 14)
				                *
				                @OneTimeDiscount / 100
			                )
		                )
		                *
		                @ExchangeRate
	                , 0.00);

	                -- Return the result of the function
	                RETURN @Price

                END
                GO

                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[GetCalculatedProductLocalPriceWithSharesAndVat]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[GetCalculatedProductLocalPriceWithSharesAndVat]

                GO 

                CREATE FUNCTION [dbo].[GetCalculatedProductLocalPriceWithSharesAndVat]
                    (
	                    -- Add the parameters for the function here
	                    @ProductNetId uniqueidentifier,
	                    @ClientAgreementNetId uniqueidentifier,
	                    @Culture nvarchar(2),
	                    @WithVat bit,
	                    @OrderItemId bigint
                    )
                    RETURNS decimal(30,14)
                    AS
                    BEGIN
	                    -- Declare the return variable here
	                    DECLARE @Price decimal(30,14);

	                    -- Declare temporary variables here
	                    DECLARE @ProductGroupId bigint;
	                    DECLARE @ProductPrice decimal(30,14);
	                    DECLARE @ProductId bigint;
	                    DECLARE @ExtraCharge decimal(30,14);
	                    DECLARE @DiscountRate decimal(30,14);
	                    DECLARE @PromotionalProduct bit;
	                    DECLARE @PricingId bigint;
	                    DECLARE @PromotionalPricingId bigint;
	                    DECLARE @OneTimeDiscount decimal(30,14);
	                    DECLARE @ExchangeRate decimal(30,14);
	                    DECLARE @IsPrintedPaymentInvoice bit;
	                    DECLARE @CurrencyId bigint;

	                    SET @CurrencyId = (
		                    SELECT [Currency].[ID] FROM [ClientAgreement]
		                    LEFT JOIN [Agreement]
		                    ON [Agreement].[ID] = [ClientAgreement].[AgreementID]
		                    LEFT JOIN [Currency]
		                    ON [Currency].[ID] = [Agreement].[CurrencyID]
		                    WHERE [ClientAgreement].[NetUID] = @ClientAgreementNetId
	                    );

	                    SET @OneTimeDiscount = 0;

	                    SET @PromotionalPricingId = (
		                    SELECT [Agreement].[PromotionalPricingID] 
			                    FROM [Agreement]
				                    LEFT JOIN [ClientAgreement]
				                    ON [ClientAgreement].[AgreementID] = [Agreement].[ID]
		                    WHERE [ClientAgreement].[NetUID] = @ClientAgreementNetId
	                    );

	                    SET @ProductId = (
		                    SELECT TOP(1) [Product].ID
		                    FROM [Product]
		                    WHERE [Product].NetUID = @ProductNetId
	                    );

	                    SET @PromotionalProduct = (
		                    SELECT TOP(1) 
			                    ISNULL(
				                    (
					                    CASE 
						                    WHEN [Product].ID IS NULL 
							                    THEN 0
						                    WHEN [Product].IsForSale = 1
							                    THEN 1
						                    WHEN [Product].IsForZeroSale = 1
							                    THEN 1
						                    WHEN [Product].[Top] = N'X9'
							                    THEN 1
						                    WHEN [Product].[Top] = N'Х9'
							                    THEN 1
						                    ELSE 0
					                    END
				                    )
			                    , 0)
		                    FROM [Product]
		                    WHERE [Product].NetUID = @ProductNetId
	                    );

	                    SET @ProductGroupId = (
		                    SELECT TOP(1) [ProductProductGroup].ProductGroupID
		                    FROM [ProductProductGroup]
		                    WHERE [ProductProductGroup].Deleted = 0
		                    AND [ProductProductGroup].ProductID = @ProductId
	                    );
	                    
	                    SET @PricingId = (
		                    SELECT TOP(1) ISNULL([Agreement].PricingID, 0)
		                    FROM [ClientAgreement]
			                    LEFT JOIN [Agreement]
				                    ON [Agreement].ID = [ClientAgreement].AgreementID
		                    WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
	                    )
	                    
	                    SET @PricingId = dbo.GetBasePricingId(@PricingId)

	                    SET @ProductPrice = (
		                    SELECT TOP(1) [ProductPricing].Price
		                    FROM [ProductPricing]
		                    WHERE [ProductPricing].Deleted = 0
		                    AND [ProductPricing].ProductID = @ProductId
		                    AND [ProductPricing].PricingID = @PricingId
	                    );

	                    SET @DiscountRate = 0.00;

	                    IF(@PromotionalProduct = 1)
		                    BEGIN
			                    IF (@ClientAgreementNetId <> '00000000-0000-0000-0000-000000000000')
				                    BEGIN
					                    IF(@PromotionalPricingId IS NULL)
						                    BEGIN
							                    SET @ExtraCharge = ISNULL(
								                    (
									                    SELECT TOP(1) 
										                    CASE 
											                    WHEN [PricingProductGroupDiscount].CalculatedExtraCharge IS NOT NULL
												                    THEN [PricingProductGroupDiscount].CalculatedExtraCharge
											                    ELSE [Pricing].CalculatedExtraCharge
										                    END
									                    FROM [ClientAgreement]
										                    LEFT JOIN [Agreement]
											                    ON [ClientAgreement].AgreementID = [Agreement].ID
										                    LEFT JOIN [Pricing]
											                    ON [Agreement].PricingID = [Pricing].ID
										                    LEFT JOIN [PricingProductGroupDiscount]
											                    ON [PricingProductGroupDiscount].PricingID = [Pricing].ID
											                    AND [PricingProductGroupDiscount].ProductGroupID = @ProductGroupId
											                    AND [PricingProductGroupDiscount].Deleted = 0
									                    WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
								                    ),
								                    (
									                    SELECT TOP(1) 
										                    CASE 
											                    WHEN [PricingProductGroupDiscount].CalculatedExtraCharge IS NOT NULL
												                    THEN [PricingProductGroupDiscount].CalculatedExtraCharge
											                    ELSE [Pricing].CalculatedExtraCharge
										                    END
									                    FROM [Pricing]
										                    LEFT JOIN [PricingProductGroupDiscount]
											                    ON [PricingProductGroupDiscount].PricingID = [Pricing].ID
											                    AND [PricingProductGroupDiscount].ProductGroupID = @ProductGroupId
											                    AND [PricingProductGroupDiscount].Deleted = 0
									                    WHERE [Pricing].Deleted = 0
									                    AND [Pricing].Culture = @Culture
									                    AND [Pricing].ForVat = 0
									                    ORDER BY [Pricing].CalculatedExtraCharge DESC
								                    )
							                    );

							                    SET @DiscountRate = ISNULL(
								                    (
									                    SELECT TOP(1) [ProductGroupDiscount].DiscountRate
									                    FROM [ProductGroupDiscount]
										                    LEFT JOIN [ClientAgreement]
											                    ON [ProductGroupDiscount].ClientAgreementID = [ClientAgreement].ID
									                    WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
									                    AND [ProductGroupDiscount].ProductGroupID = @ProductGroupId
									                    AND [ProductGroupDiscount].IsActive = 1
								                    )
							                    , 0.00);
						                    END
					                    ELSE 
						                    BEGIN
							                    SET @ExtraCharge = ISNULL((SELECT TOP(1) 
								                    CASE 
										                    WHEN [PricingProductGroupDiscount].CalculatedExtraCharge IS NOT NULL
											                    THEN [PricingProductGroupDiscount].CalculatedExtraCharge
										                    ELSE [Pricing].CalculatedExtraCharge
									                    END
								                    FROM [ClientAgreement]
									                    LEFT JOIN [Agreement]
										                    ON [ClientAgreement].AgreementID = [Agreement].ID
									                    LEFT JOIN [Pricing]
										                    ON [Agreement].[PromotionalPricingID] = [Pricing].ID
									                    LEFT JOIN [PricingProductGroupDiscount]
										                    ON [PricingProductGroupDiscount].PricingID = [Pricing].ID
										                    AND [PricingProductGroupDiscount].ProductGroupID = @ProductGroupId
										                    AND [PricingProductGroupDiscount].Deleted = 0
								                    WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
							                    ), 0.00);
					                    
							                    SET @PricingId = dbo.GetBasePricingId(@PromotionalPricingId);
						                    END

					                    SET @ProductPrice = (
						                    SELECT TOP(1) [ProductPricing].Price
						                    FROM [ProductPricing]
						                    WHERE [ProductPricing].Deleted = 0
						                    AND [ProductPricing].ProductID = @ProductId
						                    AND [ProductPricing].PricingID = @PricingId
					                    );
				                    END
			                    ELSE
				                    BEGIN
					                    SET @DiscountRate = 0.00;

					                    SET @ExtraCharge = (
						                    SELECT TOP(1) 
							                    CASE 
								                    WHEN [PricingProductGroupDiscount].CalculatedExtraCharge IS NOT NULL
									                    THEN [PricingProductGroupDiscount].CalculatedExtraCharge
								                    ELSE [Pricing].CalculatedExtraCharge
							                    END
						                    FROM [Pricing]
							                    LEFT JOIN [PricingProductGroupDiscount]
								                    ON [PricingProductGroupDiscount].PricingID = [Pricing].ID
								                    AND [PricingProductGroupDiscount].ProductGroupID = @ProductGroupId
								                    AND [PricingProductGroupDiscount].Deleted = 0
						                    WHERE [Pricing].Deleted = 0
						                    AND [Pricing].Culture = @Culture
						                    AND [Pricing].ForVat = 0
						                    ORDER BY [Pricing].CalculatedExtraCharge DESC
					                    );
				                    END
		                    END
	                    ELSE
		                    BEGIN
			                    IF (@ClientAgreementNetId <> '00000000-0000-0000-0000-000000000000')
				                    BEGIN
					                    SET @DiscountRate = ISNULL(
						                    (
							                    SELECT TOP(1) [ProductGroupDiscount].DiscountRate
							                    FROM [ProductGroupDiscount]
								                    LEFT JOIN [ClientAgreement]
									                    ON [ProductGroupDiscount].ClientAgreementID = [ClientAgreement].ID
							                    WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
							                    AND [ProductGroupDiscount].ProductGroupID = @ProductGroupId
							                    AND [ProductGroupDiscount].IsActive = 1
						                    )
					                    , 0.00);

					                    SET @ExtraCharge = ISNULL(
						                    (
							                    SELECT TOP(1) 
								                    CASE 
									                    WHEN [PricingProductGroupDiscount].CalculatedExtraCharge IS NOT NULL
										                    THEN [PricingProductGroupDiscount].CalculatedExtraCharge
									                    ELSE [Pricing].CalculatedExtraCharge
								                    END
							                    FROM [ClientAgreement]
								                    LEFT JOIN [Agreement]
									                    ON [ClientAgreement].AgreementID = [Agreement].ID
								                    LEFT JOIN [Pricing]
									                    ON [Agreement].PricingID = [Pricing].ID
								                    LEFT JOIN [PricingProductGroupDiscount]
									                    ON [PricingProductGroupDiscount].PricingID = [Pricing].ID
									                    AND [PricingProductGroupDiscount].ProductGroupID = @ProductGroupId
									                    AND [PricingProductGroupDiscount].Deleted = 0
							                    WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
						                    ),
						                    (
							                    SELECT TOP(1) 
								                    CASE 
									                    WHEN [PricingProductGroupDiscount].CalculatedExtraCharge IS NOT NULL
										                    THEN [PricingProductGroupDiscount].CalculatedExtraCharge
									                    ELSE [Pricing].CalculatedExtraCharge
								                    END
							                    FROM [Pricing]
								                    LEFT JOIN [PricingProductGroupDiscount]
									                    ON [PricingProductGroupDiscount].PricingID = [Pricing].ID
									                    AND [PricingProductGroupDiscount].ProductGroupID = @ProductGroupId
									                    AND [PricingProductGroupDiscount].Deleted = 0
							                    WHERE [Pricing].Deleted = 0
							                    AND [Pricing].Culture = @Culture
							                    AND [Pricing].ForVat = 0
							                    ORDER BY [Pricing].CalculatedExtraCharge DESC
						                    )
					                    )
				                    END
			                    ELSE
				                    BEGIN
					                    SET @DiscountRate = 0.00;

					                    SET @ExtraCharge = (
						                    SELECT TOP(1) 
							                    CASE 
								                    WHEN [PricingProductGroupDiscount].CalculatedExtraCharge IS NOT NULL
									                    THEN [PricingProductGroupDiscount].CalculatedExtraCharge
								                    ELSE [Pricing].CalculatedExtraCharge
							                    END
						                    FROM [Pricing]
							                    LEFT JOIN [PricingProductGroupDiscount]
								                    ON [PricingProductGroupDiscount].PricingID = [Pricing].ID
								                    AND [PricingProductGroupDiscount].ProductGroupID = @ProductGroupId
								                    AND [PricingProductGroupDiscount].Deleted = 0
						                    WHERE [Pricing].Deleted = 0
						                    AND [Pricing].Culture = @Culture
						                    AND [Pricing].ForVat = 0
						                    ORDER BY [Pricing].CalculatedExtraCharge DESC
					                    );
				                    END
		                    END

	                    IF(@OrderItemId IS NOT NULL)
		                    BEGIN
			                    SET @OneTimeDiscount = (
				                    SELECT [OrderItem].[OneTimeDiscount] FROM [OrderItem]
				                    WHERE [OrderItem].[ID] = @OrderItemId
			                    );

			                    SET @IsPrintedPaymentInvoice = (
				                    SELECT TOP 1 [Sale].[IsPrintedPaymentInvoice] 
					                    FROM [OrderItem]
						                    LEFT JOIN [Order]
							                    ON [Order].[ID] = [OrderItem].[OrderID]
						                    LEFT JOIN [Sale]
							                    ON [Sale].[OrderID] = [Order].[ID]
				                    WHERE [OrderItem].[ID] = @OrderItemId
			                    );

			                    IF(@IsPrintedPaymentInvoice = 1)
				                    SET @ExchangeRate = (
					                    SELECT [OrderItem].[ExchangeRateAmount] FROM [OrderItem]
					                    WHERE [OrderItem].[ID] = @OrderItemId
				                    );
			                    ELSE
				                    SET @ExchangeRate = ISNULL(
					                    dbo.[GetCurrentEuroExchangeRateFiltered](@ProductNetId, @CurrencyId, @WithVat, 0),
					                    1
				                    );
		                    END
	                    ELSE 
		                    SET @ExchangeRate = ISNULL(
			                    dbo.[GetCurrentEuroExchangeRateFiltered](@ProductNetId, @CurrencyId, @WithVat, 0),
			                    1
		                    );
	                    -- Compute the result value

	                    SET @Price = ISNULL(
		                    (
			                    ROUND(
				                    ROUND(@ProductPrice + (@ProductPrice * (@ExtraCharge / 100)), 14)
				                    -
				                    ROUND(@ProductPrice + (@ProductPrice * (@ExtraCharge / 100)), 14) * (@DiscountRate / 100)
			                    , 14)
			                    -
			                    (
				                    ROUND(
					                    ROUND(@ProductPrice + (@ProductPrice * @ExtraCharge / 100), 14)
					                    -
					                    ROUND(@ProductPrice + (@ProductPrice * @ExtraCharge / 100), 14) * (@DiscountRate / 100)
				                    , 14)
				                    *
				                    @OneTimeDiscount / 100
			                    )
		                    )
		                    *
		                    @ExchangeRate
	                    , 0.00);

	                    -- Return the result of the function
	                    RETURN @Price

                    END
                    GO

                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[GetCalculatedProductPriceWithShares_ReSale]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[GetCalculatedProductPriceWithShares_ReSale]

                GO 

                CREATE FUNCTION [dbo].[GetCalculatedProductPriceWithShares_ReSale]
                (
	                -- Add the parameters for the function here
	                @ProductNetId uniqueidentifier,
	                @ClientAgreementNetId uniqueidentifier,
	                @Culture nvarchar(2),
	                @OrderItemId bigint
                )
                RETURNS money
                AS
                BEGIN
	                -- Declare the return variable here
	                DECLARE @Price decimal(30,14);

	                -- Declare temporary variables here
	                DECLARE @ProductGroupId bigint;
	                DECLARE @ProductPrice money;
	                DECLARE @ProductId bigint;
	                DECLARE @ExtraCharge money;
	                DECLARE @PricingId bigint;
	                DECLARE @DiscountRate money;
	                DECLARE @PromotionalProduct bit;
	                DECLARE @PromotionalPricingId bigint;
	                DECLARE @OneTimeDiscount money;

	                SET @OneTimeDiscount = 0;

	                SET @PromotionalPricingId = (
		                SELECT [Agreement].[PromotionalPricingID] 
			                FROM [Agreement]
				                LEFT JOIN [ClientAgreement]
				                ON [ClientAgreement].[AgreementID] = [Agreement].[ID]
		                WHERE [ClientAgreement].[NetUID] = @ClientAgreementNetId
	                );

	                SET @ProductId = (
		                SELECT TOP(1) [Product].ID
		                FROM [Product]
		                WHERE [Product].NetUID = @ProductNetId
	                );

	                SET @PromotionalProduct = (
		                SELECT TOP(1) 
			                ISNULL(
				                (
					                CASE 
						                WHEN [Product].ID IS NULL 
							                THEN 0
						                WHEN [Product].IsForSale = 1
							                THEN 1
						                WHEN [Product].IsForZeroSale = 1
							                THEN 1
						                WHEN [Product].[Top] = N'X9'
							                THEN 1
						                WHEN [Product].[Top] = N'Х9'
							                THEN 1
						                ELSE 0
					                END
				                )
			                , 0)
		                FROM [Product]
		                WHERE [Product].NetUID = @ProductNetId
	                );

	                SET @ProductGroupId = (
		                SELECT TOP(1) [ProductProductGroup].ProductGroupID
		                FROM [ProductProductGroup]
		                WHERE [ProductProductGroup].Deleted = 0
		                AND [ProductProductGroup].ProductID = @ProductId
	                );

	                SET @PricingId = (
		                SELECT TOP(1) ISNULL([Agreement].PricingID, 0)
		                FROM [ClientAgreement]
			                LEFT JOIN [Agreement]
				                ON [Agreement].ID = [ClientAgreement].AgreementID
		                WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
	                )
	                
	                SET @PricingId = dbo.GetBasePricingId(@PricingId)

	                SET @ProductPrice = (
		                SELECT TOP(1) [ProductPricing].Price
		                FROM [ProductPricing]
		                WHERE [ProductPricing].Deleted = 0
		                AND [ProductPricing].ProductID = @ProductId
		                AND [ProductPricing].PricingID = @PricingId
	                );

	                SET @DiscountRate = 0.00;

	                IF(@PromotionalProduct = 1)
		                BEGIN
			                IF (@ClientAgreementNetId <> '00000000-0000-0000-0000-000000000000')
				                BEGIN
					                IF(@PromotionalPricingId IS NULL)
						                BEGIN
							                SET @ExtraCharge = ISNULL(
								                (
									                SELECT TOP(1) [VatPricing].CalculatedExtraCharge
									                FROM [ClientAgreement]
										                LEFT JOIN [Agreement]
											                ON [ClientAgreement].AgreementID = [Agreement].ID
										                LEFT JOIN [Pricing]
											                ON [Agreement].PricingID = [Pricing].ID
										                LEFT JOIN [Pricing] AS [VatPricing]
											                ON [VatPricing].ID = (
												                SELECT TOP(1) [JoinPricing].ID
												                FROM [Pricing] AS [JoinPricing]
													                LEFT JOIN [PricingTranslation]
														                ON [PricingTranslation].PricingID = [JoinPricing].ID
														                AND [PricingTranslation].CultureCode = N'uk'
												                WHERE ISNULL([PricingTranslation].[Name], [JoinPricing].[Name]) LIKE [Pricing].[Name] + N'_%'
												                AND [JoinPricing].Deleted = 0
												                AND [JoinPricing].ForVat = 1
											                )
									                WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
								                ),
								                0.00
							                );
	                
							                SET @PricingId = (
									                SELECT TOP(1) [VatPricing].[ID]
									                FROM [ClientAgreement]
										                LEFT JOIN [Agreement]
											                ON [ClientAgreement].AgreementID = [Agreement].ID
										                LEFT JOIN [Pricing]
											                ON [Agreement].PricingID = [Pricing].ID
										                LEFT JOIN [Pricing] AS [VatPricing]
											                ON [VatPricing].ID = (
												                SELECT TOP(1) [JoinPricing].ID
												                FROM [Pricing] AS [JoinPricing]
													                LEFT JOIN [PricingTranslation]
														                ON [PricingTranslation].PricingID = [JoinPricing].ID
														                AND [PricingTranslation].CultureCode = N'uk'
												                WHERE ISNULL([PricingTranslation].[Name], [JoinPricing].[Name]) LIKE [Pricing].[Name] + N'_%'
												                AND [JoinPricing].Deleted = 0
												                AND [JoinPricing].ForVat = 1
											                )
									                WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
							                )
	                
							                SET @PricingId = dbo.GetBasePricingId(@PricingId);

							                SET @DiscountRate = ISNULL(
								                (
									                SELECT TOP(1) [ProductGroupDiscount].DiscountRate
									                FROM [ProductGroupDiscount]
										                LEFT JOIN [ClientAgreement]
											                ON [ProductGroupDiscount].ClientAgreementID = [ClientAgreement].ID
									                WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
									                AND [ProductGroupDiscount].ProductGroupID = @ProductGroupId
									                AND [ProductGroupDiscount].IsActive = 1
								                )
							                , 0.00);
						                END
					                ELSE
						                BEGIN
							                SET @PricingId = (
								                SELECT TOP(1) [VatPricing].[ID]
								                FROM [ClientAgreement]
									                LEFT JOIN [Agreement]
										                ON [ClientAgreement].AgreementID = [Agreement].ID
									                LEFT JOIN [Pricing]
										                ON [Agreement].[PromotionalPricingID] = [Pricing].ID
									                LEFT JOIN [Pricing] AS [VatPricing]
										                ON [VatPricing].ID = (
											                SELECT TOP(1) [JoinPricing].ID
											                FROM [Pricing] AS [JoinPricing]
												                LEFT JOIN [PricingTranslation]
													                ON [PricingTranslation].PricingID = [JoinPricing].ID
													                AND [PricingTranslation].CultureCode = N'uk'
											                WHERE ISNULL([PricingTranslation].[Name], [JoinPricing].[Name]) LIKE [Pricing].[Name] + N'_%'
											                AND [JoinPricing].Deleted = 0
											                AND [JoinPricing].ForVat = 1
										                )
								                WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
							                );
	                
							                SET @ExtraCharge = ISNULL(
								                (
									                SELECT 
									                [Pricing].[CalculatedExtraCharge] 
									                FROM [Pricing]
									                WHERE [ID] = @PricingId
								                ),
								                (0.00)
							                );
			                
							                SET @PricingId = dbo.GetBasePricingId(@PricingId);
						                END
				                END
			                ELSE
				                BEGIN
					                SET @DiscountRate = 0.00;
	                
					                SET @ExtraCharge = (
						                SELECT TOP(1) 
							                CASE 
								                WHEN [PricingProductGroupDiscount].CalculatedExtraCharge IS NOT NULL
									                THEN [PricingProductGroupDiscount].CalculatedExtraCharge
								                ELSE [Pricing].CalculatedExtraCharge
							                END
						                FROM [Pricing]
							                LEFT JOIN [PricingProductGroupDiscount]
								                ON [PricingProductGroupDiscount].PricingID = [Pricing].ID
								                AND [PricingProductGroupDiscount].ProductGroupID = @ProductGroupId
								                AND [PricingProductGroupDiscount].Deleted = 0
						                WHERE [Pricing].Deleted = 0
						                AND [Pricing].Culture = @Culture
						                AND [Pricing].ForVat = 0
						                ORDER BY [Pricing].CalculatedExtraCharge DESC
					                );
				                END
		                END
	                ELSE
		                BEGIN
			                IF (@ClientAgreementNetId <> '00000000-0000-0000-0000-000000000000')
				                BEGIN
					                SET @ExtraCharge = ISNULL(
						                (
							                SELECT TOP(1) [VatPricing].CalculatedExtraCharge
							                FROM [ClientAgreement]
								                LEFT JOIN [Agreement]
									                ON [ClientAgreement].AgreementID = [Agreement].ID
								                LEFT JOIN [Pricing]
									                ON [Agreement].PricingID = [Pricing].ID
								                LEFT JOIN [Pricing] AS [VatPricing]
									                ON [VatPricing].ID = (
										                SELECT TOP(1) [JoinPricing].ID
										                FROM [Pricing] AS [JoinPricing]
											                LEFT JOIN [PricingTranslation]
												                ON [PricingTranslation].PricingID = [JoinPricing].ID
												                AND [PricingTranslation].CultureCode = N'uk'
										                WHERE ISNULL([PricingTranslation].[Name], [JoinPricing].[Name]) LIKE [Pricing].[Name] + N'_%'
										                AND [JoinPricing].Deleted = 0
										                AND [JoinPricing].ForVat = 1
									                )
							                WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
						                ),
						                0.00
					                );
	                
					                SET @PricingId = (
							                SELECT TOP(1) [VatPricing].[ID]
							                FROM [ClientAgreement]
								                LEFT JOIN [Agreement]
									                ON [ClientAgreement].AgreementID = [Agreement].ID
								                LEFT JOIN [Pricing]
									                ON [Agreement].PricingID = [Pricing].ID
								                LEFT JOIN [Pricing] AS [VatPricing]
									                ON [VatPricing].ID = (
										                SELECT TOP(1) [JoinPricing].ID
										                FROM [Pricing] AS [JoinPricing]
											                LEFT JOIN [PricingTranslation]
												                ON [PricingTranslation].PricingID = [JoinPricing].ID
												                AND [PricingTranslation].CultureCode = N'uk'
										                WHERE ISNULL([PricingTranslation].[Name], [JoinPricing].[Name]) LIKE [Pricing].[Name] + N'_%'
										                AND [JoinPricing].Deleted = 0
										                AND [JoinPricing].ForVat = 1
									                )
							                WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
					                )
	                
					                SET @PricingId = dbo.GetBasePricingId(@PricingId)
	                
					                SET @DiscountRate = ISNULL(
						                (
							                SELECT TOP(1) [ProductGroupDiscount].DiscountRate
							                FROM [ProductGroupDiscount]
								                LEFT JOIN [ClientAgreement]
									                ON [ProductGroupDiscount].ClientAgreementID = [ClientAgreement].ID
							                WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
							                AND [ProductGroupDiscount].ProductGroupID = @ProductGroupId
							                AND [ProductGroupDiscount].IsActive = 1
						                )
					                , 0.00);
				                END
			                ELSE
				                BEGIN
					                SET @DiscountRate = 0.00;
	                
					                SET @ExtraCharge = (
						                SELECT TOP(1) 
							                CASE 
								                WHEN [PricingProductGroupDiscount].CalculatedExtraCharge IS NOT NULL
									                THEN [PricingProductGroupDiscount].CalculatedExtraCharge
								                ELSE [Pricing].CalculatedExtraCharge
							                END
						                FROM [Pricing]
							                LEFT JOIN [PricingProductGroupDiscount]
								                ON [PricingProductGroupDiscount].PricingID = [Pricing].ID
								                AND [PricingProductGroupDiscount].ProductGroupID = @ProductGroupId
								                AND [PricingProductGroupDiscount].Deleted = 0
						                WHERE [Pricing].Deleted = 0
						                AND [Pricing].Culture = @Culture
						                AND [Pricing].ForVat = 0
						                ORDER BY [Pricing].CalculatedExtraCharge DESC
					                );
				                END
		                END

	                SET @ProductPrice = (
		                SELECT TOP(1) [ProductPricing].Price
		                FROM [ProductPricing]
		                WHERE [ProductPricing].Deleted = 0
		                AND [ProductPricing].ProductID = @ProductId
		                AND [ProductPricing].PricingID = @PricingId
	                );

	                IF(@OrderItemId IS NOT NULL)
		                SET @OneTimeDiscount = (
			                SELECT [OrderItem].[OneTimeDiscount] FROM [OrderItem]
			                WHERE [OrderItem].[ID] = @OrderItemId
		                );

	                -- Compute the result value

	                SET @Price = ISNULL(
		                ROUND(
			                ROUND(@ProductPrice + (@ProductPrice * (@ExtraCharge / 100)), 14)
			                -
			                ROUND(@ProductPrice + (@ProductPrice * (@ExtraCharge / 100)), 14) * (@DiscountRate / 100)
		                , 14)
		                -
		                (
			                ROUND(
				                ROUND(@ProductPrice + (@ProductPrice * @ExtraCharge / 100), 14)
				                -
				                ROUND(@ProductPrice + (@ProductPrice * @ExtraCharge / 100), 14) * (@DiscountRate / 100)
			                , 14)
			                *
			                @OneTimeDiscount / 100
		                )
	                , 0.00);

	                -- Return the result of the function
	                RETURN @Price

                END
                GO

                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[GetCalculatedProductPriceWithSharesAndVat]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[GetCalculatedProductPriceWithSharesAndVat]

                GO 

                CREATE FUNCTION [dbo].[GetCalculatedProductPriceWithSharesAndVat]
                (
	                -- Add the parameters for the function here
	                @ProductNetId uniqueidentifier,
	                @ClientAgreementNetId uniqueidentifier,
	                @Culture nvarchar(2),
	                @WithVat bit,
	                @OrderItemId bigint
                )
                RETURNS decimal(30,14)
                AS
                BEGIN
	                -- Declare the return variable here
	                DECLARE @Price decimal(30,14);

	                -- Declare temporary variables here
	                DECLARE @ProductGroupId bigint;
	                DECLARE @ProductPrice decimal(30,14);
	                DECLARE @ProductId bigint;
	                DECLARE @ExtraCharge decimal(30,14);
	                DECLARE @DiscountRate decimal(30,14);
	                DECLARE @PromotionalProduct bit;
	                DECLARE @PricingId bigint;
	                DECLARE @PromotionalPricingId bigint;
	                DECLARE @OneTimeDiscount decimal(30,14);

	                SET @OneTimeDiscount = 0;

	                SET @PromotionalPricingId = (
		                SELECT [Agreement].[PromotionalPricingID] 
			                FROM [Agreement]
				                LEFT JOIN [ClientAgreement]
				                ON [ClientAgreement].[AgreementID] = [Agreement].[ID]
		                WHERE [ClientAgreement].[NetUID] = @ClientAgreementNetId
	                );

	                SET @ProductId = (
		                SELECT TOP(1) [Product].ID
		                FROM [Product]
		                WHERE [Product].NetUID = @ProductNetId
	                );

	                SET @PromotionalProduct = (
		                SELECT TOP(1) 
			                ISNULL(
				                (
					                CASE 
						                WHEN [Product].ID IS NULL 
							                THEN 0
						                WHEN [Product].IsForSale = 1
							                THEN 1
						                WHEN [Product].IsForZeroSale = 1
							                THEN 1
						                WHEN [Product].[Top] = N'X9'
							                THEN 1
						                WHEN [Product].[Top] = N'Х9'
							                THEN 1
						                ELSE 0
					                END
				                )
			                , 0)
		                FROM [Product]
		                WHERE [Product].NetUID = @ProductNetId
	                );

	                SET @ProductGroupId = (
		                SELECT TOP(1) [ProductProductGroup].ProductGroupID
		                FROM [ProductProductGroup]
		                WHERE [ProductProductGroup].Deleted = 0
		                AND [ProductProductGroup].ProductID = @ProductId
	                );

	                SET @PricingId = (
		                SELECT TOP(1) ISNULL([Agreement].PricingID, 0)
		                FROM [ClientAgreement]
			                LEFT JOIN [Agreement]
				                ON [Agreement].ID = [ClientAgreement].AgreementID
		                WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
	                )
	                
	                SET @PricingId = dbo.GetBasePricingId(@PricingId)

	                SET @ProductPrice = (
		                SELECT TOP(1) [ProductPricing].Price
		                FROM [ProductPricing]
		                WHERE [ProductPricing].Deleted = 0
		                AND [ProductPricing].ProductID = @ProductId
		                AND [ProductPricing].PricingID = @PricingId
	                );

	                SET @DiscountRate = 0.00;

	                IF(@PromotionalProduct = 1)
		                BEGIN
			                IF (@ClientAgreementNetId <> '00000000-0000-0000-0000-000000000000')
				                BEGIN
					                IF(@PromotionalPricingId IS NULL)
						                BEGIN
							                SET @DiscountRate = ISNULL(
								                (
									                SELECT TOP(1) [ProductGroupDiscount].DiscountRate
									                FROM [ProductGroupDiscount]
										                LEFT JOIN [ClientAgreement]
											                ON [ProductGroupDiscount].ClientAgreementID = [ClientAgreement].ID
									                WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
									                AND [ProductGroupDiscount].ProductGroupID = @ProductGroupId
									                AND [ProductGroupDiscount].IsActive = 1
								                )
							                , 0.00);

							                SET @ExtraCharge = ISNULL(
								                (
									                SELECT TOP(1) 
										                CASE 
											                WHEN [PricingProductGroupDiscount].CalculatedExtraCharge IS NOT NULL
												                THEN [PricingProductGroupDiscount].CalculatedExtraCharge
											                ELSE [Pricing].CalculatedExtraCharge
										                END
									                FROM [ClientAgreement]
										                LEFT JOIN [Agreement]
											                ON [ClientAgreement].AgreementID = [Agreement].ID
										                LEFT JOIN [Pricing]
											                ON [Agreement].PricingID = [Pricing].ID
										                LEFT JOIN [PricingProductGroupDiscount]
											                ON [PricingProductGroupDiscount].PricingID = [Pricing].ID
											                AND [PricingProductGroupDiscount].ProductGroupID = @ProductGroupId
											                AND [PricingProductGroupDiscount].Deleted = 0
									                WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
								                ),
								                (
									                SELECT TOP(1) 
										                CASE 
											                WHEN [PricingProductGroupDiscount].CalculatedExtraCharge IS NOT NULL
												                THEN [PricingProductGroupDiscount].CalculatedExtraCharge
											                ELSE [Pricing].CalculatedExtraCharge
										                END
									                FROM [Pricing]
										                LEFT JOIN [PricingProductGroupDiscount]
											                ON [PricingProductGroupDiscount].PricingID = [Pricing].ID
											                AND [PricingProductGroupDiscount].ProductGroupID = @ProductGroupId
											                AND [PricingProductGroupDiscount].Deleted = 0
									                WHERE [Pricing].Deleted = 0
									                AND [Pricing].Culture = @Culture
									                AND [Pricing].ForVat = 0
									                ORDER BY [Pricing].CalculatedExtraCharge DESC
								                )
							                )
						                END
					                ELSE 
						                BEGIN
							                SET @ExtraCharge = ISNULL((SELECT TOP(1) 
								                CASE 
										                WHEN [PricingProductGroupDiscount].CalculatedExtraCharge IS NOT NULL
											                THEN [PricingProductGroupDiscount].CalculatedExtraCharge
										                ELSE [Pricing].CalculatedExtraCharge
									                END
								                FROM [ClientAgreement]
									                LEFT JOIN [Agreement]
										                ON [ClientAgreement].AgreementID = [Agreement].ID
									                LEFT JOIN [Pricing]
										                ON [Agreement].[PromotionalPricingID] = [Pricing].ID
									                LEFT JOIN [PricingProductGroupDiscount]
										                ON [PricingProductGroupDiscount].PricingID = [Pricing].ID
										                AND [PricingProductGroupDiscount].ProductGroupID = @ProductGroupId
										                AND [PricingProductGroupDiscount].Deleted = 0
								                WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
							                ), 0.00);
					                
							                SET @PricingId = dbo.GetBasePricingId(@PromotionalPricingId);
						                END

					                SET @ProductPrice = (
						                SELECT TOP(1) [ProductPricing].Price
						                FROM [ProductPricing]
						                WHERE [ProductPricing].Deleted = 0
						                AND [ProductPricing].ProductID = @ProductId
						                AND [ProductPricing].PricingID = @PricingId
					                );
				                END
			                ELSE
				                BEGIN
					                SET @DiscountRate = 0.00;

					                SET @ExtraCharge = (
						                SELECT TOP(1) 
							                CASE 
								                WHEN [PricingProductGroupDiscount].CalculatedExtraCharge IS NOT NULL
									                THEN [PricingProductGroupDiscount].CalculatedExtraCharge
								                ELSE [Pricing].CalculatedExtraCharge
							                END
						                FROM [Pricing]
							                LEFT JOIN [PricingProductGroupDiscount]
								                ON [PricingProductGroupDiscount].PricingID = [Pricing].ID
								                AND [PricingProductGroupDiscount].ProductGroupID = @ProductGroupId
								                AND [PricingProductGroupDiscount].Deleted = 0
						                WHERE [Pricing].Deleted = 0
						                AND [Pricing].Culture = @Culture
						                AND [Pricing].ForVat = 0
						                ORDER BY [Pricing].CalculatedExtraCharge DESC
					                );
				                END
		                END
	                ELSE
		                BEGIN
			                IF (@ClientAgreementNetId <> '00000000-0000-0000-0000-000000000000')
				                BEGIN
					                SET @DiscountRate = ISNULL(
						                (
							                SELECT TOP(1) [ProductGroupDiscount].DiscountRate
							                FROM [ProductGroupDiscount]
								                LEFT JOIN [ClientAgreement]
									                ON [ProductGroupDiscount].ClientAgreementID = [ClientAgreement].ID
							                WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
							                AND [ProductGroupDiscount].ProductGroupID = @ProductGroupId
							                AND [ProductGroupDiscount].IsActive = 1
						                )
					                , 0.00);

					                SET @ExtraCharge = ISNULL(
						                (
							                SELECT TOP(1) 
								                CASE 
									                WHEN [PricingProductGroupDiscount].CalculatedExtraCharge IS NOT NULL
										                THEN [PricingProductGroupDiscount].CalculatedExtraCharge
									                ELSE [Pricing].CalculatedExtraCharge
								                END
							                FROM [ClientAgreement]
								                LEFT JOIN [Agreement]
									                ON [ClientAgreement].AgreementID = [Agreement].ID
								                LEFT JOIN [Pricing]
									                ON [Agreement].PricingID = [Pricing].ID
								                LEFT JOIN [PricingProductGroupDiscount]
									                ON [PricingProductGroupDiscount].PricingID = [Pricing].ID
									                AND [PricingProductGroupDiscount].ProductGroupID = @ProductGroupId
									                AND [PricingProductGroupDiscount].Deleted = 0
							                WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
						                ),
						                (
							                SELECT TOP(1) 
								                CASE 
									                WHEN [PricingProductGroupDiscount].CalculatedExtraCharge IS NOT NULL
										                THEN [PricingProductGroupDiscount].CalculatedExtraCharge
									                ELSE [Pricing].CalculatedExtraCharge
								                END
							                FROM [Pricing]
								                LEFT JOIN [PricingProductGroupDiscount]
									                ON [PricingProductGroupDiscount].PricingID = [Pricing].ID
									                AND [PricingProductGroupDiscount].ProductGroupID = @ProductGroupId
									                AND [PricingProductGroupDiscount].Deleted = 0
							                WHERE [Pricing].Deleted = 0
							                AND [Pricing].Culture = @Culture
							                AND [Pricing].ForVat = 0
							                ORDER BY [Pricing].CalculatedExtraCharge DESC
						                )
					                )
				                END
			                ELSE
				                BEGIN
					                SET @DiscountRate = 0.00;

					                SET @ExtraCharge = (
						                SELECT TOP(1) 
							                CASE 
								                WHEN [PricingProductGroupDiscount].CalculatedExtraCharge IS NOT NULL
									                THEN [PricingProductGroupDiscount].CalculatedExtraCharge
								                ELSE [Pricing].CalculatedExtraCharge
							                END
						                FROM [Pricing]
							                LEFT JOIN [PricingProductGroupDiscount]
								                ON [PricingProductGroupDiscount].PricingID = [Pricing].ID
								                AND [PricingProductGroupDiscount].ProductGroupID = @ProductGroupId
								                AND [PricingProductGroupDiscount].Deleted = 0
						                WHERE [Pricing].Deleted = 0
						                AND [Pricing].Culture = @Culture
						                AND [Pricing].ForVat = 0
						                ORDER BY [Pricing].CalculatedExtraCharge DESC
					                );
				                END
		                END

	                IF(@OrderItemId IS NOT NULL)
		                SET @OneTimeDiscount = (
			                SELECT [OrderItem].[OneTimeDiscount] FROM [OrderItem]
			                WHERE [OrderItem].[ID] = @OrderItemId
		                );

	                -- Compute the result value
	                SET @Price = ISNULL(
		                ROUND(
			                ROUND(@ProductPrice + (@ProductPrice * (@ExtraCharge / 100)), 14)
			                -
			                ROUND(@ProductPrice + (@ProductPrice * (@ExtraCharge / 100)), 14) * (@DiscountRate / 100)
		                , 14)
		                -
		                (
			                ROUND(
				                ROUND(@ProductPrice + (@ProductPrice * @ExtraCharge / 100), 14)
				                -
				                ROUND(@ProductPrice + (@ProductPrice * @ExtraCharge / 100), 14) * (@DiscountRate / 100)
			                , 14)
			                *
			                @OneTimeDiscount / 100
		                )
	                , 0.00);

	                -- Return the result of the function
	                RETURN @Price

                END
                GO

                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[GetCrossExchangeRateToBaseCurrencyByCurrencyId]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[GetCrossExchangeRateToBaseCurrencyByCurrencyId]

                GO 

                CREATE FUNCTION [dbo].[GetCrossExchangeRateToBaseCurrencyByCurrencyId]
                (
	                @CurrencyId bigint
                )
                RETURNS money
                AS
                BEGIN
	                DECLARE @Result money = 0

	                SELECT @Result = (
						                SELECT TOP(1) 
							                (CASE 
								                WHEN COUNT([CrossExchangeRate].ID) > 0
									                THEN MAX([CrossExchangeRate].Amount)
								                ELSE 1
							                END) AS Amount
						                FROM [CrossExchangeRate] 
						                WHERE [CrossExchangeRate].Deleted = 0 
						                AND [CrossExchangeRate].CurrencyFromID = @CurrencyId 
						                AND [CrossExchangeRate].CurrencyToID = 2
					                 )

	                -- Return the result of the function
	                RETURN @Result

                END
                GO

                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[GetCurrentEuroExchangeRateByCulture]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[GetCurrentEuroExchangeRateByCulture]

                GO 

                CREATE FUNCTION [dbo].[GetCurrentEuroExchangeRateByCulture]
                (
	                -- Add the parameters for the function here
	                @Culture nvarchar(2)
                )
                RETURNS money
                AS
                BEGIN
	                DECLARE @Result money = 0;

	                IF (@Culture = 'pl')
		                BEGIN
			                SET @Result = (
				                SELECT TOP(1) 
					                CASE 
						                WHEN [ExchangeRateHistory].Amount IS NOT NULL
							                THEN [ExchangeRateHistory].Amount
						                ELSE ExchangeRate.Amount 
					                END
				                FROM ExchangeRate 
					                LEFT JOIN [ExchangeRateHistory]
						                ON [ExchangeRateHistory].ExchangeRateID = [ExchangeRate].ID
				                WHERE ExchangeRate.Deleted = 0 
				                AND ExchangeRate.Culture = @Culture 
				                AND ExchangeRate.Code = 'EUR'
				                AND ([ExchangeRateHistory].Created IS NULL OR [ExchangeRateHistory].Created <= CONVERT(date, DATEADD(day, -1, DATEADD(hour, 3, GETUTCDATE()))))
				                ORDER BY [ExchangeRateHistory].Created DESC
			                );
		                END
	                ELSE
		                BEGIN
			                SET @Result = (
				                SELECT TOP(1) ExchangeRate.Amount FROM ExchangeRate 
				                WHERE ExchangeRate.Deleted = 0 
				                AND ExchangeRate.Culture = @Culture 
				                AND ExchangeRate.Code = 'EUR'
			                );
		                END

	                -- Return the result of the function
	                RETURN @Result

                END
                GO

                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[GetCurrentEuroExchangeRateFiltered]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[GetCurrentEuroExchangeRateFiltered]

                GO 

                CREATE FUNCTION [dbo].[GetCurrentEuroExchangeRateFiltered]
                (
	                -- Add the parameters for the function here
	                @ProductNetId uniqueidentifier,
	                @CurrencyId bigint,
	                @ForVatProducts bit,
	                @IsFromReSale bit
                )
                RETURNS money
                AS
                BEGIN
	                DECLARE @Result money = 0;
	                DECLARE @ConsignmentTopRate money = 0;

	                SET @Result = ISNULL(
		                (SELECT ISNULL(
			                (
				                SELECT
				                [ExchangeRate].[Amount] 
				                FROM [ExchangeRate] 
				                WHERE [ExchangeRate].[Deleted] = 0 
				                AND [ExchangeRate].[CurrencyID] = @CurrencyId 
				                AND [ExchangeRate].[Code] = 'EUR'
				                AND [ExchangeRate].[Deleted] = 0
			                )
			                , 
			                (
				                SELECT
				                [CrossExchangeRate].[Amount]
				                FROM [CrossExchangeRate]
				                WHERE [CrossExchangeRate].[Deleted] = 0
				                AND [CrossExchangeRate].[CurrencyToID] = @CurrencyId
				                AND [CrossExchangeRate].[CurrencyFromID] = (
					                SELECT [Currency].[ID] FROM [Currency]
					                WHERE [Currency].[Deleted] = 0
					                AND [Currency].[Code] = 'EUR'
				                )
			                )
		                )),
		                1
	                )

	                SET @ConsignmentTopRate = ISNULL((
		                SELECT TOP 1 ISNULL (
			                (
				                SELECT ISNULL (
					                (
						                SELECT ISNULL (
							                (
								                SELECT TOP 1 [GovExchangeRateHistory].[Amount] FROM [GovExchangeRateHistory]
								                LEFT JOIN [GovExchangeRate]
								                ON [GovExchangeRate].[ID] = [GovExchangeRateHistory].[GovExchangeRateID]
								                WHERE [GovExchangeRate].[CurrencyID] = @CurrencyId
								                AND [GovExchangeRate].[Code] = 'EUR'
								                AND [GovExchangeRateHistory].[Created] <
									                CASE
										                WHEN [SupplyOrderUkraineItem].[ID] IS NULL
										                THEN 
											                CASE
												                WHEN [SupplyInvoice].[DateCustomDeclaration] IS NOT NULL
												                THEN [SupplyInvoice].[DateCustomDeclaration]
												                ELSE [SupplyInvoice].[Created]
											                END
										                ELSE [SupplyOrderUkraine].[InvDate]
									                END
								                ORDER BY [GovExchangeRateHistory].[Created] DESC
							                )
							                ,
							                (
								                SELECT [GovExchangeRate].[Amount] FROM [GovExchangeRate]
								                WHERE [GovExchangeRate].[Code] = 'EUR'
								                AND [GovExchangeRate].[CurrencyID] = @CurrencyId
							                )
						                )
					                )
					                ,
					                (
						                SELECT ISNULL (
							                (
								                SELECT TOP 1 [GovCrossExchangeRateHistory].[Amount] FROM [GovCrossExchangeRateHistory]
								                LEFT JOIN [GovCrossExchangeRate]
								                ON [GovCrossExchangeRate].[ID] = [GovCrossExchangeRateHistory].[GovCrossExchangeRateID]
								                WHERE [GovCrossExchangeRate].[CurrencyToID] = @CurrencyId
								                AND [GovCrossExchangeRate].[CurrencyFromID] = (
									                SELECT TOP 1 [Currency].[ID] FROM [Currency]
									                WHERE [Currency].[Code] = 'EUR'
									                AND [Currency].[Deleted] = 0
								                )
								                AND [GovCrossExchangeRateHistory].[Created] <
									                CASE
										                WHEN [SupplyOrderUkraineItem].[ID] IS NULL
										                THEN 
											                CASE
												                WHEN [SupplyInvoice].[DateCustomDeclaration] IS NOT NULL
												                THEN [SupplyInvoice].[DateCustomDeclaration]
												                ELSE [SupplyInvoice].[Created]
											                END
										                ELSE [SupplyOrderUkraine].[InvDate]
									                END
								                ORDER BY [GovCrossExchangeRateHistory].[Created] DESC
							                )
							                ,
							                (
								                SELECT [GovCrossExchangeRate].[Amount] FROM [GovCrossExchangeRate]
								                WHERE [GovCrossExchangeRate].[CurrencyFromID] = (
									                SELECT TOP 1 [Currency].[ID] FROM [Currency]
									                WHERE [Currency].[Code] = 'EUR'
									                AND [Currency].[Deleted] = 0
								                )
								                AND [GovCrossExchangeRate].[CurrencyToID] = @CurrencyId
							                )
						                )
					                )
				                )
			                )
			                ,
			                1
		                ) AS [ExchangeRateAmount]
		                FROM [ConsignmentItem]
			                LEFT JOIN [Product]
				                ON [Product].ID = [ConsignmentItem].ProductID
			                LEFT JOIN [Consignment]
				                ON [Consignment].ID = [ConsignmentItem].ConsignmentID
			                LEFT JOIN [Organization]
				                ON [Organization].ID = [Consignment].OrganizationID
			                LEFT JOIN [Storage]
				                ON [Storage].ID = [Consignment].StorageID
			                LEFT JOIN [ProductIncomeItem]
				                ON [ProductIncomeItem].[ID] = [ConsignmentItem].[ProductIncomeItemID]
			                LEFT JOIN [PackingListPackageOrderItem]
				                ON [PackingListPackageOrderItem].[ID] = [ProductIncomeItem].[PackingListPackageOrderItemID]
			                LEFT JOIN [PackingList]
				                ON [PackingList].[ID] = [PackingListPackageOrderItem].[PackingListID]
			                LEFT JOIN [SupplyInvoice]
				                ON [SupplyInvoice].[ID] = [PAckingList].[SupplyInvoiceID]
			                LEFT JOIN [SupplyOrderUkraineItem]
				                ON [SupplyOrderUkraineItem].[ID] = [ProductIncomeItem].[SupplyOrderUkraineItemID]
			                LEFT JOIN [SupplyOrderUkraine]
				                ON [SupplyOrderUkraine].[ID] = [SupplyOrderUkraineItem].[SupplyOrderUkraineID]
		                WHERE [ConsignmentItem].RemainingQty > 0
		                AND [ConsignmentItem].Deleted = 0
		                AND [Consignment].Deleted = 0
		                AND [Storage].ForDefective = 0
		                AND ([Storage].[AvailableForReSale] = @IsFromReSale
		                OR [Storage].[AvailableForReSale] = 
			                CASE	
				                WHEN @ForVatProducts = 0
				                THEN 0
				                ELSE 1
			                END
		                )
		                AND ([Storage].[ForVatProducts] = @ForVatProducts
		                OR [Storage].[ForVatProducts] =
			                CASE 
				                WHEN @IsFromReSale = 1
				                THEN 1
				                ELSE 0
			                END
		                )
		                AND [Product].NetUID = @ProductNetId
		                ORDER BY [ExchangeRateAmount] DESC
	                ), 0.00)

	                -- Return the result of the function
	                RETURN CASE WHEN @Result > @ConsignmentTopRate THEN @Result ELSE @ConsignmentTopRate END
                END
                GO

                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[GetDefaultCalculatedProductPriceWithSharesAndVat]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[GetDefaultCalculatedProductPriceWithSharesAndVat]

                GO 

                CREATE FUNCTION [dbo].[GetDefaultCalculatedProductPriceWithSharesAndVat]
                (
	                -- Add the parameters for the function here
	                @ProductNetId uniqueidentifier,
	                @ClientAgreementNetId uniqueidentifier,
	                @Culture nvarchar(2),
	                @WithVat bit,
	                @OrderItemId bigint
                )
                RETURNS money
                AS
                BEGIN
	                -- Declare the return variable here
	                DECLARE @Price money;

	                -- Declare temporary variables here
	                DECLARE @ProductGroupId bigint;
	                DECLARE @ProductPrice money;
	                DECLARE @ProductId bigint;
	                DECLARE @ExtraCharge money;
	                DECLARE @DiscountRate money;
	                DECLARE @PromotionalProduct bit;
	                DECLARE @PricingId bigint;
	                DECLARE @PromotionalPricingId bigint;
	                DECLARE @OneTimeDiscount money;

	                SET @OneTimeDiscount = 0;

	                IF (@ClientAgreementNetId <> '00000000-0000-0000-0000-000000000000')
		                SET @PromotionalPricingId = (
			                SELECT [Agreement].[PromotionalPricingID] 
			                FROM [Agreement]
				                LEFT JOIN [ClientAgreement]
					                ON [ClientAgreement].[AgreementID] = [Agreement].[ID]
			                WHERE [ClientAgreement].[NetUID] = @ClientAgreementNetId
		                );
	                ELSE
		                SET @PromotionalPricingId = (SELECT TOP(1) PromotionalPricingID FROM [EcommerceDefaultPricing]);

	                SET @ProductId = (
		                SELECT TOP(1) [Product].ID
		                FROM [Product]
		                WHERE [Product].NetUID = @ProductNetId
	                );

	                SET @PromotionalProduct = (
		                SELECT TOP(1) 
			                ISNULL(
				                (
					                CASE 
						                WHEN [Product].ID IS NULL 
							                THEN 0
						                WHEN [Product].IsForSale = 1
							                THEN 1
						                WHEN [Product].IsForZeroSale = 1
							                THEN 1
						                WHEN [Product].[Top] = N'X9'
							                THEN 1
						                WHEN [Product].[Top] = N'Х9'
							                THEN 1
						                ELSE 0
					                END
				                )
			                , 0)
		                FROM [Product]
		                WHERE [Product].NetUID = @ProductNetId
	                );

	                SET @ProductGroupId = (
		                SELECT TOP(1) [ProductProductGroup].ProductGroupID
		                FROM [ProductProductGroup]
		                WHERE [ProductProductGroup].Deleted = 0
		                AND [ProductProductGroup].ProductID = @ProductId
	                );

	                IF (@ClientAgreementNetId <> '00000000-0000-0000-0000-000000000000')
		                SET @PricingId = (
			                SELECT TOP(1) ISNULL([Agreement].PricingID, 0)
			                FROM [ClientAgreement]
				                LEFT JOIN [Agreement]
					                ON [Agreement].ID = [ClientAgreement].AgreementID
			                WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
		                )
	                ELSE
		                SET @PricingId = (SELECT TOP(1) [PricingId] FROM [EcommerceDefaultPricing])
	                
	                SET @PricingId = dbo.GetBasePricingId(@PricingId)

	                SET @ProductPrice = (
		                SELECT TOP(1) [ProductPricing].Price
		                FROM [ProductPricing]
		                WHERE [ProductPricing].Deleted = 0
		                AND [ProductPricing].ProductID = @ProductId
		                AND [ProductPricing].PricingID = @PricingId
	                );

	                SET @DiscountRate = 0.00;

	                IF(@PromotionalProduct = 1)
		                BEGIN
			                IF (@ClientAgreementNetId <> '00000000-0000-0000-0000-000000000000')
				                BEGIN
					                IF(@PromotionalPricingId IS NULL)
						                BEGIN
							                SET @DiscountRate = ISNULL(
								                (
									                SELECT TOP(1) [ProductGroupDiscount].DiscountRate
									                FROM [ProductGroupDiscount]
										                LEFT JOIN [ClientAgreement]
											                ON [ProductGroupDiscount].ClientAgreementID = [ClientAgreement].ID
									                WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
									                AND [ProductGroupDiscount].ProductGroupID = @ProductGroupId
									                AND [ProductGroupDiscount].IsActive = 1
								                )
							                , 0.00);

							                SET @ExtraCharge = ISNULL(
								                (
									                SELECT TOP(1) 
										                CASE 
											                WHEN [PricingProductGroupDiscount].CalculatedExtraCharge IS NOT NULL
												                THEN [PricingProductGroupDiscount].CalculatedExtraCharge
											                ELSE [Pricing].CalculatedExtraCharge
										                END
									                FROM [ClientAgreement]
										                LEFT JOIN [Agreement]
											                ON [ClientAgreement].AgreementID = [Agreement].ID
										                LEFT JOIN [Pricing]
											                ON [Agreement].PricingID = [Pricing].ID
										                LEFT JOIN [PricingProductGroupDiscount]
											                ON [PricingProductGroupDiscount].PricingID = [Pricing].ID
											                AND [PricingProductGroupDiscount].ProductGroupID = @ProductGroupId
											                AND [PricingProductGroupDiscount].Deleted = 0
									                WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
								                ),
								                (
									                SELECT TOP(1) 
										                CASE 
											                WHEN [PricingProductGroupDiscount].CalculatedExtraCharge IS NOT NULL
												                THEN [PricingProductGroupDiscount].CalculatedExtraCharge
											                ELSE [Pricing].CalculatedExtraCharge
										                END
									                FROM [Pricing]
										                LEFT JOIN [PricingProductGroupDiscount]
											                ON [PricingProductGroupDiscount].PricingID = [Pricing].ID
											                AND [PricingProductGroupDiscount].ProductGroupID = @ProductGroupId
											                AND [PricingProductGroupDiscount].Deleted = 0
									                WHERE [Pricing].Deleted = 0
									                AND [Pricing].Culture = @Culture
									                AND [Pricing].ForVat = 0
									                ORDER BY [Pricing].CalculatedExtraCharge DESC
								                )
							                )
						                END
					                ELSE 
						                BEGIN
							                SET @ExtraCharge = ISNULL((SELECT TOP(1) 
								                CASE 
										                WHEN [PricingProductGroupDiscount].CalculatedExtraCharge IS NOT NULL
											                THEN [PricingProductGroupDiscount].CalculatedExtraCharge
										                ELSE [Pricing].CalculatedExtraCharge
									                END
								                FROM [ClientAgreement]
									                LEFT JOIN [Agreement]
										                ON [ClientAgreement].AgreementID = [Agreement].ID
									                LEFT JOIN [Pricing]
										                ON [Agreement].[PromotionalPricingID] = [Pricing].ID
									                LEFT JOIN [PricingProductGroupDiscount]
										                ON [PricingProductGroupDiscount].PricingID = [Pricing].ID
										                AND [PricingProductGroupDiscount].ProductGroupID = @ProductGroupId
										                AND [PricingProductGroupDiscount].Deleted = 0
								                WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
							                ), 0.00);
					                
							                SET @PricingId = dbo.GetBasePricingId(@PromotionalPricingId);
						                END

					                SET @ProductPrice = (
						                SELECT TOP(1) [ProductPricing].Price
						                FROM [ProductPricing]
						                WHERE [ProductPricing].Deleted = 0
						                AND [ProductPricing].ProductID = @ProductId
						                AND [ProductPricing].PricingID = @PricingId
					                );
				                END
			                ELSE
				                BEGIN
					                SET @DiscountRate = 0.00;

					                SET @ExtraCharge = (
						                SELECT TOP(1) 
							                CASE 
								                WHEN [PricingProductGroupDiscount].CalculatedExtraCharge IS NOT NULL
									                THEN [PricingProductGroupDiscount].CalculatedExtraCharge
								                ELSE [Pricing].CalculatedExtraCharge
							                END
						                FROM [Pricing]
							                LEFT JOIN [PricingProductGroupDiscount]
								                ON [PricingProductGroupDiscount].PricingID = [Pricing].ID
								                AND [PricingProductGroupDiscount].ProductGroupID = @ProductGroupId
								                AND [PricingProductGroupDiscount].Deleted = 0
						                WHERE [Pricing].Deleted = 0
						                AND [Pricing].Culture = @Culture
						                AND [Pricing].ForVat = 0
						                ORDER BY [Pricing].CalculatedExtraCharge DESC
					                );
				                END
		                END
	                ELSE
		                BEGIN
			                IF (@ClientAgreementNetId <> '00000000-0000-0000-0000-000000000000')
				                BEGIN
					                SET @DiscountRate = ISNULL(
						                (
							                SELECT TOP(1) [ProductGroupDiscount].DiscountRate
							                FROM [ProductGroupDiscount]
								                LEFT JOIN [ClientAgreement]
									                ON [ProductGroupDiscount].ClientAgreementID = [ClientAgreement].ID
							                WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
							                AND [ProductGroupDiscount].ProductGroupID = @ProductGroupId
							                AND [ProductGroupDiscount].IsActive = 1
						                )
					                , 0.00);

					                SET @ExtraCharge = ISNULL(
						                (
							                SELECT TOP(1) 
								                CASE 
									                WHEN [PricingProductGroupDiscount].CalculatedExtraCharge IS NOT NULL
										                THEN [PricingProductGroupDiscount].CalculatedExtraCharge
									                ELSE [Pricing].CalculatedExtraCharge
								                END
							                FROM [ClientAgreement]
								                LEFT JOIN [Agreement]
									                ON [ClientAgreement].AgreementID = [Agreement].ID
								                LEFT JOIN [Pricing]
									                ON [Agreement].PricingID = [Pricing].ID
								                LEFT JOIN [PricingProductGroupDiscount]
									                ON [PricingProductGroupDiscount].PricingID = [Pricing].ID
									                AND [PricingProductGroupDiscount].ProductGroupID = @ProductGroupId
									                AND [PricingProductGroupDiscount].Deleted = 0
							                WHERE [ClientAgreement].NetUID = @ClientAgreementNetId
						                ),
						                (
							                SELECT TOP(1) 
								                CASE 
									                WHEN [PricingProductGroupDiscount].CalculatedExtraCharge IS NOT NULL
										                THEN [PricingProductGroupDiscount].CalculatedExtraCharge
									                ELSE [Pricing].CalculatedExtraCharge
								                END
							                FROM [Pricing]
								                LEFT JOIN [PricingProductGroupDiscount]
									                ON [PricingProductGroupDiscount].PricingID = [Pricing].ID
									                AND [PricingProductGroupDiscount].ProductGroupID = @ProductGroupId
									                AND [PricingProductGroupDiscount].Deleted = 0
							                WHERE [Pricing].Deleted = 0
							                AND [Pricing].Culture = @Culture
							                AND [Pricing].ForVat = 0
							                ORDER BY [Pricing].CalculatedExtraCharge DESC
						                )
					                )
				                END
			                ELSE
				                BEGIN
					                SET @DiscountRate = 0.00;

					                SET @ExtraCharge = (
						                SELECT TOP(1) 
							                CASE 
								                WHEN [PricingProductGroupDiscount].CalculatedExtraCharge IS NOT NULL
									                THEN [PricingProductGroupDiscount].CalculatedExtraCharge
								                ELSE [Pricing].CalculatedExtraCharge
							                END
						                FROM [Pricing]
							                LEFT JOIN [PricingProductGroupDiscount]
								                ON [PricingProductGroupDiscount].PricingID = [Pricing].ID
								                AND [PricingProductGroupDiscount].ProductGroupID = @ProductGroupId
								                AND [PricingProductGroupDiscount].Deleted = 0
						                WHERE [Pricing].Deleted = 0
						                AND [Pricing].Culture = @Culture
						                AND [Pricing].ForVat = 0
						                ORDER BY [Pricing].CalculatedExtraCharge DESC
					                );
				                END
		                END

	                IF(@OrderItemId IS NOT NULL)
		                SET @OneTimeDiscount = (
			                SELECT [OrderItem].[OneTimeDiscount] FROM [OrderItem]
			                WHERE [OrderItem].[ID] = @OrderItemId
		                );

	                -- Compute the result value
	                SET @Price = ISNULL(
		                ROUND(
			                ROUND(@ProductPrice + (@ProductPrice * (@ExtraCharge / 100)), 14)
			                -
			                ROUND(@ProductPrice + (@ProductPrice * (@ExtraCharge / 100)), 14) * (@DiscountRate / 100)
		                , 14)
		                -
		                (
			                ROUND(
				                ROUND(@ProductPrice + (@ProductPrice * @ExtraCharge / 100), 14)
				                -
				                ROUND(@ProductPrice + (@ProductPrice * @ExtraCharge / 100), 14) * (@DiscountRate / 100)
			                , 14)
			                *
			                @OneTimeDiscount / 100
		                )
	                , 0.00);

	                -- Return the result of the function
	                RETURN @Price

                END

                GO

                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[GetExchangedToEuroValue]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[GetExchangedToEuroValue]

                GO 

                CREATE FUNCTION [dbo].[GetExchangedToEuroValue] 
                (
	                -- Add the parameters for the function here
	                @Value money,
	                @FromCurrencyId bigint,
	                @FromDate datetime2
                )
                RETURNS money
                AS
                BEGIN
	                DECLARE @ExchangeRate money;
	                DECLARE @CrossExchangeRate money;
	                DECLARE @InverseCrossExchangeRate money; 
	                DECLARE @EuroCurrencyId bigint;
	                
 	                SELECT @EuroCurrencyId = (SELECT TOP(1) [Currency].ID FROM [Currency] WHERE [Currency].Deleted = 0 AND [Currency].Code = 'EUR');

	                SELECT @ExchangeRate = 
		                (
			                SELECT TOP(1) IIF([ExchangeRateHistory].Amount IS NOT NULL, [ExchangeRateHistory].Amount, [ExchangeRate].Amount)
			                FROM [ExchangeRate]
				                LEFT JOIN [ExchangeRateHistory]
					                ON [ExchangeRateHistory].ExchangeRateID = [ExchangeRate].ID
					                AND [ExchangeRateHistory].Created <= @FromDate
			                WHERE [ExchangeRate].CurrencyID = @FromCurrencyId
			                AND [ExchangeRate].Code = 'EUR'
			                AND [ExchangeRate].Deleted = 0
			                ORDER BY [ExchangeRateHistory].ID DESC
		                )

	                SELECT @CrossExchangeRate = 
		                ( 
			                SELECT TOP(1) IIF([CrossExchangeRateHistory].Amount IS NOT NULL, [CrossExchangeRateHistory].Amount, [CrossExchangeRate].Amount)
			                FROM [CrossExchangeRate] 
				                LEFT JOIN [CrossExchangeRateHistory]
					                ON [CrossExchangeRateHistory].CrossExchangeRateID = [CrossExchangeRate].ID
					                AND [CrossExchangeRateHistory].Created <= @FromDate
			                WHERE [CrossExchangeRate].CurrencyFromID = @FromCurrencyId 
			                AND [CrossExchangeRate].CurrencyToID = @EuroCurrencyId 
			                AND [CrossExchangeRate].Deleted = 0 
			                ORDER BY [CrossExchangeRateHistory].ID DESC
		                ); 

	                SELECT @InverseCrossExchangeRate = 
		                ( 
			                SELECT TOP(1) IIF([CrossExchangeRateHistory].Amount IS NOT NULL, [CrossExchangeRateHistory].Amount, [CrossExchangeRate].Amount)
			                FROM [CrossExchangeRate] 
				                LEFT JOIN [CrossExchangeRateHistory]
					                ON [CrossExchangeRateHistory].CrossExchangeRateID = [CrossExchangeRate].ID
					                AND [CrossExchangeRateHistory].Created <= @FromDate
			                WHERE [CrossExchangeRate].CurrencyFromID = @EuroCurrencyId
			                AND [CrossExchangeRate].CurrencyToID = @FromCurrencyId
			                AND [CrossExchangeRate].Deleted = 0 
			                ORDER BY [CrossExchangeRateHistory].ID DESC
		                ); 

	                RETURN (
		                CASE 
			                WHEN (@FromCurrencyId = @EuroCurrencyId)
				                THEN @Value
			                WHEN (@ExchangeRate IS NOT NULL)
				                THEN ROUND(@Value / @ExchangeRate, 2)
			                WHEN (@CrossExchangeRate IS NOT NULL)
				                THEN ROUND(@Value * @CrossExchangeRate, 2)
			                WHEN (@InverseCrossExchangeRate IS NOT NULL)
				                THEN ROUND(@Value / @InverseCrossExchangeRate, 2)
			                ELSE @Value
		                END
	                )

                END
                GO

                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[GetExchangeRateByCurrencyIdAndCode]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[GetExchangeRateByCurrencyIdAndCode]

                GO 

                CREATE FUNCTION [dbo].[GetExchangeRateByCurrencyIdAndCode]
                (
	                -- Add the parameters for the function here
	                @Id bigint,
	                @Code nvarchar(3),
	                @FromDate datetime2
                )
                RETURNS money
                AS
                BEGIN
	                -- Declare the return variable here
	                DECLARE @ExchangeValue money;

	                IF(@Id IS NULL)
		                BEGIN
			                SET @Id = (
				                SELECT ID FROM Currency
				                WHERE Code = 'UAH'
				                AND Deleted = 0
			                );
		                END

	                SET @ExchangeValue = (SELECT TOP(1) 
	                (CASE 
	                WHEN [ExchangeRateHistory].Amount IS NOT NULL 
	                THEN [ExchangeRateHistory].Amount 
	                ELSE [ExchangeRate].Amount 
	                END) AS [Amount] 
	                FROM [ExchangeRate] 
	                LEFT JOIN [ExchangeRateHistory] 
	                ON [ExchangeRateHistory].ExchangeRateID = [ExchangeRate].ID 
	                AND [ExchangeRateHistory].Created <= @FromDate 
	                WHERE [ExchangeRate].CurrencyID = @Id 
	                AND [ExchangeRate].Code = @Code 
	                ORDER BY [ExchangeRateHistory].Created DESC)


	                -- Return the result of the function
	                RETURN @ExchangeValue

                END
                GO

                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[GetGovExchangedToEuroValue]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[GetGovExchangedToEuroValue]

                GO 

                CREATE FUNCTION [dbo].[GetGovExchangedToEuroValue]
                (
	                @Value money,
	                @FromCurrencyId bigint,
	                @FromDate datetime2
                )
                RETURNS money
                AS
                BEGIN
	                DECLARE @GovExchangeRate money;
	                DECLARE @GovCrossExchangeRate money;
	                DECLARE @GovInverseCrossExchangeRate money; 
	                DECLARE @GovEuroCurrencyId bigint;
	                
 	                SELECT @GovEuroCurrencyId = (SELECT TOP(1) [Currency].ID FROM [Currency] WHERE [Currency].Deleted = 0 AND [Currency].Code = 'EUR');

	                SELECT @GovExchangeRate = 
		                (
			                SELECT TOP(1) IIF([GovExchangeRateHistory].Amount IS NOT NULL, [GovExchangeRateHistory].Amount, [GovExchangeRate].Amount)
			                FROM [GovExchangeRate]
				                LEFT JOIN [GovExchangeRateHistory]
					                ON [GovExchangeRateHistory].GovExchangeRateID = [GovExchangeRate].ID
					                AND [GovExchangeRateHistory].Created <= @FromDate
			                WHERE [GovExchangeRate].CurrencyID = @FromCurrencyId
			                AND [GovExchangeRate].Code = 'EUR'
			                AND [GovExchangeRate].Deleted = 0
			                ORDER BY [GovExchangeRateHistory].ID DESC
		                )

	                SELECT @GovCrossExchangeRate = 
		                ( 
			                SELECT TOP(1) IIF([GovCrossExchangeRateHistory].Amount IS NOT NULL, [GovCrossExchangeRateHistory].Amount, [GovCrossExchangeRate].Amount)
			                FROM [GovCrossExchangeRate] 
				                LEFT JOIN [GovCrossExchangeRateHistory]
					                ON [GovCrossExchangeRateHistory].GovCrossExchangeRateID = [GovCrossExchangeRate].ID
					                AND [GovCrossExchangeRateHistory].Created <= @FromDate
			                WHERE [GovCrossExchangeRate].CurrencyFromID = @FromCurrencyId 
			                AND [GovCrossExchangeRate].CurrencyToID = @GovEuroCurrencyId 
			                AND [GovCrossExchangeRate].Deleted = 0 
			                ORDER BY [GovCrossExchangeRateHistory].ID DESC
		                ); 

	                SELECT @GovInverseCrossExchangeRate = 
		                ( 
			                SELECT TOP(1) IIF([GovCrossExchangeRateHistory].Amount IS NOT NULL, [GovCrossExchangeRateHistory].Amount, [GovCrossExchangeRate].Amount)
			                FROM [GovCrossExchangeRate] 
				                LEFT JOIN [GovCrossExchangeRateHistory]
					                ON [GovCrossExchangeRateHistory].GovCrossExchangeRateID = [GovCrossExchangeRate].ID
					                AND [GovCrossExchangeRateHistory].Created <= @FromDate
			                WHERE [GovCrossExchangeRate].CurrencyFromID = @GovEuroCurrencyId
			                AND [GovCrossExchangeRate].CurrencyToID = @FromCurrencyId
			                AND [GovCrossExchangeRate].Deleted = 0 
			                ORDER BY [GovCrossExchangeRateHistory].ID DESC
		                ); 

	                RETURN (
		                CASE 
			                WHEN (@FromCurrencyId = @GovEuroCurrencyId)
				                THEN @Value
			                WHEN (@GovExchangeRate IS NOT NULL)
				                THEN ROUND(@Value / @GovExchangeRate, 2)
			                WHEN (@GovCrossExchangeRate IS NOT NULL)
				                THEN ROUND(@Value * @GovCrossExchangeRate, 2)
			                WHEN (@GovInverseCrossExchangeRate IS NOT NULL)
				                THEN ROUND(@Value / @GovInverseCrossExchangeRate, 2)
			                ELSE @Value
		                END
	                )

                END
                GO

                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[GetGovExchangedToUahValue]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[GetGovExchangedToUahValue]

                GO 

                CREATE FUNCTION [dbo].[GetGovExchangedToUahValue] 
                (
	                -- Add the parameters for the function here
	                @Value money,
	                @FromCurrencyId bigint,
	                @FromDate datetime2
                )
                RETURNS money
                AS
                BEGIN
	                DECLARE @GovExchangeRate money;
	                DECLARE @GovCrossExchangeRate money;
	                DECLARE @InverseGovCrossExchangeRate money; 
	                DECLARE @UahCurrencyId bigint;
	                DECLARE @FromCurrencyCode nvarchar(3);
	                
 	                SELECT @UahCurrencyId = (SELECT TOP(1) [Currency].ID FROM [Currency] WHERE [Currency].Deleted = 0 AND [Currency].Code = 'UAH');

	                SELECT @FromCurrencyCode = (SELECT TOP(1) [Currency].[Code] FROM [Currency] WHERE [Currency].Deleted = 0 AND [Currency].[ID] = @FromCurrencyId)

	                SELECT @GovExchangeRate = 
		                (
			                SELECT TOP(1) IIF([GovExchangeRateHistory].Amount IS NOT NULL, [GovExchangeRateHistory].Amount, [GovExchangeRate].Amount)
			                FROM [GovExchangeRate]
				                LEFT JOIN [GovExchangeRateHistory]
					                ON [GovExchangeRateHistory].GovExchangeRateID = [GovExchangeRate].ID
					                AND [GovExchangeRateHistory].Created <= @FromDate
			                WHERE [GovExchangeRate].CurrencyID = @UahCurrencyId
			                AND [GovExchangeRate].Code = @FromCurrencyCode
			                AND [GovExchangeRate].Deleted = 0
			                ORDER BY [GovExchangeRateHistory].ID DESC
		                )

	                SELECT @GovCrossExchangeRate = 
		                ( 
			                SELECT TOP(1) IIF([GovCrossExchangeRateHistory].Amount IS NOT NULL, [GovCrossExchangeRateHistory].Amount, [GovCrossExchangeRate].Amount)
			                FROM [GovCrossExchangeRate] 
				                LEFT JOIN [GovCrossExchangeRateHistory]
					                ON [GovCrossExchangeRateHistory].GovCrossExchangeRateID = [GovCrossExchangeRate].ID
					                AND [GovCrossExchangeRateHistory].Created <= @FromDate
			                WHERE [GovCrossExchangeRate].CurrencyFromID = @UahCurrencyId 
			                AND [GovCrossExchangeRate].CurrencyToID = @FromCurrencyId  
			                AND [GovCrossExchangeRate].Deleted = 0 
			                ORDER BY [GovCrossExchangeRateHistory].ID DESC
		                ); 

	                SELECT @InverseGovCrossExchangeRate = 
		                ( 
			                SELECT TOP(1) IIF([GovCrossExchangeRateHistory].Amount IS NOT NULL, [GovCrossExchangeRateHistory].Amount, [GovCrossExchangeRate].Amount)
			                FROM [GovCrossExchangeRate] 
				                LEFT JOIN [GovCrossExchangeRateHistory]
					                ON [GovCrossExchangeRateHistory].GovCrossExchangeRateID = [GovCrossExchangeRate].ID
					                AND [GovCrossExchangeRateHistory].Created <= @FromDate
			                WHERE [GovCrossExchangeRate].CurrencyFromID = @UahCurrencyId
			                AND [GovCrossExchangeRate].CurrencyToID = @FromCurrencyId
			                AND [GovCrossExchangeRate].Deleted = 0 
			                ORDER BY [GovCrossExchangeRateHistory].ID DESC
		                ); 

	                RETURN (
		                CASE 
			                WHEN (@FromCurrencyId = @UahCurrencyId)
				                THEN @Value
			                WHEN (@GovExchangeRate IS NOT NULL)
				                THEN ROUND(@Value * @GovExchangeRate, 2)
			                WHEN (@GovCrossExchangeRate IS NOT NULL)
				                THEN ROUND(@Value / @GovCrossExchangeRate, 2)
			                WHEN (@InverseGovCrossExchangeRate IS NOT NULL)
				                THEN ROUND(@Value * @InverseGovCrossExchangeRate, 2)
			                ELSE @Value
		                END
	                )

                END
                GO

                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[GetPricingExtraCharge]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[GetPricingExtraCharge]

                GO 

                CREATE FUNCTION [dbo].[GetPricingExtraCharge]
                (
	                -- Add the parameters for the function here
	                @PricingNetId uniqueidentifier
                )
                RETURNS float
                AS
                BEGIN
	                -- Declare the return variable here
	                DECLARE @Result float = 0;

	                WITH Pricing_CTE 
	                AS
		                (
			                SELECT * FROM Pricing SelectedPricing
			                WHERE SelectedPricing.NetUID = @PricingNetId

			                UNION ALL

			                SELECT BasePricing.*
			                FROM Pricing BasePricing
			                JOIN Pricing_CTE 
			                ON Pricing_CTE.BasePricingID = BasePricing.ID
		                )
	                SELECT @Result = ISNULL(SUM(Pricing_CTE.ExtraCharge), 0) FROM Pricing_CTE

	                -- Return the result of the function
	                RETURN @Result

                END
                GO
            ");
    }

    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql(@"
                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[GetTopRootConsignmentItemByConsignmentItemId]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[GetTopRootConsignmentItemByConsignmentItemId]

                GO 

                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[fn_LevenshteinDistance]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[fn_LevenshteinDistance]

                GO 

                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[GetCalculatedProductLocalPriceWithShares_ReSale]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[GetCalculatedProductLocalPriceWithShares_ReSale]

                GO 

                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[GetCalculatedProductLocalPriceWithSharesAndVat]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[GetCalculatedProductLocalPriceWithSharesAndVat]

                GO 

                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[GetCalculatedProductPriceWithShares_ReSale]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[GetCalculatedProductPriceWithShares_ReSale]

                GO 

                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[GetCalculatedProductPriceWithSharesAndVat]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[GetCalculatedProductPriceWithSharesAndVat]

                GO 

                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[GetCrossExchangeRateToBaseCurrencyByCurrencyId]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[GetCrossExchangeRateToBaseCurrencyByCurrencyId]

                GO 

                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[GetCurrentEuroExchangeRateByCulture]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[GetCurrentEuroExchangeRateByCulture]

                GO 

                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[GetCurrentEuroExchangeRateFiltered]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[GetCurrentEuroExchangeRateFiltered]

                GO 

                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[GetDefaultCalculatedProductPriceWithSharesAndVat]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[GetDefaultCalculatedProductPriceWithSharesAndVat]

                GO 

                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[GetExchangedToEuroValue]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[GetExchangedToEuroValue]

                GO 

                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[GetExchangeRateByCurrencyIdAndCode]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[GetExchangeRateByCurrencyIdAndCode]

                GO 

                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[GetGovExchangedToEuroValue]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[GetGovExchangedToEuroValue]

                GO 

                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[GetGovExchangedToUahValue]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[GetGovExchangedToUahValue]

                GO 

                IF EXISTS (SELECT *
                           FROM   sys.objects
                           WHERE  object_id = OBJECT_ID(N'[dbo].[GetPricingExtraCharge]')
                                  AND type IN ( N'FN', N'IF', N'TF', N'FS', N'FT' ))
                  DROP FUNCTION [dbo].[GetPricingExtraCharge]

                GO 
            ");
    }
}