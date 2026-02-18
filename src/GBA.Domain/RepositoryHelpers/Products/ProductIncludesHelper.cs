using System;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.RepositoryHelpers.Products;

public sealed class ProductIncludesHelper {
    private readonly IDbConnection _connection;

    public ProductIncludesHelper(IDbConnection connection) {
        _connection = connection;
    }

    public void IncludeAnaloguesForProduct(Product productToReturn, Guid? clientAgreementNetId = null, bool notIncludeWithZeroAvailability = false) {
        bool withVat = false;

        decimal currentExchangeRateEurToUah = _connection.Query<decimal>(
            "SELECT TOP 1 [ExchangeRate].[Amount] FROM [ExchangeRate] " +
            "WHERE [ExchangeRate].[Deleted] = 0 " +
            "AND [ExchangeRate].[Code] = 'EUR' " +
            "AND [ExchangeRate].[CurrencyID] = ( " +
            "SELECT TOP 1 [Currency].[ID] FROM [Currency] " +
            "WHERE [Currency].[Code] = 'UAH' " +
            "AND [Currency].[Deleted] = 0 " +
            ") ").FirstOrDefault();

        if (clientAgreementNetId.HasValue)
            withVat = _connection.Query<bool>(
                "SELECT [Agreement].WithVATAccounting FROM [ClientAgreement] " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "WHERE [ClientAgreement].NetUID = @ClientAgreementNetId "
                , new { ClientAgreementNetId = clientAgreementNetId }
            ).FirstOrDefault();

        var joinProps = new {
            productToReturn.Id,
            ClientAgreementNetId = clientAgreementNetId ?? Guid.Empty,
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
            WithVat = withVat
        };

        string analoguesExpression =
            "SELECT [Product].ID " +
            ",[ProductAnalogue].* " +
            ",[Analogue].ID " +
            ",[Analogue].Created " +
            ",[Analogue].Deleted " +
            ",[Analogue].HasAnalogue " +
            ",[Analogue].HasComponent " +
            ",[Analogue].HasImage " +
            ",[Analogue].Image " +
            ",[Analogue].[SynonymsUA] " +
            ",[Analogue].[SynonymsPL] " +
            ",[Analogue].IsForSale " +
            ",[Analogue].IsForWeb " +
            ",[Analogue].IsForZeroSale " +
            ",[Analogue].MainOriginalNumber " +
            ",[Analogue].MeasureUnitID " +
            ",[Analogue].NetUID ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            analoguesExpression += ",[Analogue].[NamePL] AS [Name] ";
            analoguesExpression += ",[Analogue].[DescriptionPL] AS [Description] ";
            analoguesExpression += ", [Analogue].[NotesPL] AS [Notes] ";
        } else {
            analoguesExpression += ",[Analogue].[NameUA] AS [Name] ";
            analoguesExpression += ",[Analogue].[DescriptionUA] AS [Description] ";
            analoguesExpression += ", [Analogue].[NotesUA] AS [Notes] ";
        }

        analoguesExpression +=
            ",[Analogue].OrderStandard " +
            ",[Analogue].PackingStandard " +
            ",[Analogue].Standard " +
            ",[Analogue].Size " +
            ",[Analogue].[Top] " +
            ",[Analogue].UCGFEA " +
            ",[Analogue].Updated " +
            ",[Analogue].VendorCode " +
            ",[Analogue].Volume " +
            ",[Analogue].Weight ";

        if (clientAgreementNetId.HasValue)
            analoguesExpression +=
                ",dbo.GetCalculatedProductPriceWithSharesAndVat(Analogue.NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentPrice " +
                ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Analogue.NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentLocalPrice " +
                ",dbo.GetCalculatedProductPriceWithShares_ReSale(Analogue.NetUID, @ClientAgreementNetId, @Culture, NULL) AS CurrentPriceReSale " +
                ",dbo.GetCalculatedProductLocalPriceWithShares_ReSale(Analogue.NetUID, @ClientAgreementNetId, @Culture, NULL) AS CurrentLocalPriceReSale ";

        analoguesExpression +=
            ",[MeasureUnit].* " +
            ",[ProductOriginalNumber].* " +
            ",[OriginalNumber].* " +
            ",[ProductProductGroup].* " +
            ",[ProductGroup].* " +
            ",[ProductCategory].* " +
            ",[Category].* " +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[ProductPricing].* " +
            ",[Pricing].* " +
            "FROM [Product] " +
            "LEFT JOIN [ProductAnalogue] " +
            "ON [ProductAnalogue].BaseProductID = [Product].ID " +
            "AND [ProductAnalogue].Deleted = 0 " +
            "LEFT JOIN [Product] AS [Analogue] " +
            "ON [Analogue].ID = [ProductAnalogue].AnalogueProductID " +
            "LEFT JOIN [MeasureUnit] " +
            "ON [Analogue].MeasureUnitID = [MeasureUnit].ID " +
            "LEFT JOIN [ProductOriginalNumber] " +
            "ON [ProductOriginalNumber].ProductID = [Analogue].ID " +
            "AND [ProductOriginalNumber].Deleted = 0 " +
            "LEFT JOIN [OriginalNumber] " +
            "ON [ProductOriginalNumber].OriginalNumberID = [OriginalNumber].ID " +
            "LEFT JOIN [ProductProductGroup] " +
            "ON [ProductProductGroup].ProductID = [Analogue].ID " +
            "AND [ProductProductGroup].Deleted = 0 " +
            "LEFT JOIN [ProductGroup] " +
            "ON [ProductGroup].ID = [ProductProductGroup].ProductGroupID " +
            "LEFT JOIN [ProductCategory] " +
            "ON [ProductCategory].ProductID = [Analogue].ID " +
            "AND [ProductCategory].Deleted = 0 " +
            "LEFT JOIN [Category] " +
            "ON [Category].ID = [ProductCategory].CategoryID " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Analogue].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "LEFT JOIN [ProductPricing] " +
            "ON [ProductPricing].ProductID = [Analogue].ID " +
            "AND [ProductPricing].Deleted = 0 " +
            "LEFT JOIN [views].[PricingView] AS [Pricing] " +
            "ON [Pricing].ID = [ProductPricing].PricingID " +
            "AND [Pricing].CultureCode = @Culture " +
            "WHERE [Product].ID = @Id " +
            "AND [Analogue].ID IS NOT NULL ";

        if (notIncludeWithZeroAvailability)
            analoguesExpression +=
                "AND [ProductAvailability].ID IS NOT NULL " +
                "AND [ProductAvailability].Amount <> 0 ";

        analoguesExpression +=
            "ORDER BY " +
            "CASE WHEN ([ProductAvailability].Amount <> 0 AND [Storage].Locale = @Culture) THEN 0 ELSE 1 END, " +
            "[Analogue].VendorCode, [Analogue].Name, " +
            "CASE WHEN [Storage].Locale = @Culture THEN 0 ELSE 1 END";

        Type[] analoguesTypes = {
            typeof(Product),
            typeof(ProductAnalogue),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ProductOriginalNumber),
            typeof(OriginalNumber),
            typeof(ProductProductGroup),
            typeof(ProductGroup),
            typeof(ProductCategory),
            typeof(Category),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(ProductPricing),
            typeof(Pricing)
        };

        Func<object[], Product> analoguesMapper = objects => {
            Product product = (Product)objects[0];
            ProductAnalogue productAnalogue = (ProductAnalogue)objects[1];
            Product analogue = (Product)objects[2];
            MeasureUnit measureUnit = (MeasureUnit)objects[3];
            ProductOriginalNumber productOriginalNumber = (ProductOriginalNumber)objects[4];
            OriginalNumber originalNumber = (OriginalNumber)objects[5];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[6];
            ProductGroup productGroup = (ProductGroup)objects[7];
            ProductCategory productCategory = (ProductCategory)objects[8];
            Category category = (Category)objects[9];
            ProductAvailability productAvailability = (ProductAvailability)objects[10];
            Storage storage = (Storage)objects[11];
            ProductPricing productPricing = (ProductPricing)objects[12];
            Pricing pricing = (Pricing)objects[13];

            if (analogue == null) return product;

            if (!productToReturn.AnalogueProducts.Any(a => a.Id.Equals(productAnalogue.Id))) {
                if (productOriginalNumber != null) {
                    productOriginalNumber.OriginalNumber = originalNumber;

                    analogue.ProductOriginalNumbers.Add(productOriginalNumber);
                }

                if (productProductGroup != null) {
                    productProductGroup.ProductGroup = productGroup;

                    analogue.ProductProductGroups.Add(productProductGroup);
                }

                if (productCategory != null) {
                    productCategory.Category = category;

                    analogue.ProductCategories.Add(productCategory);
                }

                if (productAvailability != null) {
                    productAvailability.Storage = storage;

                    if (!storage.ForDefective) {
                        if (storage.Locale.ToLower().Equals("pl")) {
                            if (storage.ForVatProducts)
                                analogue.AvailableQtyPlVAT += productAvailability.Amount;
                            else
                                analogue.AvailableQtyPl += productAvailability.Amount;
                        } else {
                            if (storage.ForVatProducts) {
                                analogue.AvailableQtyUkVAT += productAvailability.Amount;

                                if (storage.AvailableForReSale)
                                    analogue.AvailableQtyUkReSale += productAvailability.Amount;
                            } else {
                                analogue.AvailableQtyUk += productAvailability.Amount;
                            }
                        }
                    } else {
                        if (storage.Locale.ToLower().Equals("pl"))
                            analogue.AvailableDefectiveQtyPl += productAvailability.Amount;
                        else
                            analogue.AvailableDefectiveQtyUk += productAvailability.Amount;
                    }

                    analogue.ProductAvailabilities.Add(productAvailability);
                }

                if (productPricing != null) {
                    productPricing.Pricing = pricing;

                    analogue.ProductPricings.Add(productPricing);
                }

                analogue.MeasureUnit = measureUnit;

                analogue.CurrentLocalPrice = decimal.Round(analogue.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);
                analogue.CurrentPriceEurToUah = decimal.Round(analogue.CurrentPrice * currentExchangeRateEurToUah, 2, MidpointRounding.AwayFromZero);

                productAnalogue.AnalogueProduct = analogue;

                productToReturn.AnalogueProducts.Add(productAnalogue);
            } else {
                ProductAnalogue analogueFromList = productToReturn.AnalogueProducts.First(a => a.Id.Equals(productAnalogue.Id));

                if (productOriginalNumber != null && !analogueFromList.AnalogueProduct.ProductOriginalNumbers.Any(n => n.Id.Equals(productOriginalNumber.Id))) {
                    productOriginalNumber.OriginalNumber = originalNumber;

                    analogueFromList.AnalogueProduct.ProductOriginalNumbers.Add(productOriginalNumber);
                }

                if (productProductGroup != null && !analogueFromList.AnalogueProduct.ProductProductGroups.Any(g => g.Id.Equals(productProductGroup.Id))) {
                    productProductGroup.ProductGroup = productGroup;

                    analogueFromList.AnalogueProduct.ProductProductGroups.Add(productProductGroup);
                }

                if (productCategory != null && !analogueFromList.AnalogueProduct.ProductCategories.Any(c => c.Id.Equals(productCategory.Id))) {
                    productCategory.Category = category;

                    analogueFromList.AnalogueProduct.ProductCategories.Add(productCategory);
                }

                if (productAvailability != null &&
                    !analogueFromList.AnalogueProduct.ProductAvailabilities.Any(a => a.Id.Equals(productAvailability.Id))) {
                    productAvailability.Storage = storage;

                    if (!storage.ForDefective) {
                        if (storage.Locale.ToLower().Equals("pl")) {
                            if (storage.ForVatProducts)
                                analogueFromList.AnalogueProduct.AvailableQtyPlVAT += productAvailability.Amount;
                            else
                                analogueFromList.AnalogueProduct.AvailableQtyPl += productAvailability.Amount;
                        } else {
                            if (storage.ForVatProducts) {
                                analogueFromList.AnalogueProduct.AvailableQtyUkVAT += productAvailability.Amount;

                                if (storage.AvailableForReSale)
                                    analogueFromList.AnalogueProduct.AvailableQtyUkReSale += productAvailability.Amount;
                            } else {
                                analogueFromList.AnalogueProduct.AvailableQtyUk += productAvailability.Amount;
                            }
                        }
                    } else {
                        if (storage.Locale.ToLower().Equals("pl"))
                            analogueFromList.AnalogueProduct.AvailableDefectiveQtyPl += productAvailability.Amount;
                        else
                            analogueFromList.AnalogueProduct.AvailableDefectiveQtyUk += productAvailability.Amount;
                    }

                    analogueFromList.AnalogueProduct.ProductAvailabilities.Add(productAvailability);
                }

                if (productPricing != null && !analogueFromList.AnalogueProduct.ProductPricings.Any(p => p.Id.Equals(productPricing.Id))) {
                    productPricing.Pricing = pricing;

                    analogueFromList.AnalogueProduct.ProductPricings.Add(productPricing);
                }
            }

            return product;
        };

        _connection.Query(
            analoguesExpression,
            analoguesTypes,
            analoguesMapper,
            joinProps
        );
    }

    public void IncludeComponentsForProduct(
        Product productToReturn,
        Guid? clientAgreementNetId = null,
        bool withAnalogues = false,
        bool notIncludeWithZeroAvailability = false) {
        bool withVat = false;

        // decimal currentExchangeRateEurToUah = _connection.Query<decimal>(
        //     "SELECT TOP 1 [ExchangeRate].[Amount] FROM [ExchangeRate] " +
        //     "WHERE [ExchangeRate].[Deleted] = 0 " +
        //     "AND [ExchangeRate].[Code] = 'EUR' " +
        //     "AND [ExchangeRate].[CurrencyID] = ( " +
        //     "SELECT TOP 1 [Currency].[ID] FROM [Currency] " +
        //     "WHERE [Currency].[Code] = 'UAH' " +
        //     "AND [Currency].[Deleted] = 0 " +
        //     ") ").FirstOrDefault();

        long uahCurrencyId = _connection.Query<long>(
            "SELECT ID FROM Currency " +
            "WHERE Currency.Code = 'UAH'").FirstOrDefault();

        if (clientAgreementNetId.HasValue)
            withVat = _connection.Query<bool>(
                "SELECT [Agreement].WithVATAccounting FROM [ClientAgreement] " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "WHERE [ClientAgreement].NetUID = @ClientAgreementNetId "
                , new { ClientAgreementNetId = clientAgreementNetId }
            ).FirstOrDefault();

        var joinProps = new {
            productToReturn.Id,
            ClientAgreementNetId = clientAgreementNetId ?? Guid.Empty,
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
            WithVat = withVat
        };

        string componentsExpression =
            "SELECT [Product].* " +
            ",[ProductSet].* " +
            ",[Component].ID " +
            ",[Component].Created " +
            ",[Component].Deleted " +
            ",[Component].HasAnalogue " +
            ",[Component].HasComponent " +
            ",[Component].HasImage " +
            ",[Component].Image " +
            ",[Component].IsForSale " +
            ",[Component].IsForWeb " +
            ",[Component].IsForZeroSale " +
            ",[Component].MainOriginalNumber " +
            ",[Component].MeasureUnitID " +
            ",[Component].NetUID ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            componentsExpression += ", [Component].[NamePL] AS [Name] ";
            componentsExpression += ", [Component].[DescriptionPL] AS [Description] ";
            componentsExpression += ", [Component].[NotesPL] AS [Notes] ";
        } else {
            componentsExpression += ", [Component].[NameUA] AS [Name] ";
            componentsExpression += ", [Component].[DescriptionUA] AS [Description] ";
            componentsExpression += ", [Component].[NotesUA] AS [Notes] ";
        }

        componentsExpression +=
            ",[Component].OrderStandard " +
            ",[Component].PackingStandard " +
            ",[Component].Standard " +
            ",[Component].Size " +
            ",[Component].[Top] " +
            ",[Component].UCGFEA " +
            ",[Component].Updated " +
            ",[Component].VendorCode " +
            ",[Component].Volume " +
            ",[Component].Weight ";

        if (clientAgreementNetId.HasValue)
            componentsExpression +=
                ",dbo.GetCalculatedProductPriceWithSharesAndVat([Component].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentPrice " +
                ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Component].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentLocalPrice " +
                ",dbo.GetCalculatedProductPriceWithShares_ReSale([Component].NetUID, @ClientAgreementNetId, @Culture, NULL) AS CurrentPriceReSale " +
                ",dbo.GetCalculatedProductLocalPriceWithShares_ReSale([Component].NetUID, @ClientAgreementNetId, @Culture, NULL) AS CurrentLocalPriceReSale ";

        componentsExpression +=
            ",[MeasureUnit].* " +
            ",[ProductOriginalNumber].* " +
            ",[OriginalNumber].* " +
            ",[ProductProductGroup].* " +
            ",[ProductGroup].* " +
            ",[ProductCategory].* " +
            ",[Category].* " +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[ProductPricing].* " +
            ",[Pricing].* " +
            "FROM [Product] " +
            "LEFT JOIN [ProductSet] " +
            "ON [ProductSet].BaseProductID = [Product].ID " +
            "AND [ProductSet].Deleted = 0 " +
            "LEFT JOIN [Product] AS [Component] " +
            "ON [Component].ID = [ProductSet].ComponentProductID " +
            "LEFT JOIN [MeasureUnit] " +
            "ON [Component].MeasureUnitID = [MeasureUnit].ID " +
            "LEFT JOIN [ProductOriginalNumber] " +
            "ON [ProductOriginalNumber].ProductID = [Component].ID " +
            "AND [ProductOriginalNumber].Deleted = 0 " +
            "LEFT JOIN [OriginalNumber] " +
            "ON [ProductOriginalNumber].OriginalNumberID = [OriginalNumber].ID " +
            "LEFT JOIN [ProductProductGroup] " +
            "ON [ProductProductGroup].ProductID = [Component].ID " +
            "AND [ProductProductGroup].Deleted = 0 " +
            "LEFT JOIN [ProductGroup] " +
            "ON [ProductGroup].ID = [ProductProductGroup].ProductGroupID " +
            "LEFT JOIN [ProductCategory] " +
            "ON [ProductCategory].ProductID = [Component].ID " +
            "AND [ProductCategory].Deleted = 0 " +
            "LEFT JOIN [Category] " +
            "ON [Category].ID = [ProductCategory].CategoryID " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Component].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "LEFT JOIN [ProductPricing] " +
            "ON [ProductPricing].ProductID = [Component].ID " +
            "AND [ProductPricing].Deleted = 0 " +
            "LEFT JOIN [views].[PricingView] AS [Pricing] " +
            "ON [Pricing].ID = [ProductPricing].PricingID " +
            "AND [Pricing].CultureCode = @Culture " +
            "WHERE [Product].ID = @Id " +
            "AND [Component].ID IS NOT NULL ";

        if (notIncludeWithZeroAvailability)
            componentsExpression +=
                "AND [ProductAvailability].ID IS NOT NULL " +
                "AND [ProductAvailability].Amount <> 0 ";

        componentsExpression +=
            "ORDER BY " +
            "CASE WHEN ([ProductAvailability].Amount <> 0 AND [Storage].Locale = @Culture) THEN 0 ELSE 1 END, " +
            "[Component].VendorCode, [Component].Name, " +
            "CASE WHEN [Storage].Locale = @Culture THEN 0 ELSE 1 END";

        Type[] componentsTypes = {
            typeof(Product),
            typeof(ProductSet),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ProductOriginalNumber),
            typeof(OriginalNumber),
            typeof(ProductProductGroup),
            typeof(ProductGroup),
            typeof(ProductCategory),
            typeof(Category),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(ProductPricing),
            typeof(Pricing)
        };

        Func<object[], Product> componentsMapper = objects => {
            Product product = (Product)objects[0];
            ProductSet productSet = (ProductSet)objects[1];
            Product component = (Product)objects[2];
            MeasureUnit measureUnit = (MeasureUnit)objects[3];
            ProductOriginalNumber productOriginalNumber = (ProductOriginalNumber)objects[4];
            OriginalNumber originalNumber = (OriginalNumber)objects[5];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[6];
            ProductGroup productGroup = (ProductGroup)objects[7];
            ProductCategory productCategory = (ProductCategory)objects[8];
            Category category = (Category)objects[9];
            ProductAvailability productAvailability = (ProductAvailability)objects[10];
            Storage storage = (Storage)objects[11];
            ProductPricing productPricing = (ProductPricing)objects[12];
            Pricing pricing = (Pricing)objects[13];

            if (component == null) return product;

            if (!productToReturn.ComponentProducts.Any(a => a.Id.Equals(productSet.Id))) {
                if (productOriginalNumber != null) {
                    productOriginalNumber.OriginalNumber = originalNumber;

                    component.ProductOriginalNumbers.Add(productOriginalNumber);
                }

                if (productProductGroup != null) {
                    productProductGroup.ProductGroup = productGroup;

                    component.ProductProductGroups.Add(productProductGroup);
                }

                if (productCategory != null) {
                    productCategory.Category = category;

                    component.ProductCategories.Add(productCategory);
                }

                if (productAvailability != null) {
                    productAvailability.Storage = storage;

                    if (!storage.ForDefective) {
                        if (storage.Locale.ToLower().Equals("pl")) {
                            if (storage.ForVatProducts)
                                component.AvailableQtyPlVAT += productAvailability.Amount;
                            else
                                component.AvailableQtyPl += productAvailability.Amount;
                        } else {
                            if (storage.ForVatProducts) {
                                component.AvailableQtyUkVAT += productAvailability.Amount;

                                if (storage.AvailableForReSale)
                                    component.AvailableQtyUkReSale += productAvailability.Amount;
                            } else {
                                component.AvailableQtyUk += productAvailability.Amount;
                            }
                        }
                    } else {
                        if (storage.Locale.ToLower().Equals("pl"))
                            component.AvailableDefectiveQtyPl += productAvailability.Amount;
                        else
                            component.AvailableDefectiveQtyUk += productAvailability.Amount;
                    }

                    component.ProductAvailabilities.Add(productAvailability);
                }

                if (productPricing != null) {
                    productPricing.Pricing = pricing;

                    component.ProductPricings.Add(productPricing);
                }

                component.MeasureUnit = measureUnit;

                component.CurrentLocalPrice = decimal.Round(component.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);
                // component.CurrentPriceEurToUah = decimal.Round(component.CurrentPrice * currentExchangeRateEurToUah, 2, MidpointRounding.AwayFromZero);

                productSet.ComponentProduct = component;

                productToReturn.ComponentProducts.Add(productSet);
            } else {
                ProductSet setFromList = productToReturn.ComponentProducts.First(a => a.Id.Equals(productSet.Id));

                if (productOriginalNumber != null && !setFromList.ComponentProduct.ProductOriginalNumbers.Any(n => n.Id.Equals(productOriginalNumber.Id))) {
                    productOriginalNumber.OriginalNumber = originalNumber;

                    setFromList.ComponentProduct.ProductOriginalNumbers.Add(productOriginalNumber);
                }

                if (productProductGroup != null && !setFromList.ComponentProduct.ProductProductGroups.Any(g => g.Id.Equals(productProductGroup.Id))) {
                    productProductGroup.ProductGroup = productGroup;

                    setFromList.ComponentProduct.ProductProductGroups.Add(productProductGroup);
                }

                if (productCategory != null && !setFromList.ComponentProduct.ProductCategories.Any(c => c.Id.Equals(productCategory.Id))) {
                    productCategory.Category = category;

                    setFromList.ComponentProduct.ProductCategories.Add(productCategory);
                }

                if (productAvailability != null && !setFromList.ComponentProduct.ProductAvailabilities.Any(a => a.Id.Equals(productAvailability.Id))) {
                    productAvailability.Storage = storage;

                    if (!storage.ForDefective) {
                        if (storage.Locale.ToLower().Equals("pl")) {
                            if (storage.ForVatProducts)
                                setFromList.ComponentProduct.AvailableQtyPlVAT += productAvailability.Amount;
                            else
                                setFromList.ComponentProduct.AvailableQtyPl += productAvailability.Amount;
                        } else {
                            if (storage.ForVatProducts) {
                                setFromList.ComponentProduct.AvailableQtyUkVAT += productAvailability.Amount;

                                if (storage.AvailableForReSale)
                                    setFromList.ComponentProduct.AvailableQtyUkReSale += productAvailability.Amount;
                            } else {
                                setFromList.ComponentProduct.AvailableQtyUk += productAvailability.Amount;
                            }
                        }
                    } else {
                        if (storage.Locale.ToLower().Equals("pl"))
                            setFromList.ComponentProduct.AvailableDefectiveQtyPl += productAvailability.Amount;
                        else
                            setFromList.ComponentProduct.AvailableDefectiveQtyUk += productAvailability.Amount;
                    }

                    setFromList.ComponentProduct.ProductAvailabilities.Add(productAvailability);
                }

                if (productPricing != null && !setFromList.ComponentProduct.ProductPricings.Any(p => p.Id.Equals(productPricing.Id))) {
                    productPricing.Pricing = pricing;

                    setFromList.ComponentProduct.ProductPricings.Add(productPricing);
                }
            }

            return product;
        };

        _connection.Query(
            componentsExpression,
            componentsTypes,
            componentsMapper,
            joinProps
        );

        foreach (Product product in productToReturn.ComponentProducts.Select(c => c.ComponentProduct)) {
            decimal currentExchangeRateEurToUah = _connection.Query<decimal>(
                "SELECT dbo.GetCurrentEuroExchangeRateFiltered(@ProductNetId, @CurrencyId, @WithVat, NULL)",
                new { ProductNetId = product.NetUid, CurrencyID = uahCurrencyId, WithVat = withVat }).FirstOrDefault();

            product.CurrentPriceEurToUah = decimal.Round(product.CurrentPrice * currentExchangeRateEurToUah, 2, MidpointRounding.AwayFromZero);
            product.CurrentPriceReSaleEurToUah = decimal.Round(product.CurrentPriceReSale * currentExchangeRateEurToUah, 2, MidpointRounding.AwayFromZero);
        }

        if (!withAnalogues) return;

        string componentAnaloguesExpression =
            "SELECT [Product].ID " +
            ",[ProductSet].ID " +
            ",[ProductAnalogue].* " +
            ",[Analogue].ID " +
            ",[Analogue].Created " +
            ",[Analogue].Deleted " +
            ",[Analogue].HasAnalogue " +
            ",[Analogue].HasComponent " +
            ",[Analogue].HasImage " +
            ",[Analogue].Image " +
            ",[Analogue].IsForSale " +
            ",[Analogue].IsForWeb " +
            ",[Analogue].IsForZeroSale " +
            ",[Analogue].MainOriginalNumber " +
            ",[Analogue].MeasureUnitID " +
            ",[Analogue].NetUID ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            componentAnaloguesExpression += ",[Analogue].[NamePL] AS [Name] ";
            componentAnaloguesExpression += ",[Analogue].[DescriptionPL] AS [Description] ";
        } else {
            componentAnaloguesExpression += ",[Analogue].[NameUA] AS [Name] ";
            componentAnaloguesExpression += ",[Analogue].[DescriptionUA] AS [Description] ";
        }

        componentAnaloguesExpression +=
            ",[Analogue].OrderStandard " +
            ",[Analogue].PackingStandard " +
            ",[Analogue].Standard " +
            ",[Analogue].Size " +
            ",[Analogue].[Top] " +
            ",[Analogue].UCGFEA " +
            ",[Analogue].Updated " +
            ",[Analogue].VendorCode " +
            ",[Analogue].Volume " +
            ",[Analogue].Weight " +
            ",[MeasureUnit].* " +
            ",[ProductOriginalNumber].* " +
            ",[OriginalNumber].* " +
            ",[ProductProductGroup].* " +
            ",[ProductGroup].* " +
            ",[ProductCategory].* " +
            ",[Category].* " +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[ProductPricing].* " +
            ",[Pricing].* " +
            "FROM [Product] " +
            "LEFT JOIN [ProductSet] " +
            "ON [ProductSet].BaseProductID = [Product].ID " +
            "AND [ProductSet].Deleted = 0 " +
            "LEFT JOIN [ProductAnalogue] " +
            "ON [ProductAnalogue].BaseProductID = [ProductSet].ComponentProductID " +
            "AND [ProductAnalogue].Deleted = 0 " +
            "LEFT JOIN [Product] AS [Analogue] " +
            "ON [Analogue].ID = [ProductAnalogue].AnalogueProductID " +
            "LEFT JOIN [MeasureUnit] " +
            "ON [Analogue].MeasureUnitID = [MeasureUnit].ID " +
            "LEFT JOIN [ProductOriginalNumber] " +
            "ON [ProductOriginalNumber].ProductID = [Analogue].ID " +
            "AND [ProductOriginalNumber].Deleted = 0 " +
            "LEFT JOIN [OriginalNumber] " +
            "ON [ProductOriginalNumber].OriginalNumberID = [OriginalNumber].ID " +
            "LEFT JOIN [ProductProductGroup] " +
            "ON [ProductProductGroup].ProductID = [Analogue].ID " +
            "AND [ProductProductGroup].Deleted = 0 " +
            "LEFT JOIN [ProductGroup] " +
            "ON [ProductGroup].ID = [ProductProductGroup].ProductGroupID " +
            "LEFT JOIN [ProductCategory] " +
            "ON [ProductCategory].ProductID = [Analogue].ID " +
            "AND [ProductCategory].Deleted = 0 " +
            "LEFT JOIN [Category] " +
            "ON [Category].ID = [ProductCategory].CategoryID " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Analogue].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "LEFT JOIN [ProductPricing] " +
            "ON [ProductPricing].ProductID = [Analogue].ID " +
            "AND [ProductPricing].Deleted = 0 " +
            "LEFT JOIN [views].[PricingView] AS [Pricing] " +
            "ON [Pricing].ID = [ProductPricing].PricingID " +
            "AND [Pricing].CultureCode = @Culture " +
            "WHERE [Product].ID = @Id " +
            "AND [Analogue].ID IS NOT NULL " +
            "AND [Storage].ForDefective = 0 ";

        if (notIncludeWithZeroAvailability)
            componentAnaloguesExpression +=
                "AND [ProductAvailability].ID IS NOT NULL " +
                "AND [ProductAvailability].Amount <> 0 ";

        componentAnaloguesExpression += "ORDER BY CASE WHEN [Storage].Locale = @Culture THEN 0 ELSE 1 END";

        Type[] componentAnaloguesTypes = {
            typeof(Product),
            typeof(ProductSet),
            typeof(ProductAnalogue),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ProductOriginalNumber),
            typeof(OriginalNumber),
            typeof(ProductProductGroup),
            typeof(ProductGroup),
            typeof(ProductCategory),
            typeof(Category),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(ProductPricing),
            typeof(Pricing)
        };

        Func<object[], Product> componentAnaloguesMapper = objects => {
            Product product = (Product)objects[0];
            ProductSet productSet = (ProductSet)objects[1];
            ProductAnalogue productAnalogue = (ProductAnalogue)objects[2];
            Product analogue = (Product)objects[3];
            MeasureUnit measureUnit = (MeasureUnit)objects[4];
            ProductOriginalNumber productOriginalNumber = (ProductOriginalNumber)objects[5];
            OriginalNumber originalNumber = (OriginalNumber)objects[6];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[7];
            ProductGroup productGroup = (ProductGroup)objects[8];
            ProductCategory productCategory = (ProductCategory)objects[9];
            Category category = (Category)objects[10];
            ProductAvailability productAvailability = (ProductAvailability)objects[11];
            Storage storage = (Storage)objects[12];
            ProductPricing productPricing = (ProductPricing)objects[13];
            Pricing pricing = (Pricing)objects[14];

            if (productSet == null) return product;

            ProductSet component = productToReturn.ComponentProducts.First(c => c.Id.Equals(productSet.Id));

            if (!component.ComponentProduct.AnalogueProducts.Any(a => a.Id.Equals(productAnalogue.Id))) {
                if (productOriginalNumber != null) {
                    productOriginalNumber.OriginalNumber = originalNumber;

                    analogue.ProductOriginalNumbers.Add(productOriginalNumber);
                }

                if (productProductGroup != null) {
                    productProductGroup.ProductGroup = productGroup;

                    analogue.ProductProductGroups.Add(productProductGroup);
                }

                if (productCategory != null) {
                    productCategory.Category = category;

                    analogue.ProductCategories.Add(productCategory);
                }

                if (productAvailability != null) {
                    productAvailability.Storage = storage;

                    if (!storage.ForDefective) {
                        if (storage.Locale.ToLower().Equals("pl")) {
                            if (storage.ForVatProducts)
                                analogue.AvailableQtyPlVAT += productAvailability.Amount;
                            else
                                analogue.AvailableQtyPl += productAvailability.Amount;
                        } else {
                            if (storage.ForVatProducts) {
                                analogue.AvailableQtyUkVAT += productAvailability.Amount;

                                if (storage.AvailableForReSale)
                                    analogue.AvailableQtyUkReSale += productAvailability.Amount;
                            } else {
                                analogue.AvailableQtyUk += productAvailability.Amount;
                            }
                        }
                    }

                    analogue.ProductAvailabilities.Add(productAvailability);
                }

                if (productPricing != null) {
                    productPricing.Pricing = pricing;

                    analogue.ProductPricings.Add(productPricing);
                }

                analogue.MeasureUnit = measureUnit;

                analogue.CurrentLocalPrice = decimal.Round(analogue.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                productAnalogue.AnalogueProduct = analogue;

                component.ComponentProduct.AnalogueProducts.Add(productAnalogue);
            } else {
                ProductAnalogue analogueFromList = component.ComponentProduct.AnalogueProducts.First(a => a.Id.Equals(productAnalogue.Id));

                if (productOriginalNumber != null && !analogueFromList.AnalogueProduct.ProductOriginalNumbers.Any(n => n.Id.Equals(productOriginalNumber.Id))) {
                    productOriginalNumber.OriginalNumber = originalNumber;

                    analogueFromList.AnalogueProduct.ProductOriginalNumbers.Add(productOriginalNumber);
                }

                if (productProductGroup != null && !analogueFromList.AnalogueProduct.ProductProductGroups.Any(g => g.Id.Equals(productProductGroup.Id))) {
                    productProductGroup.ProductGroup = productGroup;

                    analogueFromList.AnalogueProduct.ProductProductGroups.Add(productProductGroup);
                }

                if (productCategory != null && !analogueFromList.AnalogueProduct.ProductCategories.Any(c => c.Id.Equals(productCategory.Id))) {
                    productCategory.Category = category;

                    analogueFromList.AnalogueProduct.ProductCategories.Add(productCategory);
                }

                if (productAvailability != null && !analogueFromList.AnalogueProduct.ProductAvailabilities.Any(a => a.Id.Equals(productAvailability.Id))) {
                    productAvailability.Storage = storage;

                    if (!storage.ForDefective) {
                        if (storage.Locale.ToLower().Equals("pl")) {
                            if (storage.ForVatProducts)
                                analogueFromList.AnalogueProduct.AvailableQtyPlVAT += productAvailability.Amount;
                            else
                                analogueFromList.AnalogueProduct.AvailableQtyPl += productAvailability.Amount;
                        } else {
                            if (storage.ForVatProducts) {
                                analogueFromList.AnalogueProduct.AvailableQtyUkVAT += productAvailability.Amount;

                                if (storage.AvailableForReSale)
                                    analogueFromList.AnalogueProduct.AvailableQtyUkReSale += productAvailability.Amount;
                            } else {
                                analogueFromList.AnalogueProduct.AvailableQtyUk += productAvailability.Amount;
                            }
                        }
                    }

                    analogueFromList.AnalogueProduct.ProductAvailabilities.Add(productAvailability);
                }

                if (productPricing != null && !analogueFromList.AnalogueProduct.ProductPricings.Any(p => p.Id.Equals(productPricing.Id))) {
                    productPricing.Pricing = pricing;

                    analogueFromList.AnalogueProduct.ProductPricings.Add(productPricing);
                }
            }

            return product;
        };

        _connection.Query(
            componentAnaloguesExpression,
            componentAnaloguesTypes,
            componentAnaloguesMapper,
            joinProps
        );
    }

    public void IncludeProductSetForProduct(Product toReturn, Guid? clientAgreementNetId = null, bool notIncludeWithZeroAvailability = false) {
        bool withVat = false;

        // decimal currentExchangeRateEurToUah = _connection.Query<decimal>(
        //     "SELECT TOP 1 [ExchangeRate].[Amount] FROM [ExchangeRate] " +
        //     "WHERE [ExchangeRate].[Deleted] = 0 " +
        //     "AND [ExchangeRate].[Code] = 'EUR' " +
        //     "AND [ExchangeRate].[CurrencyID] = ( " +
        //     "SELECT TOP 1 [Currency].[ID] FROM [Currency] " +
        //     "WHERE [Currency].[Code] = 'UAH' " +
        //     "AND [Currency].[Deleted] = 0 " +
        //     ") ").FirstOrDefault();

        long uahCurrencyId = _connection.Query<long>(
            "SELECT ID FROM Currency " +
            "WHERE Currency.Code = 'UAH'").FirstOrDefault();

        if (clientAgreementNetId.HasValue)
            withVat = _connection.Query<bool>(
                "SELECT [Agreement].WithVATAccounting FROM [ClientAgreement] " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "WHERE [ClientAgreement].NetUID = @ClientAgreementNetId "
                , new { ClientAgreementNetId = clientAgreementNetId }
            ).FirstOrDefault();

        var joinProps = new {
            toReturn.Id,
            ClientAgreementNetId = clientAgreementNetId ?? Guid.Empty,
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
            WithVat = withVat
        };

        Type[] productSetTypes = {
            typeof(Product),
            typeof(ProductSet),
            typeof(MeasureUnit),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(Organization)
        };

        Func<object[], Product> productSetMapper = objects => {
            Product product = (Product)objects[0];
            ProductSet productSet = (ProductSet)objects[1];
            MeasureUnit measureUnit = (MeasureUnit)objects[2];
            ProductAvailability productAvailability = (ProductAvailability)objects[3];
            Storage storage = (Storage)objects[4];
            Organization organization = (Organization)objects[5];

            if (productSet == null) return product;

            if (!toReturn.BaseSetProducts.Any(e => e.BaseProduct.Id.Equals(product.Id))) {
                if (productAvailability != null) {
                    productAvailability.Storage = storage;

                    if (!storage.ForDefective) {
                        if (storage.Locale.ToLower().Equals("pl")) {
                            if (storage.ForVatProducts)
                                product.AvailableQtyPlVAT += productAvailability.Amount;
                            else
                                product.AvailableQtyPl += productAvailability.Amount;
                        } else {
                            if (storage.ForVatProducts) {
                                product.AvailableQtyUkVAT += productAvailability.Amount;

                                if (storage.AvailableForReSale)
                                    product.AvailableQtyUkReSale += productAvailability.Amount;
                            } else if (organization != null && organization.StorageId.Equals(storage.Id)) {
                                product.AvailableQtyUk += productAvailability.Amount;
                            } else {
                                product.AvailableQtyUk += productAvailability.Amount;
                            }
                        }
                    }

                    product.ProductAvailabilities.Add(productAvailability);
                }

                product.MeasureUnit = measureUnit;

                product.CurrentLocalPrice = decimal.Round(product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);
                // product.CurrentPriceEurToUah = decimal.Round(product.CurrentPrice * currentExchangeRateEurToUah, 2, MidpointRounding.AwayFromZero);

                productSet.BaseProduct = product;
                productSet.SetComponentsQty = 1;

                toReturn.BaseSetProducts.Add(productSet);
            } else {
                ProductSet setFromList = toReturn.BaseSetProducts.First(e => e.BaseProduct.Id.Equals(product.Id));

                if (productAvailability != null && !setFromList.BaseProduct.ProductAvailabilities.Any(a => a.Id.Equals(productAvailability.Id))) {
                    productAvailability.Storage = storage;

                    if (!storage.ForDefective) {
                        if (storage.Locale.ToLower().Equals("pl")) {
                            if (storage.ForVatProducts)
                                setFromList.BaseProduct.AvailableQtyPlVAT += productAvailability.Amount;
                            else
                                setFromList.BaseProduct.AvailableQtyPl += productAvailability.Amount;
                        } else {
                            if (storage.ForVatProducts) {
                                setFromList.BaseProduct.AvailableQtyUkVAT += productAvailability.Amount;

                                if (storage.AvailableForReSale)
                                    setFromList.BaseProduct.AvailableQtyUkReSale += productAvailability.Amount;
                            } else if (organization != null && organization.StorageId.Equals(storage.Id)) {
                                setFromList.BaseProduct.AvailableQtyUk += productAvailability.Amount;
                            } else {
                                setFromList.BaseProduct.AvailableQtyUk += productAvailability.Amount;
                            }
                        }
                    }

                    setFromList.BaseProduct.ProductAvailabilities.Add(productAvailability);
                }
            }

            return product;
        };

        string productSetExpression =
            "SELECT * FROM [Product] AS [BaseProduct] ";

        if (clientAgreementNetId.HasValue)
            productSetExpression +=
                ",dbo.GetCalculatedProductPriceWithSharesAndVat([BaseProduct].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentPrice " +
                ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([BaseProduct].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentLocalPrice " +
                ",dbo.GetCalculatedProductPriceWithShares_ReSale([BaseProduct].NetUID, @ClientAgreementNetId, @Culture, NULL) AS CurrentPriceReSale " +
                ",dbo.GetCalculatedProductLocalPriceWithShares_ReSale([BaseProduct].NetUID, @ClientAgreementNetId, @Culture, NULL) AS CurrentLocalPriceReSale ";

        productSetExpression += "LEFT JOIN [ProductSet] " +
                                "ON [ProductSet].BaseProductID = [BaseProduct].ID " +
                                "AND [ProductSet].Deleted = 0 " +
                                "LEFT JOIN [MeasureUnit] " +
                                "ON [MeasureUnit].ID = [BaseProduct].MeasureUnitID " +
                                "LEFT JOIN [ProductAvailability] " +
                                "ON [ProductAvailability].ProductID = BaseProduct.ID " +
                                "AND [ProductAvailability].Deleted = 0 " +
                                "LEFT JOIN [Storage] " +
                                "ON [Storage].ID = [ProductAvailability].StorageID " +
                                "LEFT JOIN [Organization] " +
                                "ON [Organization].StorageID = [Storage].ID " +
                                "WHERE ProductSet.ComponentProductID = @Id " +
                                "AND [BaseProduct].Deleted = 0 ";

        _connection.Query(
            productSetExpression,
            productSetTypes,
            productSetMapper,
            joinProps);

        foreach (Product product in toReturn.BaseSetProducts.Select(c => c.BaseProduct)) {
            decimal currentExchangeRateEurToUah = _connection.Query<decimal>(
                "SELECT dbo.GetCurrentEuroExchangeRateFiltered(@ProductNetId, @CurrencyId, @WithVat, NULL)",
                new { ProductNetId = product.NetUid, CurrencyID = uahCurrencyId, WithVat = withVat }).FirstOrDefault();

            product.CurrentPriceEurToUah = decimal.Round(product.CurrentPrice * currentExchangeRateEurToUah, 2, MidpointRounding.AwayFromZero);
            product.CurrentPriceReSaleEurToUah = decimal.Round(product.CurrentPriceReSale * currentExchangeRateEurToUah, 2, MidpointRounding.AwayFromZero);
        }
    }
}