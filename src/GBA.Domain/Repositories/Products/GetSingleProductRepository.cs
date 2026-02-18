using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Products.Incomes;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers.ProductAvailabilityModels;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.RepositoryHelpers.Products;

namespace GBA.Domain.Repositories.Products;

public sealed class GetSingleProductRepository : IGetSingleProductRepository {
    private readonly IDbConnection _connection;

    public GetSingleProductRepository(IDbConnection connection) {
        _connection = connection;
    }

    public Product GetByIdWithCalculatedAvailability(
        long productId,
        long organizationId,
        bool withVat,
        Guid clientAgreementNetId) {
        Product toReturn = null;

        string currentCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

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

        string sqlExpression =
            "SELECT " +
            "[Product].ID " +
            ", [Product].Created " +
            ", [Product].Deleted ";

        if (currentCulture.Equals("pl")) {
            sqlExpression += ", [Product].[NameUA] AS [Name] ";
            sqlExpression += ", [Product].[DescriptionUA] AS [Description] ";
            sqlExpression += ", [Product].[NotesUA] AS [Notes] ";
        } else {
            sqlExpression += ", [Product].[NameUA] AS [Name] ";
            sqlExpression += ", [Product].[DescriptionUA] AS [Description] ";
            sqlExpression += ", [Product].[NotesUA] AS [Notes] ";
        }

        sqlExpression +=
            ", [Product].HasAnalogue " +
            ", [Product].HasComponent " +
            ", [Product].HasImage " +
            ", [Product].[Image] " +
            ", [Product].IsForSale " +
            ", [Product].IsForWeb " +
            ", [Product].IsForZeroSale " +
            ", [Product].MainOriginalNumber " +
            ", [Product].MeasureUnitID " +
            ", [Product].NetUID " +
            ", [Product].OrderStandard " +
            ", [Product].PackingStandard " +
            ", [Product].Standard " +
            ", [Product].Size " +
            ", [Product].[Top] " +
            ", [Product].UCGFEA " +
            ", [Product].Updated " +
            ", [Product].VendorCode " +
            ", [Product].Volume " +
            ", [Product].[Weight] " +
            ",dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentLocalPrice " +
            ",dbo.GetCalculatedProductPriceWithShares_ReSale(Product.NetUID, @ClientAgreementNetId, @Culture, NULL) AS CurrentPriceReSale " +
            ",dbo.GetCalculatedProductLocalPriceWithShares_ReSale(Product.NetUID, @ClientAgreementNetId, @Culture, NULL) AS CurrentLocalPriceReSale " +
            ",[ProductPricing].ID " +
            ",[ProductPricing].Price " +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[MeasureUnit].* " +
            ",[ProductImage].* " +
            ",[Organization].* " +
            "FROM [Product] " +
            "LEFT JOIN [ProductPricing] " +
            "ON [ProductPricing].ProductID = [Product].ID " +
            "AND [ProductPricing].Deleted = 0 " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [ProductImage] " +
            "ON [ProductImage].ProductID = [Product].ID " +
            "AND [ProductImage].Deleted = 0 " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].StorageID = [Storage].ID " +
            "WHERE [Product].ID = @ProductId ";

        Type[] types = {
            typeof(Product),
            typeof(ProductPricing),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(MeasureUnit),
            typeof(ProductImage),
            typeof(Organization)
        };

        Func<object[], Product> productsMapper = objects => {
            Product product = (Product)objects[0];
            ProductPricing productPricing = (ProductPricing)objects[1];
            ProductAvailability productAvailability = (ProductAvailability)objects[2];
            Storage storage = (Storage)objects[3];
            MeasureUnit measureUnit = (MeasureUnit)objects[4];
            ProductImage productImage = (ProductImage)objects[5];
            Organization organization = (Organization)objects[6];

            if (toReturn == null) {
                product.MeasureUnit = measureUnit;

                product.CurrentLocalPrice = decimal.Round(product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                toReturn = product;
            }

            if (productPricing != null && !toReturn.ProductPricings.Any(p => p.Id.Equals(productPricing.Id))) {
                productPricing.Price = toReturn.CurrentPrice;

                toReturn.ProductPricings.Add(productPricing);
            }

            // For searched product vat qty for non vat agreement should be in AvailableQtyUkReSale
            if (productAvailability != null && !toReturn.ProductAvailabilities.Any(p => p.Id.Equals(productAvailability.Id))) {
                productAvailability.Storage = storage;

                if (!storage.ForDefective) {
                    if (storage.OrganizationId.Equals(organizationId)) {
                        if (storage.Locale.ToLower().Equals("pl")) {
                            if (storage.ForVatProducts)
                                toReturn.AvailableQtyPlVAT += productAvailability.Amount;
                            else
                                toReturn.AvailableQtyPl += productAvailability.Amount;
                        } else {
                            if (storage.ForVatProducts) {
                                toReturn.AvailableQtyUkVAT += productAvailability.Amount;

                                if (storage.AvailableForReSale)
                                    toReturn.AvailableQtyUkReSale += productAvailability.Amount;
                            } else {
                                toReturn.AvailableQtyUk += productAvailability.Amount;
                            }
                        }
                    } else if (storage.AvailableForReSale && !storage.Locale.ToLower().Equals("pl")) {
                        toReturn.AvailableQtyUkReSale += productAvailability.Amount;
                        toReturn.AvailableQtyUkVAT += productAvailability.Amount;
                    } else if (organization != null && organization.StorageId.Equals(storage.Id)) {
                        toReturn.AvailableQtyUk += productAvailability.Amount;
                    }
                }

                toReturn.ProductAvailabilities.Add(productAvailability);
            }

            if (productImage == null || toReturn.ProductImages.Any(i => i.Id.Equals(productImage.Id))) return product;

            toReturn.ProductImages.Add(productImage);

            toReturn.HasImage = true;
            toReturn.Image = productImage.ImageUrl;

            return product;
        };

        _connection.Query(
            sqlExpression,
            types,
            productsMapper,
            new {
                ProductId = productId,
                OrganizationId = organizationId,
                ClientAgreementNetId = clientAgreementNetId,
                Culture = currentCulture,
                WithVat = withVat
            }
        );

        decimal currentProductExchangeRateEurToUah = _connection.Query<decimal>(
            "SELECT dbo.GetCurrentEuroExchangeRateFiltered(@ProductNetId, @CurrencyId, @WithVat, NULL)",
            new { ProductNetId = toReturn.NetUid, CurrencyID = uahCurrencyId, WithVat = withVat }).FirstOrDefault();

        toReturn.CurrentPriceEurToUah = decimal.Round(toReturn.CurrentPrice * currentProductExchangeRateEurToUah, 2, MidpointRounding.AwayFromZero);
        toReturn.CurrentPriceReSaleEurToUah = decimal.Round(toReturn.CurrentPriceReSale * currentProductExchangeRateEurToUah, 2, MidpointRounding.AwayFromZero);

        if (toReturn == null) return toReturn;

        if (toReturn.HasAnalogue) {
            types = new[] {
                typeof(Product),
                typeof(ProductAnalogue),
                typeof(Product),
                typeof(ProductPricing),
                typeof(ProductAvailability),
                typeof(Storage),
                typeof(MeasureUnit),
                typeof(Organization)
            };

            Func<object[], Product> analogueMapper = objects => {
                Product product = (Product)objects[0];
                ProductAnalogue productAnalogue = (ProductAnalogue)objects[1];
                Product analogue = (Product)objects[2];
                ProductPricing productPricing = (ProductPricing)objects[3];
                ProductAvailability productAvailability = (ProductAvailability)objects[4];
                Storage storage = (Storage)objects[5];
                MeasureUnit measureUnit = (MeasureUnit)objects[6];
                Organization organization = (Organization)objects[7];

                if (!toReturn.AnalogueProducts.Any(a => a.Id.Equals(productAnalogue.Id))) {
                    if (productPricing != null) {
                        productPricing.Price = analogue.CurrentPrice;

                        analogue.ProductPricings.Add(productPricing);
                    }

                    if (productAvailability != null) {
                        productAvailability.Storage = storage;

                        if (!storage.ForDefective) {
                            if (storage.OrganizationId.Equals(organizationId))
                                if (storage.Locale.ToLower().Equals("pl")) {
                                    if (storage.ForVatProducts)
                                        analogue.AvailableQtyPlVAT += productAvailability.Amount;
                                    else
                                        analogue.AvailableQtyPl += productAvailability.Amount;
                                } else {
                                    if (storage.ForVatProducts) {
                                        analogue.AvailableQtyUkVAT += productAvailability.Amount;

                                        if (storage.AvailableForReSale)
                                            analogue.AvailableQtyUk += productAvailability.Amount;
                                    } else {
                                        analogue.AvailableQtyUk += productAvailability.Amount;
                                    }
                                }
                            else if (storage.AvailableForReSale && !storage.Locale.ToLower().Equals("pl"))
                                analogue.AvailableQtyUk += productAvailability.Amount;
                            else if (organization != null && organization.StorageId.Equals(storage.Id))
                                analogue.AvailableQtyUk += productAvailability.Amount;
                        }

                        analogue.ProductAvailabilities.Add(productAvailability);
                    }

                    analogue.MeasureUnit = measureUnit;

                    analogue.CurrentLocalPrice = decimal.Round(analogue.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);
                    // analogue.CurrentPriceEurToUah = decimal.Round(analogue.CurrentPrice * currentExchangeRateEurToUah, 2, MidpointRounding.AwayFromZero);

                    productAnalogue.AnalogueProduct = analogue;

                    toReturn.AnalogueProducts.Add(productAnalogue);
                } else {
                    if (productAvailability == null) return product;

                    ProductAnalogue analogueFromList = toReturn.AnalogueProducts.First(a => a.Id.Equals(productAnalogue.Id));

                    if (analogueFromList.AnalogueProduct.ProductAvailabilities.Any(e => e.Id.Equals(productAvailability.Id))) return product;

                    productAvailability.Storage = storage;

                    if (!storage.ForDefective) {
                        if (storage.OrganizationId.Equals(organizationId))
                            if (storage.Locale.ToLower().Equals("pl")) {
                                if (storage.ForVatProducts)
                                    analogueFromList.AnalogueProduct.AvailableQtyPlVAT += productAvailability.Amount;
                                else
                                    analogueFromList.AnalogueProduct.AvailableQtyPl += productAvailability.Amount;
                            } else {
                                if (storage.ForVatProducts) {
                                    analogueFromList.AnalogueProduct.AvailableQtyUkVAT += productAvailability.Amount;

                                    if (storage.AvailableForReSale)
                                        analogueFromList.AnalogueProduct.AvailableQtyUk += productAvailability.Amount;
                                } else {
                                    analogueFromList.AnalogueProduct.AvailableQtyUk += productAvailability.Amount;
                                }
                            }
                        else if (storage.AvailableForReSale && !storage.Locale.ToLower().Equals("pl"))
                            analogueFromList.AnalogueProduct.AvailableQtyUk += productAvailability.Amount;
                        else if (organization != null && organization.StorageId.Equals(storage.Id))
                            analogueFromList.AnalogueProduct.AvailableQtyUk += productAvailability.Amount;
                    }

                    analogueFromList.AnalogueProduct.ProductAvailabilities.Add(productAvailability);
                }

                return product;
            };

            string analoguesExpression =
                "SELECT " +
                "[Product].ID " +
                ",[ProductAnalogue].* " +
                ", [Analogue].ID " +
                ", [Analogue].Created " +
                ", [Analogue].Deleted ";

            if (currentCulture.Equals("pl")) {
                analoguesExpression += ", [Analogue].[NameUA] AS [Name] ";
                analoguesExpression += ", [Analogue].[DescriptionUA] AS [Description] ";
                analoguesExpression += ", [Analogue].[NotesUA] AS [Notes] ";
            } else {
                analoguesExpression += ", [Analogue].[NameUA] AS [Name] ";
                analoguesExpression += ", [Analogue].[DescriptionUA] AS [Description] ";
                analoguesExpression += ", [Analogue].[NotesUA] AS [Notes] ";
            }

            analoguesExpression +=
                ", [Analogue].HasAnalogue " +
                ", [Analogue].HasComponent " +
                ", [Analogue].HasImage " +
                ", [Analogue].[Image] " +
                ", [Analogue].IsForSale " +
                ", [Analogue].IsForWeb " +
                ", [Analogue].IsForZeroSale " +
                ", [Analogue].MainOriginalNumber " +
                ", [Analogue].MeasureUnitID " +
                ", [Analogue].NetUID " +
                ", [Analogue].OrderStandard " +
                ", [Analogue].PackingStandard " +
                ", [Analogue].Standard " +
                ", [Analogue].Size " +
                ", [Analogue].[Top] " +
                ", [Analogue].UCGFEA " +
                ", [Analogue].Updated " +
                ", [Analogue].VendorCode " +
                ", [Analogue].Volume " +
                ", [Analogue].[Weight] " +
                ",dbo.GetCalculatedProductPriceWithSharesAndVat([Analogue].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentPrice " +
                ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Analogue].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentLocalPrice " +
                ",dbo.GetCalculatedProductPriceWithShares_ReSale([Analogue].NetUID, @ClientAgreementNetId, @Culture, NULL) AS CurrentPriceReSale " +
                ",dbo.GetCalculatedProductLocalPriceWithShares_ReSale([Analogue].NetUID, @ClientAgreementNetId, @Culture, NULL) AS CurrentLocalPriceReSale " +
                ",[ProductPricing].* " +
                ",[ProductAvailability].* " +
                ",[Storage].* " +
                ",[MeasureUnit].* " +
                ",[Organization].* " +
                "FROM [Product] " +
                "LEFT JOIN [ProductAnalogue] " +
                "ON [ProductAnalogue].BaseProductID = [Product].ID " +
                "LEFT JOIN [Product] AS [Analogue] " +
                "ON [Analogue].ID = [ProductAnalogue].AnalogueProductID " +
                "LEFT JOIN [ProductPricing] " +
                "ON [ProductPricing].ProductID = [Analogue].ID " +
                "AND [ProductPricing].Deleted = 0 " +
                "LEFT JOIN [ProductAvailability] " +
                "ON [ProductAvailability].ProductID = [Analogue].ID " +
                "AND [ProductAvailability].Deleted = 0 " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductAvailability].StorageID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [Analogue].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].StorageID = [Storage].ID " +
                "WHERE [Product].ID = @Id " +
                "AND [Storage].ForDefective = 0 " +
                "AND [Analogue].ID IS NOT NULL " +
                "ORDER BY " +
                "CASE WHEN ([ProductAvailability].Amount <> 0 AND [Storage].Locale = @Culture) THEN 0 ELSE 1 END, " +
                "[Analogue].VendorCode, [Analogue].Name, " +
                "CASE WHEN [Storage].Locale = @Culture THEN 0 ELSE 1 END";

            _connection.Query(
                analoguesExpression,
                types,
                analogueMapper,
                new {
                    toReturn.Id,
                    ClientAgreementNetId = clientAgreementNetId,
                    Culture = currentCulture,
                    WithVat = withVat
                }
            );
        }

        foreach (Product product in toReturn.AnalogueProducts.Select(a => a.AnalogueProduct)) {
            decimal currentExchangeRateEurToUah = _connection.Query<decimal>(
                "SELECT dbo.GetCurrentEuroExchangeRateFiltered(@ProductNetId, @CurrencyId, @WithVat, NULL)",
                new { ProductNetId = product.NetUid, CurrencyID = uahCurrencyId, WithVat = withVat }).FirstOrDefault();

            product.CurrentPriceEurToUah = decimal.Round(product.CurrentPrice * currentExchangeRateEurToUah, 2, MidpointRounding.AwayFromZero);
            product.CurrentPriceReSaleEurToUah = decimal.Round(product.CurrentPriceReSale * currentExchangeRateEurToUah, 2, MidpointRounding.AwayFromZero);
        }

        ProductIncludesHelper productIncludesHelper = new(_connection);
        //
        // if (toReturn.HasAnalogue) 
        //     productIncludesHelper.IncludeAnaloguesForProduct(toReturn, clientAgreementNetId, notIncludeWithZeroAvailability: true);


        if (!toReturn.HasComponent) return toReturn;

        types = new[] {
            typeof(Product),
            typeof(ProductSet),
            typeof(Product),
            typeof(ProductPricing),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(MeasureUnit),
            typeof(Organization)
        };

        Func<object[], Product> componentMapper = objects => {
            Product product = (Product)objects[0];
            ProductSet productSet = (ProductSet)objects[1];
            Product component = (Product)objects[2];
            ProductPricing productPricing = (ProductPricing)objects[3];
            ProductAvailability productAvailability = (ProductAvailability)objects[4];
            Storage storage = (Storage)objects[5];
            MeasureUnit measureUnit = (MeasureUnit)objects[6];
            Organization organization = (Organization)objects[7];

            if (!toReturn.ComponentProducts.Any(a => a.Id.Equals(productSet.Id))) {
                if (productPricing != null) {
                    productPricing.Price = component.CurrentPrice;

                    component.ProductPricings.Add(productPricing);
                }

                if (productAvailability != null) {
                    productAvailability.Storage = storage;

                    if (!storage.ForDefective) {
                        if (storage.OrganizationId.Equals(organizationId))
                            if (storage.Locale.ToLower().Equals("pl")) {
                                if (storage.ForVatProducts)
                                    component.AvailableQtyPlVAT += productAvailability.Amount;
                                else
                                    component.AvailableQtyPl += productAvailability.Amount;
                            } else {
                                if (storage.ForVatProducts) {
                                    component.AvailableQtyUkVAT += productAvailability.Amount;

                                    if (storage.AvailableForReSale)
                                        component.AvailableQtyUk += productAvailability.Amount;
                                } else {
                                    component.AvailableQtyUk += productAvailability.Amount;
                                }
                            }
                        else if (storage.AvailableForReSale && !storage.Locale.ToLower().Equals("pl"))
                            component.AvailableQtyUk += productAvailability.Amount;
                        else if (organization != null && organization.StorageId.Equals(storage.Id))
                            component.AvailableQtyUk += productAvailability.Amount;
                    }

                    component.ProductAvailabilities.Add(productAvailability);
                }

                component.MeasureUnit = measureUnit;

                component.CurrentLocalPrice = decimal.Round(component.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);
                // component.CurrentPriceEurToUah = decimal.Round(component.CurrentPrice * currentExchangeRateEurToUah, 2, MidpointRounding.AwayFromZero);

                productSet.ComponentProduct = component;

                toReturn.ComponentProducts.Add(productSet);
            } else {
                if (productAvailability == null) return product;

                ProductSet componentFromList = toReturn.ComponentProducts.First(a => a.Id.Equals(productSet.Id));

                if (componentFromList.ComponentProduct.ProductAvailabilities.Any(e => e.Id.Equals(productAvailability.Id))) return product;

                productAvailability.Storage = storage;

                if (!storage.ForDefective) {
                    if (storage.OrganizationId.Equals(organizationId))
                        if (storage.Locale.ToLower().Equals("pl")) {
                            if (storage.ForVatProducts)
                                componentFromList.ComponentProduct.AvailableQtyPlVAT += productAvailability.Amount;
                            else
                                componentFromList.ComponentProduct.AvailableQtyPl += productAvailability.Amount;
                        } else {
                            if (storage.ForVatProducts) {
                                componentFromList.ComponentProduct.AvailableQtyUkVAT += productAvailability.Amount;

                                if (storage.AvailableForReSale)
                                    componentFromList.ComponentProduct.AvailableQtyUk += productAvailability.Amount;
                            } else {
                                componentFromList.ComponentProduct.AvailableQtyUk += productAvailability.Amount;
                            }
                        }
                    else if (storage.AvailableForReSale && !storage.Locale.ToLower().Equals("pl"))
                        componentFromList.ComponentProduct.AvailableQtyUk += productAvailability.Amount;
                    else if (organization != null && organization.StorageId.Equals(storage.Id))
                        componentFromList.ComponentProduct.AvailableQtyUk += productAvailability.Amount;
                }

                componentFromList.ComponentProduct.ProductAvailabilities.Add(productAvailability);
            }

            return product;
        };

        string componentsExpression =
            "SELECT " +
            "[Product].ID " +
            ",[ProductSet].* " +
            ", [Component].ID " +
            ", [Component].Created " +
            ", [Component].Deleted ";

        if (currentCulture.Equals("pl")) {
            componentsExpression += ", [Component].[NameUA] AS [Name] ";
            componentsExpression += ", [Component].[DescriptionUA] AS [Description] ";
            componentsExpression += ", [Component].[NotesUA] AS [Notes] ";
        } else {
            componentsExpression += ", [Component].[NameUA] AS [Name] ";
            componentsExpression += ", [Component].[DescriptionUA] AS [Description] ";
            componentsExpression += ", [Component].[NotesUA] AS [Notes] ";
        }

        componentsExpression +=
            ", [Component].HasAnalogue " +
            ", [Component].HasComponent " +
            ", [Component].HasImage " +
            ", [Component].[Image] " +
            ", [Component].IsForSale " +
            ", [Component].IsForWeb " +
            ", [Component].IsForZeroSale " +
            ", [Component].MainOriginalNumber " +
            ", [Component].MeasureUnitID " +
            ", [Component].NetUID " +
            ", [Component].OrderStandard " +
            ", [Component].PackingStandard " +
            ", [Component].Standard " +
            ", [Component].Size " +
            ", [Component].[Top] " +
            ", [Component].UCGFEA " +
            ", [Component].Updated " +
            ", [Component].VendorCode " +
            ", [Component].Volume " +
            ", [Component].[Weight] " +
            ",dbo.GetCalculatedProductPriceWithSharesAndVat([Component].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Component].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentLocalPrice " +
            ",dbo.GetCalculatedProductPriceWithShares_ReSale([Component].NetUID, @ClientAgreementNetId, @Culture, NULL) AS CurrentPriceReSale " +
            ",dbo.GetCalculatedProductLocalPriceWithShares_ReSale([Component].NetUID, @ClientAgreementNetId, @Culture, NULL) AS CurrentLocalPriceReSale " +
            ",[ProductPricing].* " +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[MeasureUnit].* " +
            ",[Organization].* " +
            "FROM [Product] " +
            "LEFT JOIN [ProductSet] " +
            "ON [ProductSet].BaseProductID = [Product].ID " +
            "LEFT JOIN [Product] AS [Component] " +
            "ON [Component].ID = [ProductSet].ComponentProductID " +
            "LEFT JOIN [ProductPricing] " +
            "ON [ProductPricing].ProductID = [Component].ID " +
            "AND [ProductPricing].Deleted = 0 " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Component].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Component].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].StorageID = [Storage].ID " +
            "WHERE [Product].ID = @Id " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Component].ID IS NOT NULL " +
            "ORDER BY " +
            "CASE WHEN ([ProductAvailability].Amount <> 0 AND [Storage].Locale = @Culture) THEN 0 ELSE 1 END, " +
            "[Component].VendorCode, [Component].Name, " +
            "CASE WHEN [Storage].Locale = @Culture THEN 0 ELSE 1 END";

        _connection.Query(
            componentsExpression,
            types,
            componentMapper,
            new {
                toReturn.Id,
                ClientAgreementNetId = clientAgreementNetId,
                Culture = currentCulture,
                WithVat = withVat
            }
        );

        foreach (Product product in toReturn.ComponentProducts.Select(c => c.ComponentProduct)) {
            decimal currentExchangeRateEurToUah = _connection.Query<decimal>(
                "SELECT dbo.GetCurrentEuroExchangeRateFiltered(@ProductNetId, @CurrencyId, @WithVat, NULL)",
                new { ProductNetId = product.NetUid, CurrencyID = uahCurrencyId, WithVat = withVat }).FirstOrDefault();

            product.CurrentPriceEurToUah = decimal.Round(product.CurrentPrice * currentExchangeRateEurToUah, 2, MidpointRounding.AwayFromZero);
            product.CurrentPriceReSaleEurToUah = decimal.Round(product.CurrentPriceReSale * currentExchangeRateEurToUah, 2, MidpointRounding.AwayFromZero);
        }

        // productIncludesHelper.IncludeComponentsForProduct(toReturn, clientAgreementNetId, notIncludeWithZeroAvailability: true);

        bool isComponent = _connection.Query<bool>(
            "SELECT TOP 1 " +
            "CASE WHEN ProductSet.ID IS NULL THEN 0 " +
            "ELSE 1 " +
            "END " +
            "FROM ProductSet " +
            "WHERE ComponentProductID = @ProductId",
            new { ProductId = toReturn.Id }
        ).FirstOrDefault();

        if (isComponent) productIncludesHelper.IncludeProductSetForProduct(toReturn, clientAgreementNetId, true);

        return toReturn;
    }

    public Product GetById(long id) {
        Product productToReturn = null;

        string sqlExpression =
            "SELECT [Product].ID " +
            ",[Product].Created " +
            ",[Product].Deleted " +
            ",[Product].HasAnalogue " +
            ",[Product].HasComponent " +
            ",[Product].HasImage " +
            ",[Product].Image " +
            ",[Product].IsForSale " +
            ",[Product].IsForWeb " +
            ",[Product].IsForZeroSale " +
            ",[Product].MainOriginalNumber " +
            ",[Product].MeasureUnitID " +
            ",[Product].NetUID ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ", [Product].[NameUA] AS [Name] ";
            sqlExpression += ", [Product].[DescriptionUA] AS [Description] ";
            sqlExpression += ", [Product].[NotesUA] AS [Notes] ";
        } else {
            sqlExpression += ", [Product].[NameUA] AS [Name] ";
            sqlExpression += ", [Product].[DescriptionUA] AS [Description] ";
            sqlExpression += ", [Product].[NotesUA] AS [Notes] ";
        }

        sqlExpression +=
            ",[Product].[NameUA] " +
            ",[Product].[DescriptionUA] " +
            ",[Product].[NameUA] " +
            ",[Product].[DescriptionUA] " +
            ",[Product].OrderStandard " +
            ",[Product].PackingStandard " +
            ",[Product].Standard " +
            ",[Product].Size " +
            ",[Product].[Top] " +
            ",[Product].UCGFEA " +
            ",[Product].Updated " +
            ",[Product].VendorCode " +
            ",[Product].Volume " +
            ",[Product].Weight " +
            ",ProductPricing.* " +
            ",ProductProductGroup.* " +
            ",ProductSpecification.* " +
            ",[User].* " +
            ",MeasureUnit.* " +
            ",[ProductImage].* " +
            "FROM Product " +
            "LEFT JOIN ProductPricing " +
            "ON ProductPricing.ProductID = Product.ID " +
            "AND ProductPricing.Deleted = 0 " +
            "LEFT JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID " +
            "AND ProductProductGroup.Deleted = 0 " +
            "LEFT JOIN ProductSpecification " +
            "ON ProductSpecification.ProductID = Product.ID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = ProductSpecification.AddedById " +
            "LEFT JOIN MeasureUnit " +
            "ON MeasureUnit.ID = Product.MeasureUnitID " +
            "LEFT JOIN [ProductImage] " +
            "ON [ProductImage].ProductID = [Product].ID " +
            "AND [ProductImage].Deleted = 0 " +
            "WHERE Product.ID = @Id " +
            "ORDER BY ProductSpecification.Created";

        _connection.Query<Product, ProductPricing, ProductProductGroup, ProductSpecification, User, MeasureUnit, ProductImage, Product>(
            sqlExpression,
            (product, productPricing, productProductGroup, specification, user, measureUnit, image) => {
                if (productToReturn != null) {
                    if (productPricing != null && !productToReturn.ProductPricings.Any(p => p.Id.Equals(productToReturn.Id))) productToReturn.ProductPricings.Add(productPricing);

                    if (productPricing != null && !productToReturn.ProductProductGroups.Any(p => p.Id.Equals(productProductGroup.Id)))
                        productToReturn.ProductProductGroups.Add(productProductGroup);

                    if (specification != null && !productToReturn.ProductSpecifications.Any(s => s.Id.Equals(specification.Id))) {
                        specification.AddedBy = user;

                        productToReturn.ProductSpecifications.Add(specification);
                    }

                    if (image != null && !productToReturn.ProductImages.Any(i => i.Id.Equals(image.Id))) productToReturn.ProductImages.Add(image);
                } else {
                    if (productPricing != null) product.ProductPricings.Add(productPricing);

                    if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

                    if (specification != null) {
                        specification.AddedBy = user;

                        product.ProductSpecifications.Add(specification);
                    }

                    if (image != null) product.ProductImages.Add(image);

                    product.MeasureUnit = measureUnit;

                    productToReturn = product;
                }

                return product;
            },
            new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        if (productToReturn == null) return productToReturn;

        var joinProps = new {
            productToReturn.Id,
            ClientAgreementNetId = Guid.Empty,
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
        };

        string productsExpression =
            "SELECT [Product].ID " +
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
            ",dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, 0, NULL) AS ProductCurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, 0, NULL) AS ProductCurrentLocalPrice " +
            "FROM [Product] " +
            "LEFT JOIN [ProductOriginalNumber] " +
            "ON [ProductOriginalNumber].ProductID = [Product].ID " +
            "AND [ProductOriginalNumber].Deleted = 0 " +
            "LEFT JOIN [OriginalNumber] " +
            "ON [ProductOriginalNumber].OriginalNumberID = [OriginalNumber].ID " +
            "LEFT JOIN [ProductProductGroup] " +
            "ON [ProductProductGroup].ProductID = [Product].ID " +
            "AND [ProductProductGroup].Deleted = 0 " +
            "LEFT JOIN [ProductGroup] " +
            "ON [ProductGroup].ID = [ProductProductGroup].ProductGroupID " +
            "LEFT JOIN [ProductCategory] " +
            "ON [ProductCategory].ProductID = [Product].ID " +
            "AND [ProductCategory].Deleted = 0 " +
            "LEFT JOIN [Category] " +
            "ON [Category].ID = [ProductCategory].CategoryID " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "LEFT JOIN [ProductPricing] " +
            "ON [ProductPricing].ProductID = [Product].ID " +
            "AND [ProductPricing].Deleted = 0 " +
            "LEFT JOIN [views].[PricingView] AS [Pricing] " +
            "ON [Pricing].ID = [ProductPricing].PricingID " +
            "AND [Pricing].CultureCode = @Culture " +
            "WHERE [Product].ID = @Id";

        Type[] productsTypes = {
            typeof(Product),
            typeof(ProductOriginalNumber),
            typeof(OriginalNumber),
            typeof(ProductProductGroup),
            typeof(ProductGroup),
            typeof(ProductCategory),
            typeof(Category),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(ProductPricing),
            typeof(Pricing),
            typeof(decimal),
            typeof(decimal)
        };

        Func<object[], Product> productsMapper = objects => {
            Product product = (Product)objects[0];
            ProductOriginalNumber productOriginalNumber = (ProductOriginalNumber)objects[1];
            OriginalNumber originalNumber = (OriginalNumber)objects[2];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[3];
            ProductGroup productGroup = (ProductGroup)objects[4];
            ProductCategory productCategory = (ProductCategory)objects[5];
            Category category = (Category)objects[6];
            ProductAvailability productAvailability = (ProductAvailability)objects[7];
            Storage storage = (Storage)objects[8];
            ProductPricing productPricing = (ProductPricing)objects[9];
            Pricing pricing = (Pricing)objects[10];
            decimal currentPrice = (decimal)objects[11];
            decimal currentLocalPrice = (decimal)objects[12];

            productToReturn.CurrentPrice = currentPrice;
            productToReturn.CurrentLocalPrice = currentLocalPrice;

            if (productOriginalNumber != null && !productToReturn.ProductOriginalNumbers.Any(n => n.Id.Equals(productOriginalNumber.Id))) {
                productOriginalNumber.OriginalNumber = originalNumber;

                productToReturn.ProductOriginalNumbers.Add(productOriginalNumber);
            }

            if (productProductGroup != null && !productToReturn.ProductProductGroups.Any(g => g.Id.Equals(productProductGroup.Id))) {
                productProductGroup.ProductGroup = productGroup;

                productToReturn.ProductProductGroups.Add(productProductGroup);
            }

            if (productCategory != null && !productToReturn.ProductCategories.Any(c => c.Id.Equals(productCategory.Id))) {
                productCategory.Category = category;

                productToReturn.ProductCategories.Add(productCategory);
            }

            if (productAvailability != null && !productToReturn.ProductAvailabilities.Any(a => a.Id.Equals(productAvailability.Id))) {
                productAvailability.Storage = storage;

                productToReturn.ProductAvailabilities.Add(productAvailability);
            }

            if (productPricing == null || productToReturn.ProductPricings.Any(p => p.Id.Equals(productPricing.Id))) return product;

            productPricing.Pricing = pricing;

            productToReturn.ProductPricings.Add(productPricing);

            return product;
        };

        _connection.Query(
            productsExpression,
            productsTypes,
            productsMapper,
            joinProps,
            splitOn: "ID,ProductCurrentPrice,ProductCurrentLocalPrice"
        );

        if (productToReturn == null) return productToReturn;

        ProductIncludesHelper productIncludesHelper = new(_connection);

        if (productToReturn.HasAnalogue) productIncludesHelper.IncludeAnaloguesForProduct(productToReturn);

        if (productToReturn.HasComponent) productIncludesHelper.IncludeComponentsForProduct(productToReturn, withAnalogues: true);

        return productToReturn;
    }

    public List<Product> GetAll() {
        return _connection.Query<Product, ProductAvailability, Product>(
            "SELECT [Product].*, [ProductAvailability].* " +
            "FROM Product " +
            "INNER JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Product].ID " +
            "WHERE [Product].Deleted = 0 " +
            "AND [ProductAvailability].Deleted = 0 ",
            (product, productAvailability) => {
                return product;
            }
        ).ToList();
    }

    public Product GetByNetIdWithoutIncludes(Guid netId) {
        return _connection.Query<Product>(
            "SELECT * " +
            ", CONVERT(nvarchar(32), SourceAmgID, 2) [RefId] " +
            " FROM Product " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        ).SingleOrDefault();
    }

    public Product GetProductByNetId(
        Guid netId,
        Guid? clientAgreementNetId,
        bool withVat,
        long? currencyId,
        long? organizationId) {
        Product productToReturn = null;

        string sqlExpression =
            "SELECT [Product].ID " +
            ",[Product].Created " +
            ",[Product].Deleted " +
            ",[Product].HasAnalogue " +
            ",[Product].HasComponent " +
            ",[Product].HasImage " +
            ",[Product].Image " +
            ",[Product].IsForSale " +
            ",[Product].IsForWeb " +
            ",[Product].IsForZeroSale " +
            ",[Product].MainOriginalNumber " +
            ",[Product].MeasureUnitID " +
            ",[Product].NetUID ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ", [Product].[NameUA] AS [Name] ";
            sqlExpression += ", [Product].[DescriptionUA] AS [Description] ";
            sqlExpression += ", [Product].[NotesUA] AS [Notes] ";
        } else {
            sqlExpression += ", [Product].[NameUA] AS [Name] ";
            sqlExpression += ", [Product].[DescriptionUA] AS [Description] ";
            sqlExpression += ", [Product].[NotesUA] AS [Notes] ";
        }

        sqlExpression +=
            ",[Product].[NameUA] " +
            ",[Product].[DescriptionUA] " +
            ",[Product].[NameUA] " +
            ",[Product].[SynonymsUA] " +
            ",[Product].[SynonymsPL] " +
            ",[Product].[DescriptionUA] " +
            ",[Product].OrderStandard " +
            ",[Product].PackingStandard " +
            ",[Product].Standard " +
            ",[Product].Size " +
            ",[Product].[Top] " +
            ",[Product].UCGFEA " +
            ",[Product].Updated " +
            ",[Product].VendorCode " +
            ",[Product].Volume " +
            ",[Product].Weight " +
            ", dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS [CurrentPrice] " +
            ", dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS [CurrentLocalPrice] " +
            ", (SELECT Code FROM Currency " +
            "WHERE ID = @CurrencyId " +
            "AND Deleted = 0) AS CurrencyCode " +
            ",[ProductProductGroup].* " +
            ",[ProductGroup].* " +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[MeasureUnit].* " +
            ",[ProductImage].* " +
            ",[ProductOriginalNumber].* " +
            ",[OriginalNumber].* " +
            ",[SupplyOrderItem].* " +
            ",[SupplyOrder].* " +
            ",[SupplyOrderUkraineItem].* " +
            ",[SupplyOrderUkraine].* " +
            "FROM Product " +
            "LEFT JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID " +
            "AND ProductProductGroup.Deleted = 0 " +
            "LEFT JOIN ProductGroup " +
            "ON ProductGroup.ID = ProductProductGroup.ProductGroupID " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID ";
        if (withVat)
            sqlExpression +=
                "AND [Storage].OrganizationID = @OrganizationId " +
                "AND [Storage].ForDefective = 0 " +
                "AND [Storage].Locale = @Culture ";
        else
            sqlExpression +=
                "AND [Storage].ID IN ( " +
                "SELECT ID FROM Storage " +
                "WHERE Deleted = 0 " +
                "AND [Storage].Locale = @Culture " +
                "AND [Storage].ForDefective = 0 " +
                "AND [Storage].AvailableForReSale = 1 " +
                "UNION " +
                "SELECT ID FROM Storage " +
                "WHERE [Storage].Deleted = 0 " +
                "AND [Storage].OrganizationID = @OrganizationId " +
                "AND [Storage].Locale = @Culture) ";
        sqlExpression +=
            "LEFT JOIN MeasureUnit " +
            "ON MeasureUnit.ID = Product.MeasureUnitID " +
            "LEFT JOIN [ProductImage] " +
            "ON [ProductImage].ProductID = [Product].ID " +
            "AND [ProductImage].Deleted = 0 " +
            "LEFT JOIN [ProductOriginalNumber] " +
            "ON [ProductOriginalNumber].ProductID = [Product].ID " +
            "AND [ProductOriginalNumber].Deleted = 0 " +
            "LEFT JOIN [OriginalNumber] " +
            "ON [ProductOriginalNumber].OriginalNumberID = [OriginalNumber].ID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON SupplyOrderItem.ProductID = Product.ID " +
            "LEFT JOIN SupplyOrder " +
            "ON SupplyOrder.ID = [SupplyOrderItem].SupplyOrderID " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].ProductID = Product.ID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "WHERE Product.NetUID = @NetId ";


        Type[] productTypes = {
            typeof(Product),
            typeof(ProductProductGroup),
            typeof(ProductGroup),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(MeasureUnit),
            typeof(ProductImage),
            typeof(ProductOriginalNumber),
            typeof(OriginalNumber),
            typeof(SupplyOrderItem),
            typeof(SupplyOrder),
            typeof(SupplyOrderUkraineItem),
            typeof(SupplyOrderUkraine)
        };

        Func<object[], Product> productMapper = objects => {
            Product product = (Product)objects[0];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[1];
            ProductGroup productGroup = (ProductGroup)objects[2];
            ProductAvailability productAvailability = (ProductAvailability)objects[3];
            Storage storage = (Storage)objects[4];
            MeasureUnit measureUnit = (MeasureUnit)objects[5];
            ProductImage image = (ProductImage)objects[6];
            ProductOriginalNumber productOriginalNumber = (ProductOriginalNumber)objects[7];
            OriginalNumber originalNumber = (OriginalNumber)objects[8];
            SupplyOrderItem SupplyOrderItem = (SupplyOrderItem)objects[9];
            SupplyOrder SupplyOrder = (SupplyOrder)objects[10];
            SupplyOrderUkraineItem SupplyOrderUkraineItem = (SupplyOrderUkraineItem)objects[11];
            SupplyOrderUkraine SupplyOrderUkraine = (SupplyOrderUkraine)objects[12];

            if (productToReturn != null) {
                if (productOriginalNumber != null && !productToReturn.ProductOriginalNumbers.Any(n => n.Id.Equals(productOriginalNumber.Id))) {
                    productOriginalNumber.OriginalNumber = originalNumber;

                    productToReturn.ProductOriginalNumbers.Add(productOriginalNumber);
                }

                if (SupplyOrderUkraine != null)
                    if (!SupplyOrderUkraine.IsPlaced)
                        productToReturn.AvailableQtyRoad += SupplyOrderUkraineItem.Qty;

                if (SupplyOrder != null)
                    if (!SupplyOrder.IsFullyPlaced)
                        productToReturn.AvailableQtyRoad += SupplyOrderItem.Qty;

                if (productProductGroup != null && !productToReturn.ProductProductGroups.Any(p => p.Id.Equals(productProductGroup.Id))) {
                    if (productGroup != null) {
                        productProductGroup.ProductGroup = productGroup;
                        productProductGroup.ProductGroupId = productGroup.Id;
                    }

                    productToReturn.ProductProductGroups.Add(productProductGroup);
                }

                if (productAvailability != null && !productToReturn.ProductAvailabilities.Any(a => a.Id.Equals(productAvailability.Id))) {
                    if (storage.Locale.ToLower().Equals("pl")) {
                        productToReturn.AvailableQtyPl += productAvailability.Amount;
                        productToReturn.ProductAvailabilities.Add(productAvailability);
                    } else {
                        productToReturn.AvailableQtyUk += productAvailability.Amount;
                        productToReturn.ProductAvailabilities.Add(productAvailability);
                    }
                }

                if (image != null && !productToReturn.ProductImages.Any(i => i.Id.Equals(image.Id))) productToReturn.ProductImages.Add(image);
            } else {
                product.CurrentLocalPrice = decimal.Round(product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);
                product.CurrentPrice = decimal.Round(product.CurrentPrice, 2, MidpointRounding.AwayFromZero);
                if (SupplyOrderUkraine != null)
                    if (!SupplyOrderUkraine.IsPlaced)
                        product.AvailableQtyRoad += SupplyOrderUkraineItem.Qty;

                if (SupplyOrder != null)
                    if (!SupplyOrder.IsFullyPlaced)
                        product.AvailableQtyRoad += SupplyOrderItem.Qty;

                product.MeasureUnit = measureUnit;

                if (image != null) product.ProductImages.Add(image);

                if (productProductGroup != null) {
                    if (productGroup != null) {
                        productProductGroup.ProductGroup = productGroup;
                        productProductGroup.ProductGroupId = productGroup.Id;
                    }

                    product.ProductProductGroups.Add(productProductGroup);
                }

                if (productOriginalNumber != null) {
                    productOriginalNumber.OriginalNumber = originalNumber;

                    product.ProductOriginalNumbers.Add(productOriginalNumber);
                }

                if (productAvailability != null) {
                    if (storage.Locale.ToLower().Equals("pl")) {
                        product.AvailableQtyPl += productAvailability.Amount;
                        product.ProductAvailabilities.Add(productAvailability);
                    } else {
                        product.AvailableQtyUk += productAvailability.Amount;
                        product.ProductAvailabilities.Add(productAvailability);
                    }
                }

                productToReturn = product;
            }

            return product;
        };

        _connection.Query(
            sqlExpression,
            productTypes,
            productMapper,
            new {
                NetId = netId,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                ClientAgreementNetId = clientAgreementNetId,
                WithVat = withVat,
                OrganizationId = organizationId,
                CurrencyId = currencyId
            }
        );

        if (productToReturn == null) return productToReturn;

        Type[] analogueTypes = {
            typeof(Product),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(ProductSlug),
            typeof(ProductAnalogue),
            typeof(ProductImage)
        };

        Func<object[], Product> analoguesMapper = objects => {
            Product analogue = (Product)objects[0];
            ProductAvailability productAvailability = (ProductAvailability)objects[1];
            Storage storage = (Storage)objects[2];
            ProductSlug productSlug = (ProductSlug)objects[3];
            ProductAnalogue productAnalogue = (ProductAnalogue)objects[4];
            ProductImage image = (ProductImage)objects[5];

            if (analogue == null) return null;

            analogue.CurrentLocalPrice = decimal.Round(analogue.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);
            analogue.CurrentPrice = decimal.Round(analogue.CurrentPrice, 2, MidpointRounding.AwayFromZero);

            if (productToReturn.AnalogueProducts.Any(a => a.AnalogueProductId.Equals(analogue.Id))) {
                if (storage.Locale.ToLower().Equals("pl"))
                    productToReturn.AnalogueProducts.Single(a =>
                        a.AnalogueProductId.Equals(analogue.Id)).AnalogueProduct.AvailableQtyPl += productAvailability.Amount;
                else
                    productToReturn.AnalogueProducts.Single(a =>
                        a.AnalogueProductId.Equals(analogue.Id)).AnalogueProduct.AvailableQtyUk += productAvailability.Amount;
            } else {
                if (productAnalogue == null) return analogue;

                analogue.ProductSlug = productSlug;

                if (image != null) analogue.ProductImages.Add(image);

                productAnalogue.AnalogueProduct = analogue;

                if (productAvailability == null) return analogue;

                if (storage.Locale.ToLower().Equals("pl"))
                    analogue.AvailableQtyPl += productAvailability.Amount;
                else
                    analogue.AvailableQtyUk += productAvailability.Amount;

                if (!productToReturn.AnalogueProducts.Any(a => a.AnalogueProductId.Equals(analogue.Id))) productToReturn.AnalogueProducts.Add(productAnalogue);
            }

            return analogue;
        };

        string analogueExpression =
            "SELECT " +
            "[Analogue].ID " +
            ", [Analogue].Created " +
            ", [Analogue].Deleted ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            analogueExpression += ", [Analogue].[NameUA] AS [Name] ";
            analogueExpression += ", [Analogue].[DescriptionUA] AS [Description] ";
            analogueExpression += ", [Analogue].[NotesUA] AS [Notes] ";
        } else {
            analogueExpression += ", [Analogue].[NameUA] AS [Name] ";
            analogueExpression += ", [Analogue].[DescriptionUA] AS [Description] ";
            analogueExpression += ", [Analogue].[NotesUA] AS [Notes] ";
        }

        analogueExpression +=
            ", [Analogue].HasAnalogue " +
            ", [Analogue].HasComponent " +
            ", [Analogue].HasImage " +
            ", [Analogue].[Image] " +
            ", [Analogue].IsForSale " +
            ", [Analogue].IsForWeb " +
            ", [Analogue].IsForZeroSale " +
            ", [Analogue].MainOriginalNumber " +
            ", [Analogue].MeasureUnitID " +
            ", [Analogue].NetUID " +
            ", [Analogue].OrderStandard " +
            ", [Analogue].PackingStandard " +
            ", [Analogue].Standard " +
            ", [Analogue].Size " +
            ", [Analogue].[Top] " +
            ", [Analogue].UCGFEA " +
            ", [Analogue].Updated " +
            ", [Analogue].VendorCode " +
            ", [Analogue].Volume " +
            ", [Analogue].[Weight] " +
            ", dbo.GetCalculatedProductPriceWithSharesAndVat([Analogue].NetUID, @ClientAgreementNetId, @Culture, @WithVat, null) AS [CurrentPrice] " +
            ", dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Analogue].NetUID, @ClientAgreementNetId, @Culture, @WithVat, null) AS [CurrentLocalPrice] " +
            ", (SELECT Code FROM Currency " +
            "WHERE ID = @CurrencyId " +
            "AND Deleted = 0) AS CurrencyCode " +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[ProductSlug].* " +
            ",[ProductAnalogue].* " +
            ",[ProductImage].* " +
            "FROM [ProductAnalogue] " +
            "LEFT JOIN [Product] AS [Analogue] " +
            "ON [Analogue].ID = [ProductAnalogue].AnalogueProductID " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Analogue].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "AND [Storage].Deleted = 0 " +
            "LEFT JOIN [ProductSlug] " +
            "ON [ProductSlug].ID = ( " +
            "SELECT TOP(1) ID " +
            "FROM [ProductSlug] " +
            "WHERE [ProductSlug].Deleted = 0 " +
            "AND [ProductSlug].[Locale] = @Culture " +
            "AND [ProductSlug].ProductID = [Analogue].ID " +
            ") " +
            "LEFT JOIN [ProductImage] " +
            "ON [ProductImage].ProductID = [Analogue].ID " +
            "AND [ProductImage].Deleted = 0 " +
            "WHERE [ProductAnalogue].BaseProductID = @Id " +
            "AND [ProductAnalogue].Deleted = 0 " +
            "AND [Storage].Locale = @Culture " +
            "AND [Storage].ForDefective = 0 ";
        if (withVat)
            analogueExpression +=
                "AND [Storage].ID = @OrganizationId " +
                "ORDER BY [ProductAvailability].Amount DESC, [Analogue].Name, [Analogue].VendorCode ";
        else
            analogueExpression +=
                "AND [Storage].ID IN " +
                "(SELECT [Storage].ID FROM [Storage] " +
                "WHERE Deleted = 0 " +
                "AND [Storage].Locale = @Culture " +
                "AND [Storage].ForDefective = 0 " +
                "AND [Storage].AvailableForReSale = 1 " +
                "UNION " +
                "SELECT ID FROM [Storage] " +
                "WHERE [Storage].Deleted = 0 " +
                "AND [Storage].OrganizationID = @OrganizationId) " +
                "AND [Storage].Locale = @Culture " +
                "ORDER BY [ProductAvailability].Amount DESC, [Analogue].Name, [Analogue].VendorCode ";

        _connection.Query(
            analogueExpression,
            analogueTypes,
            analoguesMapper,
            new {
                productToReturn.Id,
                ClientAgreementNetId = clientAgreementNetId,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                OrganizationId = organizationId,
                CurrencyId = currencyId,
                WithVat = withVat
            }
        );

        return productToReturn;
    }

    public Product GetByNetIdForRetail(Guid netId, long organizationId, bool withVat) {
        ClientAgreement clientAgreement = _connection.Query<ClientAgreement, Agreement, ClientAgreement>(
            "SELECT [ClientAgreement].* " +
            ", [Agreement].* " +
            "FROM [ClientAgreement] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "WHERE [Client].IsForRetail = 1 " +
            "AND [Client].Deleted = 0 " +
            "AND [ClientAgreement].Deleted = 0 " +
            "AND [Agreement].[WithVatAccounting] = @WithVat " +
            "AND [Agreement].OrganizationID = @OrganizationId ",
            (currentClientAgreement, agreement) => {
                currentClientAgreement.Agreement = agreement;

                return currentClientAgreement;
            },
            new {
                OrganizationId = organizationId,
                WithVat = withVat
            }
        ).First();

        Product productToReturn = null;

        string sqlExpression =
            "SELECT [Product].ID " +
            ",[Product].Created " +
            ",[Product].Deleted " +
            ",[Product].HasAnalogue " +
            ",[Product].HasComponent " +
            ",[Product].HasImage " +
            ",[Product].Image " +
            ",[Product].IsForSale " +
            ",[Product].IsForWeb " +
            ",[Product].IsForZeroSale " +
            ",[Product].MainOriginalNumber " +
            ",[Product].MeasureUnitID " +
            ",[Product].NetUID ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ", [Product].[NameUA] AS [Name] ";
            sqlExpression += ", [Product].[DescriptionUA] AS [Description] ";
            sqlExpression += ", [Product].[NotesUA] AS [Notes] ";
        } else {
            sqlExpression += ", [Product].[NameUA] AS [Name] ";
            sqlExpression += ", [Product].[DescriptionUA] AS [Description] ";
            sqlExpression += ", [Product].[NotesUA] AS [Notes] ";
        }

        sqlExpression +=
            ",[Product].[NameUA] " +
            ",[Product].[DescriptionUA] " +
            ",[Product].[NameUA] " +
            ",[Product].[SynonymsUA] " +
            ",[Product].[SynonymsPL] " +
            ",[Product].[DescriptionUA] " +
            ",[Product].OrderStandard " +
            ",[Product].PackingStandard " +
            ",[Product].Standard " +
            ",[Product].Size " +
            ",[Product].[Top] " +
            ",[Product].UCGFEA " +
            ",[Product].Updated " +
            ",[Product].VendorCode " +
            ",[Product].Volume " +
            ",[Product].Weight " +
            ", dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS [CurrentPrice] " +
            ", dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS [CurrentLocalPrice] " +
            ", (SELECT Code FROM Currency " +
            "WHERE ID = @CurrencyId " +
            "AND Deleted = 0) AS CurrencyCode " +
            ",[ProductProductGroup].* " +
            ",[ProductGroup].* " +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[MeasureUnit].* " +
            ",[ProductImage].* " +
            ",[ProductOriginalNumber].* " +
            ",[OriginalNumber].* " +
            ",[SupplyOrderItem].* " +
            ",[SupplyOrder].* " +
            ",[SupplyOrderUkraineItem].* " +
            ",[SupplyOrderUkraine].* " +
            "FROM Product " +
            "LEFT JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID " +
            "AND ProductProductGroup.Deleted = 0 " +
            "LEFT JOIN ProductGroup " +
            "ON ProductGroup.ID = ProductProductGroup.ProductGroupID " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "AND [Storage].Deleted = 0 " +
            "LEFT JOIN MeasureUnit " +
            "ON MeasureUnit.ID = Product.MeasureUnitID " +
            "LEFT JOIN [ProductImage] " +
            "ON [ProductImage].ProductID = [Product].ID " +
            "AND [ProductImage].Deleted = 0 " +
            "LEFT JOIN [ProductOriginalNumber] " +
            "ON [ProductOriginalNumber].ProductID = [Product].ID " +
            "AND [ProductOriginalNumber].Deleted = 0 " +
            "LEFT JOIN [OriginalNumber] " +
            "ON [ProductOriginalNumber].OriginalNumberID = [OriginalNumber].ID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON SupplyOrderItem.ProductID = Product.ID " +
            "LEFT JOIN SupplyOrder " +
            "ON SupplyOrder.ID = [SupplyOrderItem].SupplyOrderID " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].ProductID = Product.ID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "WHERE Product.NetUID = @NetId " +
            "AND (Storage.ForEcommerce = 1 OR Storage.ID IS NULL) ";

        Type[] productTypes = {
            typeof(Product),
            typeof(ProductProductGroup),
            typeof(ProductGroup),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(MeasureUnit),
            typeof(ProductImage),
            typeof(ProductOriginalNumber),
            typeof(OriginalNumber),
            typeof(SupplyOrderItem),
            typeof(SupplyOrder),
            typeof(SupplyOrderUkraineItem),
            typeof(SupplyOrderUkraine)
        };

        Func<object[], Product> productMapper = objects => {
            Product product = (Product)objects[0];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[1];
            ProductGroup productGroup = (ProductGroup)objects[2];
            ProductAvailability productAvailability = (ProductAvailability)objects[3];
            Storage storage = (Storage)objects[4];
            MeasureUnit measureUnit = (MeasureUnit)objects[5];
            ProductImage image = (ProductImage)objects[6];
            ProductOriginalNumber productOriginalNumber = (ProductOriginalNumber)objects[7];
            OriginalNumber originalNumber = (OriginalNumber)objects[8];
            SupplyOrderItem SupplyOrderItem = (SupplyOrderItem)objects[9];
            SupplyOrder SupplyOrder = (SupplyOrder)objects[10];
            SupplyOrderUkraineItem SupplyOrderUkraineItem = (SupplyOrderUkraineItem)objects[11];
            SupplyOrderUkraine SupplyOrderUkraine = (SupplyOrderUkraine)objects[12];
            if (productToReturn != null) {
                if (productOriginalNumber != null && !productToReturn.ProductOriginalNumbers.Any(n => n.Id.Equals(productOriginalNumber.Id))) {
                    productOriginalNumber.OriginalNumber = originalNumber;

                    productToReturn.ProductOriginalNumbers.Add(productOriginalNumber);
                }

                if (SupplyOrderUkraine != null)
                    if (!SupplyOrderUkraine.IsPlaced)
                        productToReturn.AvailableQtyRoad += SupplyOrderUkraineItem.Qty;

                if (SupplyOrder != null)
                    if (!SupplyOrder.IsFullyPlaced)
                        productToReturn.AvailableQtyRoad += SupplyOrderItem.Qty;

                if (productProductGroup != null && !productToReturn.ProductProductGroups.Any(p => p.Id.Equals(productProductGroup.Id))) {
                    if (productGroup != null) {
                        productProductGroup.ProductGroup = productGroup;
                        productProductGroup.ProductGroupId = productGroup.Id;
                    }

                    productToReturn.ProductProductGroups.Add(productProductGroup);
                }

                if (productAvailability != null && !productToReturn.ProductAvailabilities.Any(a => a.Id.Equals(productAvailability.Id))) {
                    if (storage.Locale.ToLower().Equals("pl")) {
                        productToReturn.AvailableQtyPl += productAvailability.Amount;
                        productToReturn.ProductAvailabilities.Add(productAvailability);
                    } else {
                        productToReturn.AvailableQtyUk += productAvailability.Amount;
                        productToReturn.ProductAvailabilities.Add(productAvailability);
                    }
                }

                if (image != null && !productToReturn.ProductImages.Any(i => i.Id.Equals(image.Id))) productToReturn.ProductImages.Add(image);
            } else {
                product.CurrentLocalPrice = decimal.Round(product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);
                product.CurrentPrice = decimal.Round(product.CurrentPrice, 2, MidpointRounding.AwayFromZero);

                product.MeasureUnit = measureUnit;

                if (SupplyOrderUkraine != null)
                    if (!SupplyOrderUkraine.IsPlaced)
                        product.AvailableQtyRoad += SupplyOrderUkraineItem.Qty;

                if (SupplyOrder != null)
                    if (!SupplyOrder.IsFullyPlaced)
                        product.AvailableQtyRoad += SupplyOrderItem.Qty;

                if (image != null) product.ProductImages.Add(image);

                if (productProductGroup != null) {
                    if (productGroup != null) {
                        productProductGroup.ProductGroup = productGroup;
                        productProductGroup.ProductGroupId = productGroup.Id;
                    }

                    product.ProductProductGroups.Add(productProductGroup);
                }

                if (productOriginalNumber != null) {
                    productOriginalNumber.OriginalNumber = originalNumber;

                    product.ProductOriginalNumbers.Add(productOriginalNumber);
                }

                if (productAvailability != null) {
                    if (storage.Locale.ToLower().Equals("pl")) {
                        product.AvailableQtyPl += productAvailability.Amount;
                        product.ProductAvailabilities.Add(productAvailability);
                    } else {
                        product.AvailableQtyUk += productAvailability.Amount;
                        product.ProductAvailabilities.Add(productAvailability);
                    }
                }

                productToReturn = product;
            }

            return product;
        };

        _connection.Query(
            sqlExpression,
            productTypes,
            productMapper,
            new {
                NetId = netId,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                ClientAgreementNetId = clientAgreement.NetUid,
                WithVat = clientAgreement.Agreement.WithVATAccounting,
                clientAgreement.Agreement.OrganizationId,
                clientAgreement.Agreement.CurrencyId
            }
        );

        if (productToReturn == null) return productToReturn;

        Type[] analogueTypes = {
            typeof(Product),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(ProductSlug),
            typeof(ProductAnalogue),
            typeof(ProductImage)
        };

        Func<object[], Product> analoguesMapper = objects => {
            Product analogue = (Product)objects[0];
            ProductAvailability productAvailability = (ProductAvailability)objects[1];
            Storage storage = (Storage)objects[2];
            ProductSlug productSlug = (ProductSlug)objects[3];
            ProductAnalogue productAnalogue = (ProductAnalogue)objects[4];
            ProductImage image = (ProductImage)objects[5];

            if (analogue == null) return null;

            analogue.CurrentLocalPrice = decimal.Round(analogue.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);
            analogue.CurrentPrice = decimal.Round(analogue.CurrentPrice, 2, MidpointRounding.AwayFromZero);

            if (productToReturn.AnalogueProducts.Any(a => a.AnalogueProductId.Equals(analogue.Id))) {
                if (storage.Locale.ToLower().Equals("pl"))
                    productToReturn.AnalogueProducts.Single(a =>
                        a.AnalogueProductId.Equals(analogue.Id)).AnalogueProduct.AvailableQtyPl += productAvailability.Amount;
                else
                    productToReturn.AnalogueProducts.Single(a =>
                        a.AnalogueProductId.Equals(analogue.Id)).AnalogueProduct.AvailableQtyUk += productAvailability.Amount;
            } else {
                if (productAnalogue == null) return analogue;

                analogue.ProductSlug = productSlug;

                if (image != null) analogue.ProductImages.Add(image);

                productAnalogue.AnalogueProduct = analogue;

                if (productAvailability == null) return analogue;

                if (storage.Locale.ToLower().Equals("pl"))
                    analogue.AvailableQtyPl += productAvailability.Amount;
                else
                    analogue.AvailableQtyUk += productAvailability.Amount;

                if (!productToReturn.AnalogueProducts.Any(a => a.AnalogueProductId.Equals(analogue.Id))) productToReturn.AnalogueProducts.Add(productAnalogue);
            }

            return analogue;
        };

        string analogueExpression =
            "SELECT " +
            "[Analogue].ID " +
            ", [Analogue].Created " +
            ", [Analogue].Deleted ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            analogueExpression += ", [Analogue].[NameUA] AS [Name] ";
            analogueExpression += ", [Analogue].[DescriptionUA] AS [Description] ";
            analogueExpression += ", [Analogue].[NotesUA] AS [Notes] ";
        } else {
            analogueExpression += ", [Analogue].[NameUA] AS [Name] ";
            analogueExpression += ", [Analogue].[DescriptionUA] AS [Description] ";
            analogueExpression += ", [Analogue].[NotesUA] AS [Notes] ";
        }

        analogueExpression +=
            ", [Analogue].HasAnalogue " +
            ", [Analogue].HasComponent " +
            ", [Analogue].HasImage " +
            ", [Analogue].[Image] " +
            ", [Analogue].IsForSale " +
            ", [Analogue].IsForWeb " +
            ", [Analogue].IsForZeroSale " +
            ", [Analogue].MainOriginalNumber " +
            ", [Analogue].MeasureUnitID " +
            ", [Analogue].NetUID " +
            ", [Analogue].OrderStandard " +
            ", [Analogue].PackingStandard " +
            ", [Analogue].Standard " +
            ", [Analogue].Size " +
            ", [Analogue].[Top] " +
            ", [Analogue].UCGFEA " +
            ", [Analogue].Updated " +
            ", [Analogue].VendorCode " +
            ", [Analogue].Volume " +
            ", [Analogue].[Weight] " +
            ", dbo.GetCalculatedProductPriceWithSharesAndVat([Analogue].NetUID, @ClientAgreementNetId, @Culture, @WithVat, null) AS [CurrentPrice] " +
            ", dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Analogue].NetUID, @ClientAgreementNetId, @Culture, @WithVat, null) AS [CurrentLocalPrice] " +
            ", (SELECT Code FROM Currency " +
            "WHERE ID = @CurrencyId " +
            "AND Deleted = 0) AS CurrencyCode " +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[ProductSlug].* " +
            ",[ProductAnalogue].* " +
            ",[ProductImage].* " +
            "FROM [ProductAnalogue] " +
            "LEFT JOIN [Product] AS [Analogue] " +
            "ON [Analogue].ID = [ProductAnalogue].AnalogueProductID " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Analogue].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "AND [Storage].Deleted = 0 " +
            "LEFT JOIN [ProductSlug] " +
            "ON [ProductSlug].ID = ( " +
            "SELECT TOP(1) ID " +
            "FROM [ProductSlug] " +
            "WHERE [ProductSlug].Deleted = 0 " +
            "AND [ProductSlug].[Locale] = @Culture " +
            "AND [ProductSlug].ProductID = [Analogue].ID " +
            ") " +
            "LEFT JOIN [ProductImage] " +
            "ON [ProductImage].ProductID = [Analogue].ID " +
            "AND [ProductImage].Deleted = 0 " +
            "WHERE [ProductAnalogue].BaseProductID = @Id " +
            "AND [ProductAnalogue].Deleted = 0 " +
            "AND [Storage].Locale = @Culture " +
            "AND [Storage].ForEcommerce = 1 ";

        _connection.Query(
            analogueExpression,
            analogueTypes,
            analoguesMapper,
            new {
                productToReturn.Id,
                ClientAgreementNetId = clientAgreement.NetUid,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                clientAgreement.Agreement.OrganizationId,
                clientAgreement.Agreement.CurrencyId,
                WithVat = true
            }
        );

        return productToReturn;
    }

    public Product GetByNetId(Guid netId, Guid? clientAgreementNetId = null) {
        Product productToReturn = null;

        ClientAgreement clientAgreement = null;

        if (clientAgreementNetId.HasValue)
            clientAgreement = _connection.Query<ClientAgreement, Agreement, ClientAgreement>(
                "SELECT * FROM [ClientAgreement] " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "WHERE [ClientAgreement].NetUID = @СlientAgreementNetId ",
                (currentClientAgreement, agreement) => {
                    currentClientAgreement.Agreement = agreement;

                    return currentClientAgreement;
                },
                new { СlientAgreementNetId = clientAgreementNetId }
            ).FirstOrDefault();

        string sqlExpression =
            "SELECT [Product].ID " +
            ",[Product].Created " +
            ",[Product].Deleted " +
            ",[Product].HasAnalogue " +
            ",[Product].HasComponent " +
            ",[Product].HasImage " +
            ",[Product].Image " +
            ",[Product].IsForSale " +
            ",[Product].IsForWeb " +
            ",[Product].IsForZeroSale " +
            ",[Product].MainOriginalNumber " +
            ",[Product].MeasureUnitID " +
            ",[Product].NetUID ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ", [Product].[NameUA] AS [Name] ";
            sqlExpression += ", [Product].[DescriptionUA] AS [Description] ";
            sqlExpression += ", [Product].[NotesUA] AS [Notes] ";
        } else {
            sqlExpression += ", [Product].[NameUA] AS [Name] ";
            sqlExpression += ", [Product].[DescriptionUA] AS [Description] ";
            sqlExpression += ", [Product].[NotesUA] AS [Notes] ";
        }

        sqlExpression +=
            ",[Product].[NameUA] " +
            ",[Product].[DescriptionUA] " +
            ",[Product].[NameUA] " +
            ",[Product].[SynonymsUA] " +
            ",[Product].[SynonymsPL] " +
            ",[Product].[DescriptionUA] " +
            ",[Product].OrderStandard " +
            ",[Product].PackingStandard " +
            ",[Product].Standard " +
            ",[Product].Size " +
            ",[Product].[Top] " +
            ",[Product].UCGFEA " +
            ",[Product].Updated " +
            ",[Product].VendorCode " +
            ",[Product].Volume " +
            ",[Product].Weight ";

        sqlExpression += clientAgreement != null
            ? ", dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, null) AS [CurrentPrice] " +
              ", dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, null) AS [CurrentLocalPrice] "
            : "";

        sqlExpression +=
            ",ProductPricing.* " +
            ",ProductProductGroup.* " +
            ",ProductSpecification.* " +
            ",[User].* " +
            ",MeasureUnit.* " +
            ",[ProductImage].* " +
            ",[ProductGroup].* " +
            ",[ProductOriginalNumber].* " +
            ",[OriginalNumber].* " +
            ",[ProductCategory].* " +
            ",[Category].* " +
            "FROM Product " +
            "LEFT JOIN ProductPricing " +
            "ON ProductPricing.ProductID = Product.ID " +
            "AND ProductPricing.Deleted = 0 " +
            "LEFT JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID " +
            "AND ProductProductGroup.Deleted = 0 " +
            "LEFT JOIN [ProductGroup] " +
            "ON [ProductGroup].[ID] = [ProductProductGroup].[ProductGroupID] " +
            "LEFT JOIN ProductSpecification " +
            "ON ProductSpecification.ProductID = Product.ID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = ProductSpecification.AddedById " +
            "LEFT JOIN MeasureUnit " +
            "ON MeasureUnit.ID = Product.MeasureUnitID " +
            "LEFT JOIN [ProductImage] " +
            "ON [ProductImage].ProductID = [Product].ID " +
            "AND [ProductImage].Deleted = 0 " +
            "LEFT JOIN [ProductOriginalNumber] " +
            "ON [ProductOriginalNumber].ProductID = [Product].ID " +
            "AND [ProductOriginalNumber].Deleted = 0 " +
            "LEFT JOIN [OriginalNumber] " +
            "ON [ProductOriginalNumber].OriginalNumberID = [OriginalNumber].ID " +
            "LEFT JOIN [ProductCategory] " +
            "ON [ProductCategory].ProductID = [Product].ID " +
            "AND [ProductCategory].Deleted = 0 " +
            "LEFT JOIN [Category] " +
            "ON [Category].ID = [ProductCategory].CategoryID " +
            "WHERE Product.NetUID = @NetId " +
            "ORDER BY ProductSpecification.Created";

        Type[] types = {
            typeof(Product),
            typeof(ProductPricing),
            typeof(ProductProductGroup),
            typeof(ProductSpecification),
            typeof(User),
            typeof(MeasureUnit),
            typeof(ProductImage),
            typeof(ProductGroup),
            typeof(ProductOriginalNumber),
            typeof(OriginalNumber),
            typeof(ProductCategory),
            typeof(Category)
        };

        var productProps = new {
            NetId = netId,
            ClientAgreementNetId = clientAgreement != null ? clientAgreement.NetUid : Guid.Empty,
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
            WithVat = clientAgreement != null ? clientAgreement.Agreement.WithVATAccounting : false
        };

        Func<object[], Product> mapper = objects => {
            Product product = (Product)objects[0];
            ProductPricing productPricing = (ProductPricing)objects[1];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[2];
            ProductSpecification specification = (ProductSpecification)objects[3];
            User user = (User)objects[4];
            MeasureUnit measureUnit = (MeasureUnit)objects[5];
            ProductImage image = (ProductImage)objects[6];
            ProductGroup productGroup = (ProductGroup)objects[7];
            ProductOriginalNumber productOriginalNumber = (ProductOriginalNumber)objects[8];
            OriginalNumber originalNumber = (OriginalNumber)objects[9];
            ProductCategory productCategory = (ProductCategory)objects[10];
            Category category = (Category)objects[11];

            if (productToReturn != null) {
                if (productPricing != null && !productToReturn.ProductPricings.Any(p => p.Id.Equals(productToReturn.Id))) productToReturn.ProductPricings.Add(productPricing);

                if (productProductGroup != null && !productToReturn.ProductProductGroups.Any(p => p.Id.Equals(productProductGroup.Id))) {
                    productProductGroup.ProductGroup = productGroup;
                    productToReturn.ProductProductGroups.Add(productProductGroup);
                }

                if (productOriginalNumber != null && !productToReturn.ProductOriginalNumbers.Any(n => n.Id.Equals(productOriginalNumber.Id))) {
                    productOriginalNumber.OriginalNumber = originalNumber;

                    productToReturn.ProductOriginalNumbers.Add(productOriginalNumber);
                }

                if (productCategory != null && !productToReturn.ProductCategories.Any(c => c.Id.Equals(productCategory.Id))) {
                    productCategory.Category = category;

                    productToReturn.ProductCategories.Add(productCategory);
                }

                if (specification != null && !productToReturn.ProductSpecifications.Any(s => s.Id.Equals(specification.Id))) {
                    specification.AddedBy = user;

                    productToReturn.ProductSpecifications.Add(specification);
                }

                if (image != null && !productToReturn.ProductImages.Any(i => i.Id.Equals(image.Id))) productToReturn.ProductImages.Add(image);
            } else {
                if (productPricing != null) product.ProductPricings.Add(productPricing);

                if (productProductGroup != null) {
                    productProductGroup.ProductGroup = productGroup;
                    product.ProductProductGroups.Add(productProductGroup);
                }

                if (productOriginalNumber != null && !product.ProductOriginalNumbers.Any(n => n.Id.Equals(productOriginalNumber.Id))) {
                    productOriginalNumber.OriginalNumber = originalNumber;

                    product.ProductOriginalNumbers.Add(productOriginalNumber);
                }

                if (productCategory != null && !product.ProductCategories.Any(c => c.Id.Equals(productCategory.Id))) {
                    productCategory.Category = category;

                    product.ProductCategories.Add(productCategory);
                }

                if (specification != null) {
                    specification.AddedBy = user;

                    product.ProductSpecifications.Add(specification);
                }

                if (image != null) product.ProductImages.Add(image);

                product.MeasureUnit = measureUnit;

                productToReturn = product;
            }

            return product;
        };

        _connection.Query(sqlExpression, types, mapper, productProps);

        if (productToReturn == null) return productToReturn;

        var joinProps = new {
            productToReturn.Id,
            ClientAgreementNetId = clientAgreementNetId ?? Guid.Empty,
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
        };

        string productsExpression =
            "SELECT [Product].ID " +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[Organization].* " +
            ",[ProductPricing].* " +
            ",[Pricing].* " +
            ",[ProductPlacement].* " +
            "FROM [Product] " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [Storage].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [ProductPricing] " +
            "ON [ProductPricing].ProductID = [Product].ID " +
            "AND [ProductPricing].Deleted = 0 " +
            "LEFT JOIN [views].[PricingView] AS [Pricing] " +
            "ON [Pricing].ID = [ProductPricing].PricingID " +
            "AND [Pricing].CultureCode = @Culture " +
            "LEFT JOIN [ProductPlacement] " +
            "ON [ProductPlacement].StorageID = [Storage].ID " +
            "AND [ProductPlacement].ProductID = [Product].ID " +
            "AND [ProductPlacement].PackingListPackageOrderItemID IS NULL " +
            "AND [ProductPlacement].SupplyOrderUkraineItemID IS NULL " +
            "AND [ProductPlacement].Deleted = 0 " +
            "AND [ProductPlacement].Qty != 0 " +
            "WHERE [Product].ID = @Id " +
            "AND ( " +
            "[Storage].ForDefective = 0 " +
            "OR " +
            "(" +
            "[Storage].ForDefective = 1 " +
            "AND " +
            "[ProductAvailability].Amount <> 0" +
            ")" +
            ") " +
            "ORDER BY CASE WHEN [Storage].Locale = @Culture THEN 0 ELSE 1 END " +
            ", [ProductPlacement].Qty DESC;";

        Type[] productsTypes = {
            typeof(Product),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(Organization),
            typeof(ProductPricing),
            typeof(Pricing),
            typeof(ProductPlacement)
        };
        List<ProductPlacement> productPlacements = new();
        Func<object[], Product> productsMapper = objects => {
            Product product = (Product)objects[0];
            ProductAvailability productAvailability = (ProductAvailability)objects[1];
            Storage storage = (Storage)objects[2];
            Organization organization = (Organization)objects[3];
            ProductPricing productPricing = (ProductPricing)objects[4];
            Pricing pricing = (Pricing)objects[5];
            ProductPlacement productPlacement = (ProductPlacement)objects[6];

            // productToReturn.CurrentPrice = currenPrice;
            // productToReturn.CurrentLocalPrice = decimal.Round(currentLocalPrice, 2, MidpointRounding.AwayFromZero);

            if (productPricing != null && !productToReturn.ProductPricings.Any(p => p.Id.Equals(productPricing.Id))) {
                productPricing.Pricing = pricing;

                productToReturn.ProductPricings.Add(productPricing);
            }

            if (productAvailability == null) return product;

            if (!productToReturn.ProductAvailabilities.Any(a => a.Id.Equals(productAvailability.Id))) {
                if (productPlacement != null) storage.ProductPlacements.Add(productPlacement);

                storage.Organization = organization;

                productAvailability.Storage = storage;

                if (!storage.ForDefective) {
                    if (storage.OrganizationId.Equals(organization.Id))
                        if (storage.Locale.ToLower().Equals("pl")) {
                            if (storage.ForVatProducts)
                                productToReturn.AvailableQtyPlVAT += productAvailability.Amount;
                            else
                                productToReturn.AvailableQtyPl += productAvailability.Amount;
                        } else {
                            if (storage.ForVatProducts) {
                                productToReturn.AvailableQtyUkVAT += productAvailability.Amount;

                                if (storage.AvailableForReSale)
                                    productToReturn.AvailableQtyUkReSale += productAvailability.Amount;
                            } else {
                                productToReturn.AvailableQtyUk += productAvailability.Amount;
                            }
                        }
                    else if (storage.AvailableForReSale && !storage.Locale.ToLower().Equals("pl"))
                        productToReturn.AvailableQtyUkReSale += productAvailability.Amount;
                }

                productToReturn.ProductAvailabilities.Add(productAvailability);
            } else if (productPlacement != null) {
                ProductAvailability availabilityFromList = productToReturn.ProductAvailabilities.First(a => a.Id.Equals(productAvailability.Id));
                if (!availabilityFromList.Storage.ProductPlacements.Any(p => p.Id.Equals(productPlacement.Id))) {
                    ProductPlacement placementMerge = availabilityFromList.Storage.ProductPlacements.FirstOrDefault(x =>
                        x.CellNumber == productPlacement.CellNumber && x.RowNumber == productPlacement.RowNumber && x.StorageNumber == productPlacement.StorageNumber);
                    if (!productPlacements.Any(x => x.Id.Equals(productPlacement?.Id)) && placementMerge != null) {
                        placementMerge.Qty += productPlacement.Qty;
                        productPlacements.Add(productPlacement);
                    }

                    if (placementMerge == null) availabilityFromList.Storage.ProductPlacements.Add(productPlacement);
                }
                //if (!availabilityFromList.Storage.ProductPlacements.Any(p => p.Id.Equals(productPlacement.Id))) {
                //    availabilityFromList.Storage.ProductPlacements.Add(productPlacement);
                //}
            }

            return product;
        };

        _connection.Query(
            productsExpression,
            productsTypes,
            productsMapper,
            joinProps
        );

        if (productToReturn == null) return productToReturn;

        ProductIncludesHelper productIncludesHelper = new(_connection);

        if (productToReturn.HasAnalogue) productIncludesHelper.IncludeAnaloguesForProduct(productToReturn);

        if (productToReturn.HasComponent) productIncludesHelper.IncludeComponentsForProduct(productToReturn, withAnalogues: true);

        bool isComponent = _connection.Query<bool>(
            "SELECT TOP 1 " +
            "CASE WHEN ProductSet.ID IS NULL THEN 0 " +
            "ELSE 1 " +
            "END " +
            "FROM ProductSet " +
            "WHERE ComponentProductID = @ProductId",
            new { ProductId = productToReturn.Id }
        ).FirstOrDefault();

        if (isComponent) productIncludesHelper.IncludeProductSetForProduct(productToReturn);

        string incomesSqlExpression =
            "SELECT * " +
            "FROM [ProductIncome] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ProductIncome].UserID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductIncome].StorageID " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [ProductIncomeItem].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [PackingListPackage] " +
            "ON [PackingListPackage].ID = [PackingListPackageOrderItem].PackingListPackageID " +
            "LEFT JOIN [PackingList] AS [PackagePackingList] " +
            "ON [PackagePackingList].ID = [PackingListPackage].PackingListID " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [SupplyInvoiceOrderItem].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "WHERE [SupplyInvoiceOrderItem].ProductID = @Id " +
            "AND [ProductIncome].Deleted = 0 " +
            "ORDER BY [ProductIncomeItem].RemainingQty DESC";

        Type[] incomesTypes = {
            typeof(ProductIncome),
            typeof(User),
            typeof(Storage),
            typeof(ProductIncomeItem),
            typeof(PackingListPackageOrderItem),
            typeof(PackingList),
            typeof(PackingListPackage),
            typeof(PackingList),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyInvoice),
            typeof(SupplyOrderItem),
            typeof(SupplyOrder),
            typeof(Client),
            typeof(SupplyOrderNumber)
        };

        Func<object[], ProductIncome> incomesMapper = objects => {
            ProductIncome productIncome = (ProductIncome)objects[0];
            User user = (User)objects[1];
            Storage storage = (Storage)objects[2];
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[3];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[4];
            PackingList packingList = (PackingList)objects[5];
            PackingListPackage package = (PackingListPackage)objects[6];
            PackingList packagePackingList = (PackingList)objects[7];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[8];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[9];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[10];
            SupplyOrder supplyOrder = (SupplyOrder)objects[11];
            Client client = (Client)objects[12];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[13];

            if (!productToReturn.ProductIncomes.Any(i => i.Id.Equals(productIncome.Id))) {
                if (package != null) package.PackingList = packagePackingList;

                if (supplyOrder != null) {
                    supplyOrder.Client = client;
                    supplyOrder.SupplyOrderNumber = supplyOrderNumber;
                }

                if (supplyOrderItem != null)
                    supplyOrderItem.SupplyOrder = supplyOrder;

                supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                supplyInvoiceOrderItem.SupplyInvoice = supplyInvoice;

                packingListPackageOrderItem.PackingList = packingList;
                packingListPackageOrderItem.PackingListPackage = package;
                packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                productIncomeItem.PackingListPackageOrderItem = packingListPackageOrderItem;

                productIncome.ProductIncomeItems.Add(productIncomeItem);

                productIncome.User = user;
                productIncome.Storage = storage;

                productToReturn.ProductIncomes.Add(productIncome);
            } else {
                ProductIncome fromList = productToReturn.ProductIncomes.First(i => i.Id.Equals(productIncome.Id));

                if (fromList.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncome;

                if (package != null) package.PackingList = packagePackingList;

                if (supplyOrder != null) {
                    supplyOrder.Client = client;
                    supplyOrder.SupplyOrderNumber = supplyOrderNumber;
                }

                if (supplyOrderItem != null)
                    supplyOrderItem.SupplyOrder = supplyOrder;

                supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                supplyInvoiceOrderItem.SupplyInvoice = supplyInvoice;

                packingListPackageOrderItem.PackingList = packingList;
                packingListPackageOrderItem.PackingListPackage = package;
                packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                productIncomeItem.PackingListPackageOrderItem = packingListPackageOrderItem;

                fromList.ProductIncomeItems.Add(productIncomeItem);
            }

            return productIncome;
        };

        _connection.Query(
            incomesSqlExpression,
            incomesTypes,
            incomesMapper,
            new {
                productToReturn.Id,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );
        Type[] incomesTypesConsigmentItem = {
            typeof(ConsignmentItem),
            typeof(Consignment),
            typeof(ProductIncome)
        };

        Func<object[], ProductIncome> incomesMapperConsignmentItem = objects => {
            ConsignmentItem consignmentItem = (ConsignmentItem)objects[0];
            Consignment consignment = (Consignment)objects[1];
            ProductIncome productIncome = (ProductIncome)objects[2];
            List<ProductPlacement> productPlacement = productToReturn.ProductAvailabilities
                .SelectMany(x => x.Storage.ProductPlacements.Where(y => y.ConsignmentItemId == consignmentItem.Id)).ToList();

            foreach (ProductPlacement placement in productPlacement) {
                consignment.ProductIncome = productIncome;
                consignmentItem.Consignment = consignment;
                placement.ConsignmentItem = consignmentItem;
            }

            return productIncome;
        };
        List<long?> idsConsignmentItem = productToReturn.ProductAvailabilities.SelectMany(x => x.Storage.ProductPlacements.Select(y => y.ConsignmentItemId))
            .Where(id => id.HasValue).ToList();
        _connection.Query(
            "SELECT DISTINCT * FROM ConsignmentItem " +
            "LEFT JOIN Consignment " +
            "ON Consignment.ID = ConsignmentItem.ConsignmentID " +
            "LEFT JOIN ProductIncome " +
            "ON ProductIncome.ID = Consignment.ProductIncomeID " +
            "WHERE ConsignmentItem.ID IN @ids ",
            incomesTypesConsigmentItem,
            incomesMapperConsignmentItem,
            new {
                ids = idsConsignmentItem
            }
        );

        return productToReturn;
    }

    public Product GetBySlug(string slug, Guid? clientAgreementNetId = null) {
        Product productToReturn = null;

        string sqlExpression =
            "SELECT [Product].ID " +
            ",[Product].Created " +
            ",[Product].Deleted " +
            ",[Product].HasAnalogue " +
            ",[Product].HasComponent " +
            ",[Product].HasImage " +
            ",[Product].Image " +
            ",[Product].IsForSale " +
            ",[Product].IsForWeb " +
            ",[Product].IsForZeroSale " +
            ",[Product].MainOriginalNumber " +
            ",[Product].MeasureUnitID " +
            ",[Product].NetUID ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ", [Product].[NameUA] AS [Name] ";
            sqlExpression += ", [Product].[DescriptionUA] AS [Description] ";
            sqlExpression += ", [Product].[NotesUA] AS [Notes] ";
        } else {
            sqlExpression += ", [Product].[NameUA] AS [Name] ";
            sqlExpression += ", [Product].[DescriptionUA] AS [Description] ";
            sqlExpression += ", [Product].[NotesUA] AS [Notes] ";
        }

        sqlExpression +=
            ",[Product].[NameUA] " +
            ",[Product].[DescriptionUA] " +
            ",[Product].[NameUA] " +
            ",[Product].[SynonymsUA] " +
            ",[Product].[SynonymsPL] " +
            ",[Product].[DescriptionUA] " +
            ",[Product].OrderStandard " +
            ",[Product].PackingStandard " +
            ",[Product].Standard " +
            ",[Product].Size " +
            ",[Product].[Top] " +
            ",[Product].UCGFEA " +
            ",[Product].Updated " +
            ",[Product].VendorCode " +
            ",[Product].Volume " +
            ",[Product].Weight " +
            ",ProductPricing.* " +
            ",ProductProductGroup.* " +
            ",ProductSpecification.* " +
            ",[User].* " +
            ",MeasureUnit.* " +
            ",[ProductImage].* " +
            ",[ProductSlug].* " +
            "FROM Product " +
            "LEFT JOIN ProductPricing " +
            "ON ProductPricing.ProductID = Product.ID " +
            "AND ProductPricing.Deleted = 0 " +
            "LEFT JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID " +
            "AND ProductProductGroup.Deleted = 0 " +
            "LEFT JOIN ProductSpecification " +
            "ON ProductSpecification.ProductID = Product.ID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = ProductSpecification.AddedById " +
            "LEFT JOIN MeasureUnit " +
            "ON MeasureUnit.ID = Product.MeasureUnitID " +
            "LEFT JOIN [ProductImage] " +
            "ON [ProductImage].ProductID = [Product].ID " +
            "AND [ProductImage].Deleted = 0 " +
            "LEFT JOIN [ProductSlug] " +
            "ON [ProductSlug].ID = (" +
            "SELECT TOP(1) ID " +
            "FROM [ProductSlug] " +
            "WHERE [ProductSlug].Deleted = 0 " +
            "AND [ProductSlug].[Locale] = @Culture " +
            "AND [ProductSlug].ProductID = [Product].ID" +
            ") " +
            "WHERE Product.ID = (" +
            "SELECT TOP(1) [ProductSlug].ProductID " +
            "FROM [ProductSlug] " +
            "WHERE [ProductSlug].Deleted = 0 " +
            "AND [ProductSlug].[Locale] = @Culture " +
            "AND [ProductSlug].[Url] = @Slug " +
            (
                CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")
                    ? "AND [ProductSlug].Locale = N'pl'"
                    : "AND [ProductSlug].Locale = N'uk'"
            ) +
            ") " +
            "ORDER BY ProductSpecification.Created";

        Type[] types = {
            typeof(Product),
            typeof(ProductPricing),
            typeof(ProductProductGroup),
            typeof(ProductSpecification),
            typeof(User),
            typeof(MeasureUnit),
            typeof(ProductImage),
            typeof(ProductSlug)
        };

        Func<object[], Product> mapper = objects => {
            Product product = (Product)objects[0];
            ProductPricing productPricing = (ProductPricing)objects[1];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[2];
            ProductSpecification specification = (ProductSpecification)objects[3];
            User user = (User)objects[4];
            MeasureUnit measureUnit = (MeasureUnit)objects[5];
            ProductImage image = (ProductImage)objects[6];
            ProductSlug productSlug = (ProductSlug)objects[7];

            if (productToReturn != null) {
                if (productPricing != null && !productToReturn.ProductPricings.Any(p => p.Id.Equals(productToReturn.Id))) productToReturn.ProductPricings.Add(productPricing);

                if (productPricing != null && !productToReturn.ProductProductGroups.Any(p => p.Id.Equals(productProductGroup.Id)))
                    productToReturn.ProductProductGroups.Add(productProductGroup);

                if (specification != null && !productToReturn.ProductSpecifications.Any(s => s.Id.Equals(specification.Id))) {
                    specification.AddedBy = user;

                    productToReturn.ProductSpecifications.Add(specification);
                }

                if (image != null && !productToReturn.ProductImages.Any(i => i.Id.Equals(image.Id))) productToReturn.ProductImages.Add(image);
            } else {
                if (productPricing != null) product.ProductPricings.Add(productPricing);

                if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

                if (specification != null) {
                    specification.AddedBy = user;

                    product.ProductSpecifications.Add(specification);
                }

                if (image != null) product.ProductImages.Add(image);

                product.MeasureUnit = measureUnit;
                product.ProductSlug = productSlug;

                productToReturn = product;
            }

            return product;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new { Slug = slug, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        if (productToReturn == null) return productToReturn;

        bool withVat = clientAgreementNetId.HasValue && _connection.Query<bool>(
            "SELECT [Agreement].[WithVATAccounting] FROM [ClientAgreement] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "WHERE [ClientAgreement].[NetUID] = @NetId; ",
            new { NetId = clientAgreementNetId.Value }).FirstOrDefault();

        var joinProps = new {
            productToReturn.Id,
            ClientAgreementNetId = clientAgreementNetId ?? Guid.Empty,
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
            WithVat = withVat
        };

        string productsExpression =
            "SELECT [Product].ID " +
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
            ",[ProductPlacement].* " +
            ",dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS ProductCurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS ProductCurrentLocalPrice " +
            "FROM [Product] " +
            "LEFT JOIN [ProductOriginalNumber] " +
            "ON [ProductOriginalNumber].ProductID = [Product].ID " +
            "AND [ProductOriginalNumber].Deleted = 0 " +
            "LEFT JOIN [OriginalNumber] " +
            "ON [ProductOriginalNumber].OriginalNumberID = [OriginalNumber].ID " +
            "LEFT JOIN [ProductProductGroup] " +
            "ON [ProductProductGroup].ProductID = [Product].ID " +
            "AND [ProductProductGroup].Deleted = 0 " +
            "LEFT JOIN [ProductGroup] " +
            "ON [ProductGroup].ID = [ProductProductGroup].ProductGroupID " +
            "LEFT JOIN [ProductCategory] " +
            "ON [ProductCategory].ProductID = [Product].ID " +
            "AND [ProductCategory].Deleted = 0 " +
            "LEFT JOIN [Category] " +
            "ON [Category].ID = [ProductCategory].CategoryID " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "LEFT JOIN [ProductPricing] " +
            "ON [ProductPricing].ProductID = [Product].ID " +
            "AND [ProductPricing].Deleted = 0 " +
            "LEFT JOIN [views].[PricingView] AS [Pricing] " +
            "ON [Pricing].ID = [ProductPricing].PricingID " +
            "AND [Pricing].CultureCode = @Culture " +
            "LEFT JOIN [ProductPlacement] " +
            "ON [ProductPlacement].StorageID = [Storage].ID " +
            "AND [ProductPlacement].ProductID = [Product].ID " +
            "AND [ProductPlacement].PackingListPackageOrderItemID IS NULL " +
            "AND [ProductPlacement].SupplyOrderUkraineItemID IS NULL " +
            "AND [ProductPlacement].Deleted = 0 " +
            "WHERE [Product].ID = @Id " +
            "AND ( " +
            "[Storage].ForDefective = 0 " +
            "OR " +
            "(" +
            "[Storage].ForDefective = 1 " +
            "AND " +
            "[ProductAvailability].Amount <> 0" +
            ")" +
            ") " +
            "ORDER BY CASE WHEN [Storage].Locale = @Culture THEN 0 ELSE 1 END";

        Type[] productsTypes = {
            typeof(Product),
            typeof(ProductOriginalNumber),
            typeof(OriginalNumber),
            typeof(ProductProductGroup),
            typeof(ProductGroup),
            typeof(ProductCategory),
            typeof(Category),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(ProductPricing),
            typeof(Pricing),
            typeof(ProductPlacement),
            typeof(decimal),
            typeof(decimal)
        };

        Func<object[], Product> productsMapper = objects => {
            Product product = (Product)objects[0];
            ProductOriginalNumber productOriginalNumber = (ProductOriginalNumber)objects[1];
            OriginalNumber originalNumber = (OriginalNumber)objects[2];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[3];
            ProductGroup productGroup = (ProductGroup)objects[4];
            ProductCategory productCategory = (ProductCategory)objects[5];
            Category category = (Category)objects[6];
            ProductAvailability productAvailability = (ProductAvailability)objects[7];
            Storage storage = (Storage)objects[8];
            ProductPricing productPricing = (ProductPricing)objects[9];
            Pricing pricing = (Pricing)objects[10];
            ProductPlacement productPlacement = (ProductPlacement)objects[11];
            decimal currentPrice = (decimal)objects[12];
            decimal currentLocalPrice = (decimal)objects[13];

            productToReturn.CurrentPrice = currentPrice;
            productToReturn.CurrentLocalPrice = decimal.Round(currentLocalPrice, 2, MidpointRounding.AwayFromZero);

            if (productOriginalNumber != null && !productToReturn.ProductOriginalNumbers.Any(n => n.Id.Equals(productOriginalNumber.Id))) {
                productOriginalNumber.OriginalNumber = originalNumber;

                productToReturn.ProductOriginalNumbers.Add(productOriginalNumber);
            }

            if (productProductGroup != null && !productToReturn.ProductProductGroups.Any(g => g.Id.Equals(productProductGroup.Id))) {
                productProductGroup.ProductGroup = productGroup;

                productToReturn.ProductProductGroups.Add(productProductGroup);
            }

            if (productCategory != null && !productToReturn.ProductCategories.Any(c => c.Id.Equals(productCategory.Id))) {
                productCategory.Category = category;

                productToReturn.ProductCategories.Add(productCategory);
            }

            if (productPricing != null && !productToReturn.ProductPricings.Any(p => p.Id.Equals(productPricing.Id))) {
                productPricing.Pricing = pricing;

                productToReturn.ProductPricings.Add(productPricing);
            }

            if (productAvailability == null) return product;

            if (!productToReturn.ProductAvailabilities.Any(a => a.Id.Equals(productAvailability.Id))) {
                if (productPlacement != null) storage.ProductPlacements.Add(productPlacement);

                productAvailability.Storage = storage;

                if (!storage.ForDefective) {
                    if (storage.Locale.ToLower().Equals("pl")) {
                        if (storage.ForVatProducts)
                            productToReturn.AvailableQtyPlVAT += productAvailability.Amount;
                        else
                            productToReturn.AvailableQtyPl += productAvailability.Amount;
                    } else {
                        if (storage.ForVatProducts) {
                            productToReturn.AvailableQtyUkVAT += productAvailability.Amount;

                            if (storage.AvailableForReSale)
                                productToReturn.AvailableQtyUkReSale += productAvailability.Amount;
                        } else {
                            productToReturn.AvailableQtyUk += productAvailability.Amount;
                        }
                    }
                }

                productToReturn.ProductAvailabilities.Add(productAvailability);
            } else if (productPlacement != null) {
                ProductAvailability availabilityFromList = productToReturn.ProductAvailabilities.First(a => a.Id.Equals(productAvailability.Id));

                if (!availabilityFromList.Storage.ProductPlacements.Any(p => p.Id.Equals(productPlacement.Id)))
                    availabilityFromList.Storage.ProductPlacements.Add(productPlacement);
            }

            return product;
        };

        _connection.Query(
            productsExpression,
            productsTypes,
            productsMapper,
            joinProps,
            splitOn: "ID,ProductCurrentPrice,ProductCurrentLocalPrice"
        );

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
            analoguesExpression += ",[Analogue].[NameUA] AS [Name] ";
            analoguesExpression += ",[Analogue].[DescriptionUA] AS [Description] ";
        } else {
            analoguesExpression += ",[Analogue].[NameUA] AS [Name] ";
            analoguesExpression += ",[Analogue].[DescriptionUA] AS [Description] ";
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
            ",[Analogue].Weight " +
            ",dbo.GetCalculatedProductPriceWithSharesAndVat([Analogue].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Analogue].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentLocalPrice " +
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
            ",[ProductSlug].* " +
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
            "LEFT JOIN [ProductSlug] " +
            "ON [ProductSlug].ID = (" +
            "SELECT TOP(1) ID " +
            "FROM [ProductSlug] " +
            "WHERE [ProductSlug].Deleted = 0 " +
            "AND [ProductSlug].[Locale] = @Culture " +
            "AND [ProductSlug].ProductID = [Analogue].ID" +
            ") " +
            "WHERE [Product].ID = @Id " +
            "AND [Analogue].ID IS NOT NULL " +
            "AND [Storage].ForDefective = 0 " +
            "ORDER BY CASE WHEN [Storage].Locale = @Culture THEN 0 ELSE 1 END";

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
            typeof(Pricing),
            typeof(ProductSlug)
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
            ProductSlug productSlug = (ProductSlug)objects[14];

            if (analogue == null) return product;

            analogue.ProductSlug = productSlug;

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
                                    analogue.AvailableQtyUk += productAvailability.Amount;
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
                                    analogueFromList.AnalogueProduct.AvailableQtyUk += productAvailability.Amount;
                            } else {
                                analogueFromList.AnalogueProduct.AvailableQtyUk += productAvailability.Amount;
                            }
                        }
                    }

                    analogueFromList.AnalogueProduct.ProductAvailabilities.Add(productAvailability);
                }

                if (productPricing == null || analogueFromList.AnalogueProduct.ProductPricings.Any(p => p.Id.Equals(productPricing.Id))) return product;

                productPricing.Pricing = pricing;

                analogueFromList.AnalogueProduct.ProductPricings.Add(productPricing);
            }

            return product;
        };

        _connection.Query(
            analoguesExpression,
            analoguesTypes,
            analoguesMapper,
            joinProps
        );

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
            componentsExpression += ", [Component].[NameUA] AS [Name] ";
            componentsExpression += ", [Component].[DescriptionUA] AS [Description] ";
            componentsExpression += ", [Component].[NotesUA] AS [Notes] ";
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
            ",[Component].Weight " +
            ",dbo.GetCalculatedProductPriceWithSharesAndVat([Component].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Component].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentLocalPrice " +
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
            ",[ProductSlug].* " +
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
            "LEFT JOIN [ProductSlug] " +
            "ON [ProductSlug].ID = (" +
            "SELECT TOP(1) ID " +
            "FROM [ProductSlug] " +
            "WHERE [ProductSlug].Deleted = 0 " +
            "AND [ProductSlug].[Locale] = @Culture " +
            "AND [ProductSlug].ProductID = [Component].ID" +
            ") " +
            "WHERE [Product].ID = @Id " +
            "AND [Component].ID IS NOT NULL " +
            "AND [Storage].ForDefective = 0 " +
            "ORDER BY CASE WHEN [Storage].Locale = @Culture THEN 0 ELSE 1 END";

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
            typeof(Pricing),
            typeof(ProductSlug)
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
            ProductSlug productSlug = (ProductSlug)objects[14];

            if (component == null) return product;

            component.ProductSlug = productSlug;

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
                                    component.AvailableQtyUk += productAvailability.Amount;
                            } else {
                                component.AvailableQtyUk += productAvailability.Amount;
                            }
                        }
                    }

                    component.ProductAvailabilities.Add(productAvailability);
                }

                if (productPricing != null) {
                    productPricing.Pricing = pricing;

                    component.ProductPricings.Add(productPricing);
                }

                component.MeasureUnit = measureUnit;

                component.CurrentLocalPrice = decimal.Round(component.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

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
                                    setFromList.ComponentProduct.AvailableQtyUk += productAvailability.Amount;
                            } else {
                                setFromList.ComponentProduct.AvailableQtyUk += productAvailability.Amount;
                            }
                        }
                    }

                    setFromList.ComponentProduct.ProductAvailabilities.Add(productAvailability);
                }

                if (productPricing == null || setFromList.ComponentProduct.ProductPricings.Any(p => p.Id.Equals(productPricing.Id))) return product;

                productPricing.Pricing = pricing;

                setFromList.ComponentProduct.ProductPricings.Add(productPricing);
            }

            return product;
        };

        _connection.Query(
            componentsExpression,
            componentsTypes,
            componentsMapper,
            joinProps
        );

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
            componentAnaloguesExpression += ",[Analogue].[NameUA] AS [Name] ";
            componentAnaloguesExpression += ",[Analogue].[DescriptionUA] AS [Description] ";
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
            ",dbo.GetCalculatedProductPriceWithSharesAndVat([Analogue].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Analogue].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentLocalPrice " +
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
            ",[ProductSlug].* " +
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
            "LEFT JOIN [ProductSlug] " +
            "ON [ProductSlug].ID = (" +
            "SELECT TOP(1) ID " +
            "FROM [ProductSlug] " +
            "WHERE [ProductSlug].Deleted = 0 " +
            "AND [ProductSlug].[Locale] = @Culture " +
            "AND [ProductSlug].ProductID = [Analogue].ID" +
            ") " +
            "WHERE [Product].ID = @Id " +
            "AND [Analogue].ID IS NOT NULL " +
            "AND [Storage].ForDefective = 0 " +
            "ORDER BY CASE WHEN [Storage].Locale = @Culture THEN 0 ELSE 1 END";

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
            typeof(Pricing),
            typeof(ProductSlug)
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
            ProductSlug productSlug = (ProductSlug)objects[15];

            if (productSet == null) return product;

            ProductSet component = productToReturn.ComponentProducts.First(c => c.Id.Equals(productSet.Id));

            if (analogue == null) return product;

            analogue.ProductSlug = productSlug;

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
                                    analogue.AvailableQtyUk += productAvailability.Amount;
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
                                    analogueFromList.AnalogueProduct.AvailableQtyUk += productAvailability.Amount;
                            } else {
                                analogueFromList.AnalogueProduct.AvailableQtyUk += productAvailability.Amount;
                            }
                        }
                    }

                    analogueFromList.AnalogueProduct.ProductAvailabilities.Add(productAvailability);
                }

                if (productPricing == null || analogueFromList.AnalogueProduct.ProductPricings.Any(p => p.Id.Equals(productPricing.Id))) return product;

                productPricing.Pricing = pricing;

                analogueFromList.AnalogueProduct.ProductPricings.Add(productPricing);
            }

            return product;
        };

        _connection.Query(
            componentAnaloguesExpression,
            componentAnaloguesTypes,
            componentAnaloguesMapper,
            joinProps
        );

        string incomesSqlExpression =
            "SELECT * " +
            "FROM [ProductIncome] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ProductIncome].UserID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductIncome].StorageID " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [ProductIncomeItem].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [PackingListPackage] " +
            "ON [PackingListPackage].ID = [PackingListPackageOrderItem].PackingListPackageID " +
            "LEFT JOIN [PackingList] AS [PackagePackingList] " +
            "ON [PackagePackingList].ID = [PackingListPackage].PackingListID " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [SupplyInvoiceOrderItem].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "WHERE [SupplyInvoiceOrderItem].ProductID = @Id " +
            "AND [ProductIncome].Deleted = 0 " +
            "ORDER BY [ProductIncome].FromDate DESC";

        Type[] incomesTypes = {
            typeof(ProductIncome),
            typeof(User),
            typeof(Storage),
            typeof(ProductIncomeItem),
            typeof(PackingListPackageOrderItem),
            typeof(PackingList),
            typeof(PackingListPackage),
            typeof(PackingList),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyInvoice),
            typeof(SupplyOrderItem),
            typeof(SupplyOrder),
            typeof(Client),
            typeof(SupplyOrderNumber)
        };

        Func<object[], ProductIncome> incomesMapper = objects => {
            ProductIncome productIncome = (ProductIncome)objects[0];
            User user = (User)objects[1];
            Storage storage = (Storage)objects[2];
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[3];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[4];
            PackingList packingList = (PackingList)objects[5];
            PackingListPackage package = (PackingListPackage)objects[6];
            PackingList packagePackingList = (PackingList)objects[7];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[8];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[9];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[10];
            SupplyOrder supplyOrder = (SupplyOrder)objects[11];
            Client client = (Client)objects[12];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[13];

            if (!productToReturn.ProductIncomes.Any(i => i.Id.Equals(productIncome.Id))) {
                if (package != null) package.PackingList = packagePackingList;

                if (supplyOrder != null) {
                    supplyOrder.Client = client;
                    supplyOrder.SupplyOrderNumber = supplyOrderNumber;
                }

                if (supplyOrderItem != null)
                    supplyOrderItem.SupplyOrder = supplyOrder;

                supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                supplyInvoiceOrderItem.SupplyInvoice = supplyInvoice;

                packingListPackageOrderItem.PackingList = packingList;
                packingListPackageOrderItem.PackingListPackage = package;
                packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                productIncomeItem.PackingListPackageOrderItem = packingListPackageOrderItem;

                productIncome.ProductIncomeItems.Add(productIncomeItem);

                productIncome.User = user;
                productIncome.Storage = storage;

                productToReturn.ProductIncomes.Add(productIncome);
            } else {
                ProductIncome fromList = productToReturn.ProductIncomes.First(i => i.Id.Equals(productIncome.Id));

                if (fromList.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncome;

                if (package != null) package.PackingList = packagePackingList;

                if (supplyOrder != null) {
                    supplyOrder.Client = client;
                    supplyOrder.SupplyOrderNumber = supplyOrderNumber;
                }

                if (supplyOrderItem != null)
                    supplyOrderItem.SupplyOrder = supplyOrder;

                supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                supplyInvoiceOrderItem.SupplyInvoice = supplyInvoice;

                packingListPackageOrderItem.PackingList = packingList;
                packingListPackageOrderItem.PackingListPackage = package;
                packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                productIncomeItem.PackingListPackageOrderItem = packingListPackageOrderItem;

                fromList.ProductIncomeItems.Add(productIncomeItem);
            }

            return productIncome;
        };

        _connection.Query(
            incomesSqlExpression,
            incomesTypes,
            incomesMapper,
            new {
                productToReturn.Id,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        return productToReturn;
    }

    public Product GetByNetId(Guid netId, Guid nonVatAgreementNetId, Guid? vatAgreementNetId) {
        Product productToReturn = null;

        string sqlExpression =
            "SELECT [Product].ID " +
            ",[Product].Created " +
            ",[Product].Deleted " +
            ",[Product].HasAnalogue " +
            ",[Product].HasComponent " +
            ",[Product].HasImage " +
            ",[Product].Image " +
            ",[Product].IsForSale " +
            ",[Product].IsForWeb " +
            ",[Product].IsForZeroSale " +
            ",[Product].MainOriginalNumber " +
            ",[Product].MeasureUnitID " +
            ",[Product].NetUID ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ", [Product].[NameUA] AS [Name] ";
            sqlExpression += ", [Product].[DescriptionUA] AS [Description] ";
            sqlExpression += ", [Product].[NotesUA] AS [Notes] ";
        } else {
            sqlExpression += ", [Product].[NameUA] AS [Name] ";
            sqlExpression += ", [Product].[DescriptionUA] AS [Description] ";
            sqlExpression += ", [Product].[NotesUA] AS [Notes] ";
        }

        sqlExpression +=
            ",[Product].[NameUA] " +
            ",[Product].[DescriptionUA] " +
            ",[Product].[NameUA] " +
            ",[Product].[SynonymsUA] " +
            ",[Product].[SynonymsPL] " +
            ",[Product].[DescriptionUA] " +
            ",[Product].OrderStandard " +
            ",[Product].PackingStandard " +
            ",[Product].Standard " +
            ",[Product].Size " +
            ",[Product].[Top] " +
            ",[Product].UCGFEA " +
            ",[Product].Updated " +
            ",[Product].VendorCode " +
            ",[Product].Volume " +
            ",[Product].Weight " +
            ",ProductPricing.* " +
            ",ProductProductGroup.* " +
            ",ProductSpecification.* " +
            ",[User].* " +
            ",MeasureUnit.* " +
            ",[ProductImage].* " +
            "FROM Product " +
            "LEFT JOIN ProductPricing " +
            "ON ProductPricing.ProductID = Product.ID " +
            "AND ProductPricing.Deleted = 0 " +
            "LEFT JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID " +
            "AND ProductProductGroup.Deleted = 0 " +
            "LEFT JOIN ProductSpecification " +
            "ON ProductSpecification.ProductID = Product.ID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = ProductSpecification.AddedById " +
            "LEFT JOIN MeasureUnit " +
            "ON MeasureUnit.ID = Product.MeasureUnitID " +
            "LEFT JOIN [ProductImage] " +
            "ON [ProductImage].ProductID = [Product].ID " +
            "AND [ProductImage].Deleted = 0 " +
            "WHERE Product.NetUID = @NetId " +
            "ORDER BY ProductSpecification.Created";

        _connection.Query<Product, ProductPricing, ProductProductGroup, ProductSpecification, User, MeasureUnit, ProductImage, Product>(
            sqlExpression,
            (product, productPricing, productProductGroup, specification, user, measureUnit, image) => {
                if (productToReturn != null) {
                    if (productPricing != null && !productToReturn.ProductPricings.Any(p => p.Id.Equals(productToReturn.Id))) productToReturn.ProductPricings.Add(productPricing);

                    if (productPricing != null && !productToReturn.ProductProductGroups.Any(p => p.Id.Equals(productProductGroup.Id)))
                        productToReturn.ProductProductGroups.Add(productProductGroup);

                    if (specification != null && !productToReturn.ProductSpecifications.Any(s => s.Id.Equals(specification.Id))) {
                        specification.AddedBy = user;

                        productToReturn.ProductSpecifications.Add(specification);
                    }

                    if (image != null && !productToReturn.ProductImages.Any(i => i.Id.Equals(image.Id))) productToReturn.ProductImages.Add(image);
                } else {
                    if (productPricing != null) product.ProductPricings.Add(productPricing);

                    if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

                    if (specification != null) {
                        specification.AddedBy = user;

                        product.ProductSpecifications.Add(specification);
                    }

                    if (image != null) product.ProductImages.Add(image);

                    product.MeasureUnit = measureUnit;

                    productToReturn = product;
                }

                return product;
            },
            new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        if (productToReturn == null) return productToReturn;

        var joinProps = new {
            productToReturn.Id,
            NonVatAgreementNetId = nonVatAgreementNetId,
            VatAgreementNetId = vatAgreementNetId ?? Guid.Empty,
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
        };

        string productsExpression =
            "SELECT [Product].ID " +
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
            ",[ProductPlacement].* " +
            ",dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS ProductCurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS ProductCurrentLocalPrice " +
            ",dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS ProductCurrentPriceVat " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS ProductCurrentLocalPriceVat " +
            "FROM [Product] " +
            "LEFT JOIN [ProductOriginalNumber] " +
            "ON [ProductOriginalNumber].ProductID = [Product].ID " +
            "AND [ProductOriginalNumber].Deleted = 0 " +
            "LEFT JOIN [OriginalNumber] " +
            "ON [ProductOriginalNumber].OriginalNumberID = [OriginalNumber].ID " +
            "LEFT JOIN [ProductProductGroup] " +
            "ON [ProductProductGroup].ProductID = [Product].ID " +
            "AND [ProductProductGroup].Deleted = 0 " +
            "LEFT JOIN [ProductGroup] " +
            "ON [ProductGroup].ID = [ProductProductGroup].ProductGroupID " +
            "LEFT JOIN [ProductCategory] " +
            "ON [ProductCategory].ProductID = [Product].ID " +
            "AND [ProductCategory].Deleted = 0 " +
            "LEFT JOIN [Category] " +
            "ON [Category].ID = [ProductCategory].CategoryID " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "LEFT JOIN [ProductPricing] " +
            "ON [ProductPricing].ProductID = [Product].ID " +
            "AND [ProductPricing].Deleted = 0 " +
            "LEFT JOIN [views].[PricingView] AS [Pricing] " +
            "ON [Pricing].ID = [ProductPricing].PricingID " +
            "AND [Pricing].CultureCode = @Culture " +
            "LEFT JOIN [ProductPlacement] " +
            "ON [ProductPlacement].StorageID = [Storage].ID " +
            "AND [ProductPlacement].ProductID = [Product].ID " +
            "AND [ProductPlacement].PackingListPackageOrderItemID IS NULL " +
            "AND [ProductPlacement].SupplyOrderUkraineItemID IS NULL " +
            "AND [ProductPlacement].Deleted = 0 " +
            "WHERE [Product].ID = @Id " +
            "AND [Storage].ForDefective = 0 " +
            "ORDER BY CASE WHEN [Storage].Locale = @Culture THEN 0 ELSE 1 END";

        Type[] productsTypes = {
            typeof(Product),
            typeof(ProductOriginalNumber),
            typeof(OriginalNumber),
            typeof(ProductProductGroup),
            typeof(ProductGroup),
            typeof(ProductCategory),
            typeof(Category),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(ProductPricing),
            typeof(Pricing),
            typeof(ProductPlacement),
            typeof(decimal),
            typeof(decimal),
            typeof(decimal),
            typeof(decimal)
        };

        Func<object[], Product> productsMapper = objects => {
            Product product = (Product)objects[0];
            ProductOriginalNumber productOriginalNumber = (ProductOriginalNumber)objects[1];
            OriginalNumber originalNumber = (OriginalNumber)objects[2];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[3];
            ProductGroup productGroup = (ProductGroup)objects[4];
            ProductCategory productCategory = (ProductCategory)objects[5];
            Category category = (Category)objects[6];
            ProductAvailability productAvailability = (ProductAvailability)objects[7];
            Storage storage = (Storage)objects[8];
            ProductPricing productPricing = (ProductPricing)objects[9];
            Pricing pricing = (Pricing)objects[10];
            ProductPlacement productPlacement = (ProductPlacement)objects[11];
            decimal currentPrice = (decimal)objects[12];
            decimal currentLocalPrice = (decimal)objects[13];
            decimal currentPriceVat = (decimal)objects[14];
            decimal currentLocalPriceVat = (decimal)objects[15];

            productToReturn.CurrentPrice = currentPrice;
            productToReturn.CurrentLocalPrice = decimal.Round(currentLocalPrice, 2, MidpointRounding.AwayFromZero);
            productToReturn.CurrentWithVatPrice = currentPriceVat;
            productToReturn.CurrentLocalWithVatPrice = decimal.Round(currentLocalPriceVat, 2, MidpointRounding.AwayFromZero);

            if (productOriginalNumber != null && !productToReturn.ProductOriginalNumbers.Any(n => n.Id.Equals(productOriginalNumber.Id))) {
                productOriginalNumber.OriginalNumber = originalNumber;

                productToReturn.ProductOriginalNumbers.Add(productOriginalNumber);
            }

            if (productProductGroup != null && !productToReturn.ProductProductGroups.Any(g => g.Id.Equals(productProductGroup.Id))) {
                productProductGroup.ProductGroup = productGroup;

                productToReturn.ProductProductGroups.Add(productProductGroup);
            }

            if (productCategory != null && !productToReturn.ProductCategories.Any(c => c.Id.Equals(productCategory.Id))) {
                productCategory.Category = category;

                productToReturn.ProductCategories.Add(productCategory);
            }

            if (productPricing != null && !productToReturn.ProductPricings.Any(p => p.Id.Equals(productPricing.Id))) {
                productPricing.Pricing = pricing;

                productToReturn.ProductPricings.Add(productPricing);
            }

            if (productAvailability != null) {
                if (!productToReturn.ProductAvailabilities.Any(a => a.Id.Equals(productAvailability.Id))) {
                    if (productPlacement != null) storage.ProductPlacements.Add(productPlacement);

                    productAvailability.Storage = storage;

                    if (!storage.ForDefective) {
                        if (storage.Locale.ToLower().Equals("pl")) {
                            if (storage.ForVatProducts)
                                productToReturn.AvailableQtyPlVAT += productAvailability.Amount;
                            else
                                productToReturn.AvailableQtyPl += productAvailability.Amount;
                        } else {
                            if (storage.ForVatProducts) {
                                productToReturn.AvailableQtyUkVAT += productAvailability.Amount;

                                if (storage.AvailableForReSale)
                                    productToReturn.AvailableQtyUkReSale += productAvailability.Amount;
                            } else {
                                productToReturn.AvailableQtyUk += productAvailability.Amount;
                            }
                        }
                    }

                    productToReturn.ProductAvailabilities.Add(productAvailability);
                } else if (productPlacement != null) {
                    ProductAvailability availabilityFromList = productToReturn.ProductAvailabilities.First(a => a.Id.Equals(productAvailability.Id));

                    if (!availabilityFromList.Storage.ProductPlacements.Any(p => p.Id.Equals(productPlacement.Id)))
                        availabilityFromList.Storage.ProductPlacements.Add(productPlacement);
                }
            }

            return product;
        };

        _connection.Query(
            productsExpression,
            productsTypes,
            productsMapper,
            joinProps,
            splitOn: "ID,ProductCurrentPrice,ProductCurrentLocalPrice,ProductCurrentPriceVat,ProductCurrentLocalPriceVat"
        );

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
            analoguesExpression += ",[Analogue].[NameUA] AS [Name] ";
            analoguesExpression += ",[Analogue].[DescriptionUA] AS [Description] ";
        } else {
            analoguesExpression += ",[Analogue].[NameUA] AS [Name] ";
            analoguesExpression += ",[Analogue].[DescriptionUA] AS [Description] ";
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
            ",[Analogue].Weight " +
            ",dbo.GetCalculatedProductPriceWithSharesAndVat([Analogue].NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS CurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Analogue].NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS CurrentLocalPrice ";

        if (vatAgreementNetId.HasValue)
            analoguesExpression +=
                ",dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentWithVatPrice " +
                ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentLocalWithVatPrice ";

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
            "AND [Analogue].ID IS NOT NULL " +
            "AND [Storage].ForDefective = 0 " +
            "ORDER BY CASE WHEN [Storage].Locale = @Culture THEN 0 ELSE 1 END";

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
                                    analogue.AvailableQtyUk += productAvailability.Amount;
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
                                    analogueFromList.AnalogueProduct.AvailableQtyUk += productAvailability.Amount;
                            } else {
                                analogueFromList.AnalogueProduct.AvailableQtyUk += productAvailability.Amount;
                            }
                        }
                    }

                    analogueFromList.AnalogueProduct.ProductAvailabilities.Add(productAvailability);
                }

                if (productPricing == null || analogueFromList.AnalogueProduct.ProductPricings.Any(p => p.Id.Equals(productPricing.Id))) return product;

                productPricing.Pricing = pricing;

                analogueFromList.AnalogueProduct.ProductPricings.Add(productPricing);
            }

            return product;
        };

        _connection.Query(
            analoguesExpression,
            analoguesTypes,
            analoguesMapper,
            joinProps
        );

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
            componentsExpression += ", [Component].[NameUA] AS [Name] ";
            componentsExpression += ", [Component].[DescriptionUA] AS [Description] ";
            componentsExpression += ", [Component].[NotesUA] AS [Notes] ";
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

        if (vatAgreementNetId.HasValue)
            componentsExpression +=
                ",dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentWithVatPrice " +
                ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentLocalWithVatPrice ";

        componentsExpression +=
            ",dbo.GetCalculatedProductPriceWithSharesAndVat([Component].NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS CurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Component].NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS CurrentLocalPrice " +
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
            "AND [Component].ID IS NOT NULL " +
            "AND [Storage].ForDefective = 0 " +
            "ORDER BY CASE WHEN [Storage].Locale = @Culture THEN 0 ELSE 1 END";

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
                                    component.AvailableQtyUk += productAvailability.Amount;
                            } else {
                                component.AvailableQtyUk += productAvailability.Amount;
                            }
                        }
                    }

                    component.ProductAvailabilities.Add(productAvailability);
                }

                if (productPricing != null) {
                    productPricing.Pricing = pricing;

                    component.ProductPricings.Add(productPricing);
                }

                component.MeasureUnit = measureUnit;

                component.CurrentLocalPrice = decimal.Round(component.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

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
                                    setFromList.ComponentProduct.AvailableQtyUk += productAvailability.Amount;
                            } else {
                                setFromList.ComponentProduct.AvailableQtyUk += productAvailability.Amount;
                            }
                        }
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
            componentAnaloguesExpression += ",[Analogue].[NameUA] AS [Name] ";
            componentAnaloguesExpression += ",[Analogue].[DescriptionUA] AS [Description] ";
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
            ",[Analogue].Weight ";

        if (vatAgreementNetId.HasValue)
            componentAnaloguesExpression +=
                ",dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentWithVatPrice " +
                ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentLocalWithVatPrice ";

        componentAnaloguesExpression +=
            ",dbo.GetCalculatedProductPriceWithSharesAndVat([Analogue].NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS CurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Analogue].NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS CurrentLocalPrice " +
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
            "AND [Storage].ForDefective = 0 " +
            "ORDER BY CASE WHEN [Storage].Locale = @Culture THEN 0 ELSE 1 END";

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
                                    analogue.AvailableQtyUk += productAvailability.Amount;
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
                                    analogueFromList.AnalogueProduct.AvailableQtyUk += productAvailability.Amount;
                            } else {
                                analogueFromList.AnalogueProduct.AvailableQtyUk += productAvailability.Amount;
                            }
                        }
                    }

                    analogueFromList.AnalogueProduct.ProductAvailabilities.Add(productAvailability);
                }

                if (productPricing == null || analogueFromList.AnalogueProduct.ProductPricings.Any(p => p.Id.Equals(productPricing.Id))) return product;

                productPricing.Pricing = pricing;

                analogueFromList.AnalogueProduct.ProductPricings.Add(productPricing);
            }

            return product;
        };

        _connection.Query(
            componentAnaloguesExpression,
            componentAnaloguesTypes,
            componentAnaloguesMapper,
            joinProps
        );

        string incomesSqlExpression =
            "SELECT * " +
            "FROM [ProductIncome] " +
            "LEFT JOIN [User] " +
            "ON [User].ID = [ProductIncome].UserID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductIncome].StorageID " +
            "LEFT JOIN [ProductIncomeItem] " +
            "ON [ProductIncomeItem].ProductIncomeID = [ProductIncome].ID " +
            "LEFT JOIN [PackingListPackageOrderItem] " +
            "ON [ProductIncomeItem].PackingListPackageOrderItemID = [PackingListPackageOrderItem].ID " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ID = [PackingListPackageOrderItem].PackingListID " +
            "LEFT JOIN [PackingListPackage] " +
            "ON [PackingListPackage].ID = [PackingListPackageOrderItem].PackingListPackageID " +
            "LEFT JOIN [PackingList] AS [PackagePackingList] " +
            "ON [PackagePackingList].ID = [PackingListPackage].PackingListID " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].ID = [PackingListPackageOrderItem].SupplyInvoiceOrderItemID " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [SupplyInvoiceOrderItem].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].ID = [SupplyInvoiceOrderItem].SupplyOrderItemID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "WHERE [SupplyInvoiceOrderItem].ProductID = @Id " +
            "AND [ProductIncome].Deleted = 0 " +
            "ORDER BY [ProductIncome].FromDate DESC";

        Type[] incomesTypes = {
            typeof(ProductIncome),
            typeof(User),
            typeof(Storage),
            typeof(ProductIncomeItem),
            typeof(PackingListPackageOrderItem),
            typeof(PackingList),
            typeof(PackingListPackage),
            typeof(PackingList),
            typeof(SupplyInvoiceOrderItem),
            typeof(SupplyInvoice),
            typeof(SupplyOrderItem),
            typeof(SupplyOrder),
            typeof(Client),
            typeof(SupplyOrderNumber)
        };

        Func<object[], ProductIncome> incomesMapper = objects => {
            ProductIncome productIncome = (ProductIncome)objects[0];
            User user = (User)objects[1];
            Storage storage = (Storage)objects[2];
            ProductIncomeItem productIncomeItem = (ProductIncomeItem)objects[3];
            PackingListPackageOrderItem packingListPackageOrderItem = (PackingListPackageOrderItem)objects[4];
            PackingList packingList = (PackingList)objects[5];
            PackingListPackage package = (PackingListPackage)objects[6];
            PackingList packagePackingList = (PackingList)objects[7];
            SupplyInvoiceOrderItem supplyInvoiceOrderItem = (SupplyInvoiceOrderItem)objects[8];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[9];
            SupplyOrderItem supplyOrderItem = (SupplyOrderItem)objects[10];
            SupplyOrder supplyOrder = (SupplyOrder)objects[11];
            Client client = (Client)objects[12];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[13];

            if (!productToReturn.ProductIncomes.Any(i => i.Id.Equals(productIncome.Id))) {
                if (package != null) package.PackingList = packagePackingList;

                if (supplyOrder != null) {
                    supplyOrder.Client = client;
                    supplyOrder.SupplyOrderNumber = supplyOrderNumber;
                }

                if (supplyOrderItem != null)
                    supplyOrderItem.SupplyOrder = supplyOrder;

                supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                supplyInvoiceOrderItem.SupplyInvoice = supplyInvoice;

                packingListPackageOrderItem.PackingList = packingList;
                packingListPackageOrderItem.PackingListPackage = package;
                packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                productIncomeItem.PackingListPackageOrderItem = packingListPackageOrderItem;

                productIncome.ProductIncomeItems.Add(productIncomeItem);

                productIncome.User = user;
                productIncome.Storage = storage;

                productToReturn.ProductIncomes.Add(productIncome);
            } else {
                ProductIncome fromList = productToReturn.ProductIncomes.First(i => i.Id.Equals(productIncome.Id));

                if (fromList.ProductIncomeItems.Any(i => i.Id.Equals(productIncomeItem.Id))) return productIncome;

                if (package != null) package.PackingList = packagePackingList;

                if (supplyOrder != null) {
                    supplyOrder.Client = client;
                    supplyOrder.SupplyOrderNumber = supplyOrderNumber;
                }

                if (supplyOrderItem != null)
                    supplyOrderItem.SupplyOrder = supplyOrder;

                supplyInvoiceOrderItem.SupplyOrderItem = supplyOrderItem;
                supplyInvoiceOrderItem.SupplyInvoice = supplyInvoice;

                packingListPackageOrderItem.PackingList = packingList;
                packingListPackageOrderItem.PackingListPackage = package;
                packingListPackageOrderItem.SupplyInvoiceOrderItem = supplyInvoiceOrderItem;

                productIncomeItem.PackingListPackageOrderItem = packingListPackageOrderItem;

                fromList.ProductIncomeItems.Add(productIncomeItem);
            }

            return productIncome;
        };

        _connection.Query(
            incomesSqlExpression,
            incomesTypes,
            incomesMapper,
            new {
                productToReturn.Id,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        return productToReturn;
    }

    public Product GetByNetIdWithAvailabilityByCurrentCulture(Guid netId, Guid? clientAgreementNetId = null) {
        Product productToReturn = null;

        string sqlExpression =
            "SELECT [Product].ID " +
            ",[Product].Created " +
            ",[Product].Deleted " +
            ",[Product].HasAnalogue " +
            ",[Product].HasComponent " +
            ",[Product].HasImage " +
            ",[Product].Image " +
            ",[Product].IsForSale " +
            ",[Product].IsForWeb " +
            ",[Product].IsForZeroSale " +
            ",[Product].MainOriginalNumber " +
            ",[Product].MeasureUnitID " +
            ",[Product].NetUID ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ", [Product].[NameUA] AS [Name] ";
            sqlExpression += ", [Product].[DescriptionUA] AS [Description] ";
            sqlExpression += ", [Product].[NotesUA] AS [Notes] ";
        } else {
            sqlExpression += ", [Product].[NameUA] AS [Name] ";
            sqlExpression += ", [Product].[DescriptionUA] AS [Description] ";
            sqlExpression += ", [Product].[NotesUA] AS [Notes] ";
        }

        sqlExpression +=
            ",[Product].[NameUA] " +
            ",[Product].[DescriptionUA] " +
            ",[Product].[NameUA] " +
            ",[Product].[DescriptionUA] " +
            ",[Product].OrderStandard " +
            ",[Product].PackingStandard " +
            ",[Product].Standard " +
            ",[Product].Size " +
            ",[Product].[SynonymsUA] " +
            ",[Product].[SynonymsPL] " +
            ",[Product].[Top] " +
            ",[Product].UCGFEA " +
            ",[Product].Updated " +
            ",[Product].VendorCode " +
            ",[Product].Volume " +
            ",[Product].Weight " +
            ",ProductPricing.* " +
            ",ProductProductGroup.* " +
            ",ProductSpecification.* " +
            ",[User].* " +
            ",MeasureUnit.* " +
            ",[ProductImage].* " +
            "FROM Product " +
            "LEFT JOIN ProductPricing " +
            "ON ProductPricing.ProductID = Product.ID " +
            "AND ProductPricing.Deleted = 0 " +
            "LEFT JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID " +
            "AND ProductProductGroup.Deleted = 0 " +
            "LEFT JOIN ProductSpecification " +
            "ON ProductSpecification.ProductID = Product.ID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = ProductSpecification.AddedById " +
            "LEFT JOIN MeasureUnit " +
            "ON MeasureUnit.ID = Product.MeasureUnitID " +
            "LEFT JOIN [ProductImage] " +
            "ON [ProductImage].ProductID = [Product].ID " +
            "AND [ProductImage].Deleted = 0 " +
            "WHERE Product.NetUID = @NetId " +
            "ORDER BY ProductSpecification.Created";

        _connection.Query<Product, ProductPricing, ProductProductGroup, ProductSpecification, User, MeasureUnit, ProductImage, Product>(
            sqlExpression,
            (product, productPricing, productProductGroup, specification, user, measureUnit, image) => {
                if (productToReturn != null) {
                    if (productPricing != null && !productToReturn.ProductPricings.Any(p => p.Id.Equals(productToReturn.Id))) productToReturn.ProductPricings.Add(productPricing);

                    if (productPricing != null && !productToReturn.ProductProductGroups.Any(p => p.Id.Equals(productProductGroup.Id)))
                        productToReturn.ProductProductGroups.Add(productProductGroup);

                    if (specification != null && !productToReturn.ProductSpecifications.Any(s => s.Id.Equals(specification.Id))) {
                        specification.AddedBy = user;

                        productToReturn.ProductSpecifications.Add(specification);
                    }

                    if (image != null && !productToReturn.ProductImages.Any(i => i.Id.Equals(image.Id))) productToReturn.ProductImages.Add(image);
                } else {
                    if (productPricing != null) product.ProductPricings.Add(productPricing);

                    if (productProductGroup != null) product.ProductProductGroups.Add(productProductGroup);

                    if (specification != null) {
                        specification.AddedBy = user;

                        product.ProductSpecifications.Add(specification);
                    }

                    if (image != null) product.ProductImages.Add(image);

                    product.MeasureUnit = measureUnit;

                    productToReturn = product;
                }

                return product;
            },
            new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        if (productToReturn == null) return productToReturn;

        bool withVat = clientAgreementNetId.HasValue && _connection.Query<bool>(
            "SELECT [Agreement].[WithVATAccounting] FROM [ClientAgreement] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "WHERE [ClientAgreement].[NetUID] = @NetId; ",
            new { NetId = clientAgreementNetId.Value }).FirstOrDefault();

        var joinProps = new {
            productToReturn.Id,
            ClientAgreementNetId = clientAgreementNetId ?? Guid.Empty,
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
            WithVat = withVat
        };

        string productsExpression =
            "SELECT [Product].ID " +
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
            ",dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS ProductCurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS ProductCurrentLocalPrice " +
            "FROM [Product] " +
            "LEFT JOIN [ProductOriginalNumber] " +
            "ON [ProductOriginalNumber].ProductID = [Product].ID " +
            "AND [ProductOriginalNumber].Deleted = 0 " +
            "LEFT JOIN [OriginalNumber] " +
            "ON [ProductOriginalNumber].OriginalNumberID = [OriginalNumber].ID " +
            "LEFT JOIN [ProductProductGroup] " +
            "ON [ProductProductGroup].ProductID = [Product].ID " +
            "AND [ProductProductGroup].Deleted = 0 " +
            "LEFT JOIN [ProductGroup] " +
            "ON [ProductGroup].ID = [ProductProductGroup].ProductGroupID " +
            "LEFT JOIN [ProductCategory] " +
            "ON [ProductCategory].ProductID = [Product].ID " +
            "AND [ProductCategory].Deleted = 0 " +
            "LEFT JOIN [Category] " +
            "ON [Category].ID = [ProductCategory].CategoryID " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "LEFT JOIN [ProductPricing] " +
            "ON [ProductPricing].ProductID = [Product].ID " +
            "AND [ProductPricing].Deleted = 0 " +
            "LEFT JOIN [views].[PricingView] AS [Pricing] " +
            "ON [Pricing].ID = [ProductPricing].PricingID " +
            "AND [Pricing].CultureCode = @Culture " +
            "WHERE [Product].ID = @Id " +
            "AND ([Storage].Locale = @Culture OR [Storage].Locale IS NULL)";

        Type[] productsTypes = {
            typeof(Product),
            typeof(ProductOriginalNumber),
            typeof(OriginalNumber),
            typeof(ProductProductGroup),
            typeof(ProductGroup),
            typeof(ProductCategory),
            typeof(Category),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(ProductPricing),
            typeof(Pricing),
            typeof(decimal),
            typeof(decimal)
        };

        Func<object[], Product> productsMapper = objects => {
            Product product = (Product)objects[0];
            ProductOriginalNumber productOriginalNumber = (ProductOriginalNumber)objects[1];
            OriginalNumber originalNumber = (OriginalNumber)objects[2];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[3];
            ProductGroup productGroup = (ProductGroup)objects[4];
            ProductCategory productCategory = (ProductCategory)objects[5];
            Category category = (Category)objects[6];
            ProductAvailability productAvailability = (ProductAvailability)objects[7];
            Storage storage = (Storage)objects[8];
            ProductPricing productPricing = (ProductPricing)objects[9];
            Pricing pricing = (Pricing)objects[10];
            decimal currentPrice = (decimal)objects[11];
            decimal currentLocalPrice = (decimal)objects[12];

            productToReturn.CurrentPrice = currentPrice;
            productToReturn.CurrentLocalPrice = currentLocalPrice;

            if (productOriginalNumber != null && !productToReturn.ProductOriginalNumbers.Any(n => n.Id.Equals(productOriginalNumber.Id))) {
                productOriginalNumber.OriginalNumber = originalNumber;

                productToReturn.ProductOriginalNumbers.Add(productOriginalNumber);
            }

            if (productProductGroup != null && !productToReturn.ProductProductGroups.Any(g => g.Id.Equals(productProductGroup.Id))) {
                productProductGroup.ProductGroup = productGroup;

                productToReturn.ProductProductGroups.Add(productProductGroup);
            }

            if (productCategory != null && !productToReturn.ProductCategories.Any(c => c.Id.Equals(productCategory.Id))) {
                productCategory.Category = category;

                productToReturn.ProductCategories.Add(productCategory);
            }

            if (productAvailability != null && !productToReturn.ProductAvailabilities.Any(a => a.Id.Equals(productAvailability.Id))) {
                productAvailability.Storage = storage;

                productToReturn.ProductAvailabilities.Add(productAvailability);
            }

            if (productPricing == null || productToReturn.ProductPricings.Any(p => p.Id.Equals(productPricing.Id))) return product;

            productPricing.Pricing = pricing;

            productToReturn.ProductPricings.Add(productPricing);

            return product;
        };

        _connection.Query(
            productsExpression,
            productsTypes,
            productsMapper,
            joinProps,
            splitOn: "ID,ProductCurrentPrice,ProductCurrentLocalPrice"
        );

        string analoguesExpression =
            "SELECT [Product].ID " +
            ",[ProductAnalogue].* " +
            ",[Analogue].ID " +
            ",[Analogue].Created " +
            ",[Analogue].Deleted " +
            ",[Analogue].HasAnalogue " +
            ",[Analogue].HasComponent " +
            ",[Analogue].HasImage " +
            ",[Analogue].[SynonymsUA] " +
            ",[Analogue].[SynonymsPL] " +
            ",[Analogue].Image " +
            ",[Analogue].IsForSale " +
            ",[Analogue].IsForWeb " +
            ",[Analogue].IsForZeroSale " +
            ",[Analogue].MainOriginalNumber " +
            ",[Analogue].MeasureUnitID " +
            ",[Analogue].NetUID ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            analoguesExpression += ",[Analogue].[NameUA] AS [Name] ";
            analoguesExpression += ",[Analogue].[DescriptionUA] AS [Description] ";
        } else {
            analoguesExpression += ",[Analogue].[NameUA] AS [Name] ";
            analoguesExpression += ",[Analogue].[DescriptionUA] AS [Description] ";
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
            "AND [Analogue].ID IS NOT NULL";

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

                    analogue.ProductAvailabilities.Add(productAvailability);
                }

                if (productPricing != null) {
                    productPricing.Pricing = pricing;

                    analogue.ProductPricings.Add(productPricing);
                }

                analogue.MeasureUnit = measureUnit;

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

                if (productAvailability != null && !analogueFromList.AnalogueProduct.ProductAvailabilities.Any(a => a.Id.Equals(productAvailability.Id))) {
                    productAvailability.Storage = storage;

                    analogueFromList.AnalogueProduct.ProductAvailabilities.Add(productAvailability);
                }

                if (productPricing == null || analogueFromList.AnalogueProduct.ProductPricings.Any(p => p.Id.Equals(productPricing.Id))) return product;

                productPricing.Pricing = pricing;

                analogueFromList.AnalogueProduct.ProductPricings.Add(productPricing);
            }

            return product;
        };

        _connection.Query(
            analoguesExpression,
            analoguesTypes,
            analoguesMapper,
            joinProps
        );

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
            componentsExpression += ", [Component].[NameUA] AS [Name] ";
            componentsExpression += ", [Component].[DescriptionUA] AS [Description] ";
            componentsExpression += ", [Component].[NotesUA] AS [Notes] ";
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
            ",[Component].Weight " +
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
            "AND [Component].ID IS NOT NULL";

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

                    component.ProductAvailabilities.Add(productAvailability);
                }

                if (productPricing != null) {
                    productPricing.Pricing = pricing;

                    component.ProductPricings.Add(productPricing);
                }

                component.MeasureUnit = measureUnit;

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

                    setFromList.ComponentProduct.ProductAvailabilities.Add(productAvailability);
                }

                if (productPricing == null || setFromList.ComponentProduct.ProductPricings.Any(p => p.Id.Equals(productPricing.Id))) return product;

                productPricing.Pricing = pricing;

                setFromList.ComponentProduct.ProductPricings.Add(productPricing);
            }

            return product;
        };

        _connection.Query(
            componentsExpression,
            componentsTypes,
            componentsMapper,
            joinProps
        );

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
            componentAnaloguesExpression += ",[Analogue].[NameUA] AS [Name] ";
            componentAnaloguesExpression += ",[Analogue].[DescriptionUA] AS [Description] ";
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
            ",[Analogue].[SynonymsUA] " +
            ",[Analogue].[SynonymsPL] " +
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
            "AND [Analogue].ID IS NOT NULL";

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

                    analogue.ProductAvailabilities.Add(productAvailability);
                }

                if (productPricing != null) {
                    productPricing.Pricing = pricing;

                    analogue.ProductPricings.Add(productPricing);
                }

                analogue.MeasureUnit = measureUnit;

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

                    analogueFromList.AnalogueProduct.ProductAvailabilities.Add(productAvailability);
                }

                if (productPricing == null || analogueFromList.AnalogueProduct.ProductPricings.Any(p => p.Id.Equals(productPricing.Id))) return product;

                productPricing.Pricing = pricing;

                analogueFromList.AnalogueProduct.ProductPricings.Add(productPricing);
            }

            return product;
        };

        _connection.Query(
            componentAnaloguesExpression,
            componentAnaloguesTypes,
            componentAnaloguesMapper,
            joinProps
        );

        return productToReturn;
    }

    public Product GetBySlug(string slug, Guid nonVatAgreementNetId, Guid? vatAgreementNetId) {
        Product productToReturn = null;

        string sqlExpression =
            "SELECT [Product].ID " +
            ",[Product].Created " +
            ",[Product].Deleted " +
            ",[Product].HasAnalogue " +
            ",[Product].HasComponent " +
            ",[Product].HasImage " +
            ",[Product].Image " +
            ",[Product].IsForSale " +
            ",[Product].IsForWeb " +
            ",[Product].IsForZeroSale " +
            ",[Product].MainOriginalNumber " +
            ",[Product].MeasureUnitID " +
            ",[Product].NetUID ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ", [Product].[NameUA] AS [Name] ";
            sqlExpression += ", [Product].[DescriptionUA] AS [Description] ";
            sqlExpression += ", [Product].[NotesUA] AS [Notes] ";
        } else {
            sqlExpression += ", [Product].[NameUA] AS [Name] ";
            sqlExpression += ", [Product].[DescriptionUA] AS [Description] ";
            sqlExpression += ", [Product].[NotesUA] AS [Notes] ";
        }

        sqlExpression +=
            ",[Product].[NameUA] " +
            ",[Product].[DescriptionUA] " +
            ",[Product].[NameUA] " +
            ",[Product].[SynonymsUA] " +
            ",[Product].[SynonymsPL] " +
            ",[Product].[DescriptionUA] " +
            ",[Product].OrderStandard " +
            ",[Product].PackingStandard " +
            ",[Product].Standard " +
            ",[Product].Size " +
            ",[Product].[Top] " +
            ",[Product].UCGFEA " +
            ",[Product].Updated " +
            ",[Product].VendorCode " +
            ",[Product].Volume " +
            ",[Product].Weight " +
            ",ProductSpecification.* " +
            ",[User].* " +
            ",MeasureUnit.* " +
            ",[ProductImage].* " +
            ",[ProductSlug].* " +
            "FROM Product " +
            "LEFT JOIN ProductSpecification " +
            "ON ProductSpecification.ProductID = Product.ID " +
            "LEFT JOIN [User] " +
            "ON [User].ID = ProductSpecification.AddedById " +
            "LEFT JOIN MeasureUnit " +
            "ON MeasureUnit.ID = Product.MeasureUnitID " +
            "LEFT JOIN [ProductImage] " +
            "ON [ProductImage].ProductID = [Product].ID " +
            "AND [ProductImage].Deleted = 0 " +
            "LEFT JOIN [ProductSlug] " +
            "ON [ProductSlug].ID = (" +
            "SELECT TOP(1) ID " +
            "FROM [ProductSlug] " +
            "WHERE [ProductSlug].Deleted = 0 " +
            "AND [ProductSlug].[Locale] = @Culture " +
            "AND [ProductSlug].ProductID = [Product].ID" +
            ") " +
            "WHERE Product.ID = (" +
            "SELECT TOP(1) [ProductSlug].ProductID " +
            "FROM [ProductSlug] " +
            "WHERE [ProductSlug].Deleted = 0 " +
            "AND [ProductSlug].[Locale] = @Culture " +
            "AND [ProductSlug].[Url] = @Slug " +
            (
                CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")
                    ? "AND [ProductSlug].Locale = N'pl'"
                    : "AND [ProductSlug].Locale = N'uk'"
            ) +
            ") " +
            "ORDER BY ProductSpecification.Created";

        Type[] types = {
            typeof(Product),
            typeof(ProductSpecification),
            typeof(User),
            typeof(MeasureUnit),
            typeof(ProductImage),
            typeof(ProductSlug)
        };

        Func<object[], Product> mapper = objects => {
            Product product = (Product)objects[0];
            ProductSpecification specification = (ProductSpecification)objects[1];
            User user = (User)objects[2];
            MeasureUnit measureUnit = (MeasureUnit)objects[3];
            ProductImage image = (ProductImage)objects[4];
            ProductSlug productSlug = (ProductSlug)objects[5];

            if (productToReturn == null) {
                productToReturn = product;

                productToReturn.MeasureUnit = measureUnit;
                productToReturn.ProductSlug = productSlug;
            }

            if (specification != null && !productToReturn.ProductSpecifications.Any(s => s.Id.Equals(specification.Id))) {
                specification.AddedBy = user;

                productToReturn.ProductSpecifications.Add(specification);
            }

            if (image != null && !productToReturn.ProductImages.Any(i => i.Id.Equals(image.Id))) productToReturn.ProductImages.Add(image);

            return product;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new { Slug = slug, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        if (productToReturn == null) return productToReturn;

        var joinProps = new {
            productToReturn.Id,
            NonVatAgreementNetId = nonVatAgreementNetId,
            VatAgreementNetId = vatAgreementNetId ?? Guid.Empty,
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
        };

        string productsExpression =
            "SELECT [Product].ID " +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS ProductCurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS ProductCurrentLocalPrice " +
            ",dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS ProductCurrentPriceVat " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS ProductCurrentLocalPriceVat " +
            "FROM [Product] " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [Product].ID = @Id " +
            "AND [Storage].ForDefective = 0 " +
            "ORDER BY CASE WHEN [Storage].Locale = @Culture THEN 0 ELSE 1 END";

        Type[] productsTypes = {
            typeof(Product),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(decimal),
            typeof(decimal),
            typeof(decimal),
            typeof(decimal)
        };

        Func<object[], Product> productsMapper = objects => {
            Product product = (Product)objects[0];
            ProductAvailability productAvailability = (ProductAvailability)objects[1];
            Storage storage = (Storage)objects[2];
            decimal currentPrice = (decimal)objects[3];
            decimal currentLocalPrice = (decimal)objects[4];
            decimal currentPriceVat = (decimal)objects[5];
            decimal currentLocalPriceVat = (decimal)objects[6];

            productToReturn.CurrentPrice = currentPrice;
            productToReturn.CurrentLocalPrice = decimal.Round(currentLocalPrice, 2, MidpointRounding.AwayFromZero);
            productToReturn.CurrentWithVatPrice = currentPriceVat;
            productToReturn.CurrentLocalWithVatPrice = decimal.Round(currentLocalPriceVat, 2, MidpointRounding.AwayFromZero);

            if (productAvailability == null) return product;

            if (productToReturn.ProductAvailabilities.Any(a => a.Id.Equals(productAvailability.Id))) return product;

            productAvailability.Storage = storage;

            if (!storage.ForDefective) {
                if (storage.Locale.ToLower().Equals("pl")) {
                    if (storage.ForVatProducts)
                        productToReturn.AvailableQtyPlVAT += productAvailability.Amount;
                    else
                        productToReturn.AvailableQtyPl += productAvailability.Amount;
                } else {
                    if (storage.ForVatProducts) {
                        productToReturn.AvailableQtyUkVAT += productAvailability.Amount;

                        if (storage.AvailableForReSale)
                            productToReturn.AvailableQtyUkReSale += productAvailability.Amount;
                    } else {
                        productToReturn.AvailableQtyUk += productAvailability.Amount;
                    }
                }
            }

            productToReturn.ProductAvailabilities.Add(productAvailability);

            return product;
        };

        _connection.Query(
            productsExpression,
            productsTypes,
            productsMapper,
            joinProps,
            splitOn: "ID,ProductCurrentPrice,ProductCurrentLocalPrice,ProductCurrentPriceVat,ProductCurrentLocalPriceVat"
        );

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
            analoguesExpression += ",[Analogue].[NameUA] AS [Name] ";
            analoguesExpression += ",[Analogue].[DescriptionUA] AS [Description] ";
        } else {
            analoguesExpression += ",[Analogue].[NameUA] AS [Name] ";
            analoguesExpression += ",[Analogue].[DescriptionUA] AS [Description] ";
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
            ",[Analogue].Weight " +
            ",dbo.GetCalculatedProductPriceWithSharesAndVat([Analogue].NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS ProductCurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Analogue].NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS ProductCurrentLocalPrice " +
            ",dbo.GetCalculatedProductPriceWithSharesAndVat([Analogue].NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS ProductCurrentPriceVat " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Analogue].NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS ProductCurrentLocalPriceVat " +
            ",[MeasureUnit].* " +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[ProductSlug].* " +
            "FROM [Product] " +
            "LEFT JOIN [ProductAnalogue] " +
            "ON [ProductAnalogue].BaseProductID = [Product].ID " +
            "AND [ProductAnalogue].Deleted = 0 " +
            "LEFT JOIN [Product] AS [Analogue] " +
            "ON [Analogue].ID = [ProductAnalogue].AnalogueProductID " +
            "LEFT JOIN [MeasureUnit] " +
            "ON [Analogue].MeasureUnitID = [MeasureUnit].ID " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Analogue].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "LEFT JOIN [ProductSlug] " +
            "ON [ProductSlug].ID = (" +
            "SELECT TOP(1) ID " +
            "FROM [ProductSlug] " +
            "WHERE [ProductSlug].Deleted = 0 " +
            "AND [ProductSlug].[Locale] = @Culture " +
            "AND [ProductSlug].ProductID = [Analogue].ID" +
            ") " +
            "WHERE [Product].ID = @Id " +
            "AND [Analogue].ID IS NOT NULL " +
            "AND [Storage].ForDefective = 0 " +
            "ORDER BY CASE WHEN [Storage].Locale = @Culture THEN 0 ELSE 1 END";

        Type[] analoguesTypes = {
            typeof(Product),
            typeof(ProductAnalogue),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(ProductSlug)
        };

        Func<object[], Product> analoguesMapper = objects => {
            Product product = (Product)objects[0];
            ProductAnalogue productAnalogue = (ProductAnalogue)objects[1];
            Product analogue = (Product)objects[2];
            MeasureUnit measureUnit = (MeasureUnit)objects[3];
            ProductAvailability productAvailability = (ProductAvailability)objects[4];
            Storage storage = (Storage)objects[5];
            ProductSlug productSlug = (ProductSlug)objects[6];

            if (analogue == null) return product;

            analogue.ProductSlug = productSlug;

            if (!productToReturn.AnalogueProducts.Any(a => a.Id.Equals(productAnalogue.Id))) {
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
                                    analogue.AvailableQtyUk += productAvailability.Amount;
                            } else {
                                analogue.AvailableQtyUk += productAvailability.Amount;
                            }
                        }
                    }

                    analogue.ProductAvailabilities.Add(productAvailability);
                }

                analogue.MeasureUnit = measureUnit;

                analogue.CurrentLocalPrice = decimal.Round(analogue.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                productAnalogue.AnalogueProduct = analogue;

                productToReturn.AnalogueProducts.Add(productAnalogue);

                productToReturn.HasAnalogue = true;
            } else {
                ProductAnalogue analogueFromList = productToReturn.AnalogueProducts.First(a => a.Id.Equals(productAnalogue.Id));

                if (productAvailability == null
                    || analogueFromList.AnalogueProduct.ProductAvailabilities.Any(a => a.Id.Equals(productAvailability.Id))) return product;

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
                                analogueFromList.AnalogueProduct.AvailableQtyUk += productAvailability.Amount;
                        } else {
                            analogueFromList.AnalogueProduct.AvailableQtyUk += productAvailability.Amount;
                        }
                    }
                }

                analogueFromList.AnalogueProduct.ProductAvailabilities.Add(productAvailability);
            }

            return product;
        };

        _connection.Query(
            analoguesExpression,
            analoguesTypes,
            analoguesMapper,
            joinProps
        );

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
            componentsExpression += ", [Component].[NameUA] AS [Name] ";
            componentsExpression += ", [Component].[DescriptionUA] AS [Description] ";
            componentsExpression += ", [Component].[NotesUA] AS [Notes] ";
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
            ",[Component].Weight " +
            ",dbo.GetCalculatedProductPriceWithSharesAndVat([Component].NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS ProductCurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Component].NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS ProductCurrentLocalPrice " +
            ",dbo.GetCalculatedProductPriceWithSharesAndVat([Component].NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS ProductCurrentPriceVat " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Component].NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS ProductCurrentLocalPriceVat " +
            ",[MeasureUnit].* " +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[ProductSlug].* " +
            "FROM [Product] " +
            "LEFT JOIN [ProductSet] " +
            "ON [ProductSet].BaseProductID = [Product].ID " +
            "AND [ProductSet].Deleted = 0 " +
            "LEFT JOIN [Product] AS [Component] " +
            "ON [Component].ID = [ProductSet].ComponentProductID " +
            "LEFT JOIN [MeasureUnit] " +
            "ON [Component].MeasureUnitID = [MeasureUnit].ID " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Component].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "LEFT JOIN [ProductSlug] " +
            "ON [ProductSlug].ID = (" +
            "SELECT TOP(1) ID " +
            "FROM [ProductSlug] " +
            "WHERE [ProductSlug].Deleted = 0 " +
            "AND [ProductSlug].[Locale] = @Culture " +
            "AND [ProductSlug].ProductID = [Component].ID" +
            ") " +
            "WHERE [Product].ID = @Id " +
            "AND [Component].ID IS NOT NULL " +
            "AND [Storage].ForDefective = 0 " +
            "ORDER BY CASE WHEN [Storage].Locale = @Culture THEN 0 ELSE 1 END";

        Type[] componentsTypes = {
            typeof(Product),
            typeof(ProductSet),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(ProductSlug)
        };

        Func<object[], Product> componentsMapper = objects => {
            Product product = (Product)objects[0];
            ProductSet productSet = (ProductSet)objects[1];
            Product component = (Product)objects[2];
            MeasureUnit measureUnit = (MeasureUnit)objects[3];
            ProductAvailability productAvailability = (ProductAvailability)objects[4];
            Storage storage = (Storage)objects[5];
            ProductSlug productSlug = (ProductSlug)objects[6];

            if (component == null) return product;

            component.ProductSlug = productSlug;

            if (!productToReturn.ComponentProducts.Any(a => a.Id.Equals(productSet.Id))) {
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
                                    component.AvailableQtyUk += productAvailability.Amount;
                            } else {
                                component.AvailableQtyUk += productAvailability.Amount;
                            }
                        }
                    }

                    component.ProductAvailabilities.Add(productAvailability);
                }

                component.MeasureUnit = measureUnit;

                component.CurrentLocalPrice = decimal.Round(component.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                productSet.ComponentProduct = component;

                productToReturn.ComponentProducts.Add(productSet);

                productToReturn.HasComponent = true;
            } else {
                ProductSet setFromList = productToReturn.ComponentProducts.First(a => a.Id.Equals(productSet.Id));

                if (productAvailability == null
                    || setFromList.ComponentProduct.ProductAvailabilities.Any(a => a.Id.Equals(productAvailability.Id))) return product;

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
                                setFromList.ComponentProduct.AvailableQtyUk += productAvailability.Amount;
                        } else {
                            setFromList.ComponentProduct.AvailableQtyUk += productAvailability.Amount;
                        }
                    }
                }

                setFromList.ComponentProduct.ProductAvailabilities.Add(productAvailability);
            }

            return product;
        };

        _connection.Query(
            componentsExpression,
            componentsTypes,
            componentsMapper,
            joinProps
        );

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
            componentAnaloguesExpression += ",[Analogue].[NameUA] AS [Name] ";
            componentAnaloguesExpression += ",[Analogue].[DescriptionUA] AS [Description] ";
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
            ",dbo.GetCalculatedProductPriceWithSharesAndVat([Analogue].NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS ProductCurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Analogue].NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS ProductCurrentLocalPrice " +
            ",dbo.GetCalculatedProductPriceWithSharesAndVat([Analogue].NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS ProductCurrentPriceVat " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Analogue].NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS ProductCurrentLocalPriceVat " +
            ",[MeasureUnit].* " +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[ProductSlug].* " +
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
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Analogue].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "LEFT JOIN [ProductSlug] " +
            "ON [ProductSlug].ID = (" +
            "SELECT TOP(1) ID " +
            "FROM [ProductSlug] " +
            "WHERE [ProductSlug].Deleted = 0 " +
            "AND [ProductSlug].[Locale] = @Culture " +
            "AND [ProductSlug].ProductID = [Analogue].ID" +
            ") " +
            "WHERE [Product].ID = @Id " +
            "AND [Analogue].ID IS NOT NULL " +
            "AND [Storage].ForDefective = 0 " +
            "ORDER BY CASE WHEN [Storage].Locale = @Culture THEN 0 ELSE 1 END";

        Type[] componentAnaloguesTypes = {
            typeof(Product),
            typeof(ProductSet),
            typeof(ProductAnalogue),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(ProductSlug)
        };

        Func<object[], Product> componentAnaloguesMapper = objects => {
            Product product = (Product)objects[0];
            ProductSet productSet = (ProductSet)objects[1];
            ProductAnalogue productAnalogue = (ProductAnalogue)objects[2];
            Product analogue = (Product)objects[3];
            MeasureUnit measureUnit = (MeasureUnit)objects[4];
            ProductAvailability productAvailability = (ProductAvailability)objects[5];
            Storage storage = (Storage)objects[6];
            ProductSlug productSlug = (ProductSlug)objects[7];

            if (productSet == null) return product;

            ProductSet component = productToReturn.ComponentProducts.First(c => c.Id.Equals(productSet.Id));

            if (analogue == null) return product;

            analogue.ProductSlug = productSlug;

            if (!component.ComponentProduct.AnalogueProducts.Any(a => a.Id.Equals(productAnalogue.Id))) {
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
                                    analogue.AvailableQtyUk += productAvailability.Amount;
                            } else {
                                analogue.AvailableQtyUk += productAvailability.Amount;
                            }
                        }
                    }

                    analogue.ProductAvailabilities.Add(productAvailability);
                }

                analogue.MeasureUnit = measureUnit;

                analogue.CurrentLocalPrice = decimal.Round(analogue.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                productAnalogue.AnalogueProduct = analogue;

                component.ComponentProduct.AnalogueProducts.Add(productAnalogue);

                component.ComponentProduct.HasAnalogue = true;
            } else {
                ProductAnalogue analogueFromList = component.ComponentProduct.AnalogueProducts.First(a => a.Id.Equals(productAnalogue.Id));

                if (productAvailability == null || analogueFromList.AnalogueProduct.ProductAvailabilities.Any(a => a.Id.Equals(productAvailability.Id))) return product;

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
                                analogueFromList.AnalogueProduct.AvailableQtyUk += productAvailability.Amount;
                        } else {
                            analogueFromList.AnalogueProduct.AvailableQtyUk += productAvailability.Amount;
                        }
                    }
                }

                analogueFromList.AnalogueProduct.ProductAvailabilities.Add(productAvailability);
            }

            return product;
        };

        _connection.Query(
            componentAnaloguesExpression,
            componentAnaloguesTypes,
            componentAnaloguesMapper,
            joinProps
        );

        return productToReturn;
    }

    public long GetProductIdByVendorCode(string vendorCode) {
        return _connection.Query<long>(
            "SELECT TOP(1) [Product].ID " +
            "FROM [Product] " +
            "WHERE [Product].Deleted = 0 " +
            "AND [Product].VendorCode = @VendorCode",
            new { VendorCode = vendorCode }
        ).FirstOrDefault();
    }

    public Product GetProductByVendorCode(string vendorCode) {
        return _connection.Query<Product>(
            "SELECT TOP(1) * " +
            "FROM [Product] " +
            "WHERE [Product].Deleted = 0 " +
            "AND [Product].VendorCode = @VendorCode",
            new { VendorCode = vendorCode }
        ).SingleOrDefault();
    }

    public Product GetProductByVendorCodeWithMeasureUnit(string vendorCode) {
        return _connection.Query<Product, MeasureUnit, Product>(
            "SELECT TOP(1) * " +
            "FROM [Product] " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "WHERE [Product].Deleted = 0 " +
            "AND [Product].VendorCode = @VendorCode",
            (product, measureUnit) => {
                product.MeasureUnit = measureUnit;

                return product;
            },
            new { VendorCode = vendorCode, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        ).SingleOrDefault();
    }

    public Product GetByVendorCodeAndRuleLocaleWithProductGroupAndWriteOffRules(string vendorCode, string locale) {
        return _connection.Query<Product, ProductWriteOffRule, ProductProductGroup, ProductGroup, ProductWriteOffRule, Product>(
                "SELECT TOP(1) * " +
                "FROM [Product] " +
                "LEFT JOIN [ProductWriteOffRule] " +
                "ON [ProductWriteOffRule].ProductID = [Product].ID " +
                "AND [ProductWriteOffRule].Deleted = 0 " +
                "AND [ProductWriteOffRule].RuleLocale = @Locale " +
                "LEFT JOIN [ProductProductGroup] " +
                "ON [ProductProductGroup].ProductID = [Product].ID " +
                "AND [ProductProductGroup].Deleted = 0 " +
                "LEFT JOIN [ProductGroup] " +
                "ON [ProductGroup].ID = [ProductProductGroup].ProductGroupID " +
                "LEFT JOIN [ProductWriteOffRule] AS [GroupRule] " +
                "ON [GroupRule].ProductGroupID = [ProductGroup].ID " +
                "AND [GroupRule].Deleted = 0 " +
                "AND [GroupRule].RuleLocale = @Locale " +
                "WHERE [Product].VendorCode = @VendorCode",
                (product, productWriteOffRule, productProductGroup, productGroup, groupRule) => {
                    if (productWriteOffRule != null) product.ProductWriteOffRules.Add(productWriteOffRule);

                    if (productProductGroup == null || groupRule == null) return product;

                    productGroup.ProductWriteOffRules.Add(groupRule);

                    productProductGroup.ProductGroup = productGroup;

                    product.ProductProductGroups.Add(productProductGroup);

                    return product;
                },
                new { VendorCode = vendorCode, Locale = locale }
            )
            .SingleOrDefault();
    }

    public Product GetByIdAndRuleLocaleWithProductGroupAndWriteOffRules(long id, string locale) {
        return _connection.Query<Product, ProductWriteOffRule, ProductProductGroup, ProductGroup, ProductWriteOffRule, Product>(
                "SELECT TOP(1) * " +
                "FROM [Product] " +
                "LEFT JOIN [ProductWriteOffRule] " +
                "ON [ProductWriteOffRule].ProductID = [Product].ID " +
                "AND [ProductWriteOffRule].Deleted = 0 " +
                "AND [ProductWriteOffRule].RuleLocale = @Locale " +
                "LEFT JOIN [ProductProductGroup] " +
                "ON [ProductProductGroup].ProductID = [Product].ID " +
                "AND [ProductProductGroup].Deleted = 0 " +
                "LEFT JOIN [ProductGroup] " +
                "ON [ProductGroup].ID = [ProductProductGroup].ProductGroupID " +
                "LEFT JOIN [ProductWriteOffRule] AS [GroupRule] " +
                "ON [GroupRule].ProductGroupID = [ProductGroup].ID " +
                "AND [GroupRule].Deleted = 0 " +
                "AND [GroupRule].RuleLocale = @Locale " +
                "WHERE [Product].ID = @Id",
                (product, productWriteOffRule, productProductGroup, productGroup, groupRule) => {
                    if (productWriteOffRule != null) product.ProductWriteOffRules.Add(productWriteOffRule);

                    if (productProductGroup == null || groupRule == null) return product;

                    productGroup.ProductWriteOffRules.Add(groupRule);

                    productProductGroup.ProductGroup = productGroup;

                    product.ProductProductGroups.Add(productProductGroup);

                    return product;
                },
                new { Id = id, Locale = locale }
            )
            .SingleOrDefault();
    }

    public ProductAvailabilityModel GetAllProductAvailabilities(
        Guid productNetId,
        Guid clientAgreementNetId,
        Guid saleNetId) {
        ProductAvailabilityModel toReturn = new() {
            TotalAvailabilities = _connection.Query<TypeProductAvailability, double, KeyValuePair<TypeProductAvailability, double>>(
                    "DECLARE @IsVat bit = " +
                    "(SELECT [Agreement].[WithVATAccounting] FROM [ClientAgreement] " +
                    "LEFT JOIN [Agreement] " +
                    "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
                    "WHERE [ClientAgreement].[NetUID] = @ClientAgreementNetId); " +
                    "SELECT " +
                    "0 AS [Name] " +
                    ",CASE " +
                    "WHEN SUM([ProductReservation].[Qty]) IS NULL " +
                    "THEN 0 " +
                    "ELSE SUM([ProductReservation].[Qty]) " +
                    "END AS [Amount] " +
                    "FROM [ProductReservation] " +
                    "LEFT JOIN [OrderItem] " +
                    "ON [OrderItem].[ID] = [ProductReservation].[OrderItemID] " +
                    "LEFT JOIN [Product] " +
                    "ON [Product].[ID] = [OrderItem].[ProductID] " +
                    "LEFT JOIN [Order] " +
                    "ON [Order].[ID] = [OrderItem].[OrderID] " +
                    "LEFT JOIN [Sale] " +
                    "ON [Sale].[OrderID] = [Order].[ID] " +
                    "LEFT JOIN [ClientAgreement] " +
                    "ON [ClientAgreement].[ID] = [Order].[ClientAgreementID] " +
                    "LEFT JOIN [Agreement] " +
                    "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
                    "LEFT JOIN [Organization] " +
                    "ON [Organization].[ID] = [Agreement].[OrganizationID] " +
                    "WHERE [ProductReservation].[Deleted] = 0 " +
                    "AND [Product].[NetUID] = @NetId " +
                    "AND [Organization].[Culture] = @Culture " +
                    "AND [Agreement].[WithVATAccounting] = @IsVat " +
                    "AND [Sale].[ChangedToInvoice] IS NULL " +
                    "AND [Sale].[NetUID] != @SaleNetId " +
                    "UNION " +
                    "SELECT " +
                    "1 AS [Name] " +
                    ", CASE " +
                    "WHEN SUM([ProductAvailability].[Amount]) IS NULL " +
                    "THEN 0 " +
                    "ELSE SUM([ProductAvailability].[Amount]) " +
                    "END AS [Amount] " +
                    "FROM [ProductAvailability] " +
                    "LEFT JOIN [Storage] " +
                    "ON [Storage].[ID] = [ProductAvailability].[StorageID] " +
                    "LEFT JOIN [Product] " +
                    "ON [Product].[ID] = [ProductAvailability].[ProductID] " +
                    "WHERE [Product].[NetUID] = @NetId " +
                    "AND [ProductAvailability].[Deleted] = 0 " +
                    "AND [Storage].[Deleted] = 0 " +
                    "AND [Storage].[Locale] = 'uk' " +
                    "AND [Storage].[ForVatProducts] = 1 " +
                    "AND [Storage].[AvailableForReSale] = 0 " +
                    "AND [Storage].[ForDefective] = 0 " +
                    "UNION " +
                    "SELECT " +
                    "6 AS [Name] " +
                    ", CASE " +
                    "WHEN SUM([ProductAvailability].[Amount]) IS NULL " +
                    "THEN 0 " +
                    "ELSE SUM([ProductAvailability].[Amount]) " +
                    "END AS [Amount] " +
                    "FROM [ProductAvailability] " +
                    "LEFT JOIN [Storage] " +
                    "ON [Storage].[ID] = [ProductAvailability].[StorageID] " +
                    "LEFT JOIN [Product] " +
                    "ON [Product].[ID] = [ProductAvailability].[ProductID] " +
                    "WHERE [Product].[NetUID] = @NetId " +
                    "AND [ProductAvailability].[Deleted] = 0 " +
                    "AND [Storage].[Deleted] = 0 " +
                    "AND [Storage].[Locale] = 'uk' " +
                    "AND [Storage].[ForVatProducts] = 1 " +
                    "AND [Storage].[AvailableForReSale] = 1 " +
                    "AND [Storage].[ForDefective] = 0 " +
                    "UNION " +
                    "SELECT " +
                    "2 AS [Name] " +
                    ", CASE " +
                    "WHEN SUM([ProductAvailability].[Amount]) IS NULL " +
                    "THEN 0 " +
                    "ELSE SUM([ProductAvailability].[Amount]) " +
                    "END AS [Amount] " +
                    "FROM [ProductAvailability] " +
                    "LEFT JOIN [Storage] " +
                    "ON [Storage].[ID] = [ProductAvailability].[StorageID] " +
                    "LEFT JOIN [Product] " +
                    "ON [Product].[ID] = [ProductAvailability].[ProductID] " +
                    "WHERE [Product].[NetUID] = @NetId " +
                    "AND [ProductAvailability].[Deleted] = 0 " +
                    "AND [Storage].[Deleted] = 0 " +
                    "AND [Storage].[Locale] = 'uk' " +
                    "AND [Storage].[ForVatProducts] = 0 " +
                    "AND [Storage].[ForDefective] = 0 " +
                    "UNION " +
                    "SELECT " +
                    "3 AS [Name] " +
                    ", CASE " +
                    "WHEN SUM([ProductAvailability].[Amount]) IS NULL " +
                    "THEN 0 " +
                    "ELSE SUM([ProductAvailability].[Amount]) " +
                    "END AS [Amount] " +
                    "FROM [ProductAvailability] " +
                    "LEFT JOIN [Storage] " +
                    "ON [Storage].[ID] = [ProductAvailability].[StorageID] " +
                    "LEFT JOIN [Product] " +
                    "ON [Product].[ID] = [ProductAvailability].[ProductID] " +
                    "WHERE [Product].[NetUID] = @NetId " +
                    "AND [ProductAvailability].[Deleted] = 0 " +
                    "AND [Storage].[Deleted] = 0 " +
                    "AND [Storage].[Locale] = 'pl' " +
                    "AND [Storage].[ForDefective] = 0 " +
                    "UNION " +
                    "SELECT " +
                    "4 AS [Name] " +
                    ", CASE " +
                    "WHEN SUM([PackingListPackageOrderItem].[Qty]) IS NULL " +
                    "THEN 0 " +
                    "ELSE SUM([PackingListPackageOrderItem].[Qty]) " +
                    "END AS [Amount] " +
                    "FROM [PackingListPackageOrderItem] " +
                    "LEFT JOIN [PackingList] " +
                    "ON [PackingList].[ID] = [PackingListPackageOrderItem].[PackingListID] " +
                    "LEFT JOIN [SupplyInvoice] " +
                    "ON [SupplyInvoice].[ID] = [PackingList].[SupplyInvoiceID] " +
                    "LEFT JOIN [SupplyOrder] " +
                    "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
                    "LEFT JOIN [Organization] " +
                    "ON [Organization].[ID] = [SupplyOrder].[OrganizationID] " +
                    "LEFT JOIN [SupplyInvoiceOrderItem] " +
                    "ON [SupplyInvoiceOrderItem].[SupplyInvoiceID] = [SupplyInvoice].[ID] " +
                    "LEFT JOIN [SupplyOrderItem] " +
                    "ON [SupplyOrderItem].[ID] = [SupplyInvoiceOrderItem].[SupplyOrderItemID] " +
                    "LEFT JOIN [Product] " +
                    "ON [Product].[ID] = [SupplyInvoiceOrderItem].[ProductID] " +
                    "LEFT JOIN [DeliveryProductProtocol] " +
                    "ON [DeliveryProductProtocol].[ID] = [SupplyInvoice].[DeliveryProductProtocolID] " +
                    "WHERE [PackingListPackageOrderItem].[Deleted] = 0 " +
                    "AND [SupplyOrder].[Deleted] = 0 " +
                    "AND [PackingList].[Deleted] = 0 " +
                    "AND [SupplyInvoice].[Deleted] = 0 " +
                    "AND [SupplyInvoiceOrderItem].[Deleted] = 0 " +
                    "AND [SupplyOrderItem].[Deleted] = 0 " +
                    "AND ([SupplyOrder].[IsOrderShipped] = 1 OR " +
                    "[DeliveryProductProtocol].[IsShipped] = 1) " +
                    "AND [Product].[NetUID] = @NetId " +
                    "AND [Organization].[Culture] = 'pl' " +
                    "AND [PackingListPackageOrderItem].[IsPlaced] = 0 " +
                    "UNION " +
                    "SELECT " +
                    "5 AS [Name] " +
                    ",( " +
                    "(SELECT " +
                    "CASE " +
                    "WHEN SUM([PackingListPackageOrderItem].[Qty]) IS NULL " +
                    "THEN 0 " +
                    "ELSE SUM([PackingListPackageOrderItem].[Qty]) " +
                    "END AS [Amount] " +
                    "FROM [PackingListPackageOrderItem] " +
                    "LEFT JOIN [PackingList] " +
                    "ON [PackingList].[ID] = [PackingListPackageOrderItem].[PackingListID] " +
                    "LEFT JOIN [SupplyInvoice] " +
                    "ON [SupplyInvoice].[ID] = [PackingList].[SupplyInvoiceID] " +
                    "LEFT JOIN [SupplyOrder] " +
                    "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
                    "LEFT JOIN [Organization] " +
                    "ON [Organization].[ID] = [SupplyOrder].[OrganizationID] " +
                    "LEFT JOIN [SupplyInvoiceOrderItem] " +
                    "ON [SupplyInvoiceOrderItem].[SupplyInvoiceID] = [SupplyInvoice].[ID] " +
                    "LEFT JOIN [SupplyOrderItem] " +
                    "ON [SupplyOrderItem].[ID] = [SupplyInvoiceOrderItem].[SupplyOrderItemID] " +
                    "LEFT JOIN [Product] " +
                    "ON [Product].[ID] = [SupplyInvoiceOrderItem].[ProductID] " +
                    "LEFT JOIN [DeliveryProductProtocol] " +
                    "ON [DeliveryProductProtocol].[ID] = [SupplyInvoice].[DeliveryProductProtocolID] " +
                    "WHERE [PackingListPackageOrderItem].[Deleted] = 0 " +
                    "AND [SupplyOrder].[Deleted] = 0 " +
                    "AND [PackingList].[Deleted] = 0 " +
                    "AND [SupplyInvoice].[Deleted] = 0 " +
                    "AND [SupplyInvoiceOrderItem].[Deleted] = 0 " +
                    "AND [SupplyOrderItem].[Deleted] = 0 " +
                    "AND ([SupplyOrder].[IsOrderShipped] = 1 OR " +
                    "[DeliveryProductProtocol].[IsShipped] = 1) " +
                    "AND [Product].[NetUID] = @NetId " +
                    "AND [Organization].[Culture] = 'uk' " +
                    "AND [PackingListPackageOrderItem].[IsPlaced] = 0) " +
                    "+ " +
                    "(SELECT " +
                    "CASE " +
                    "WHEN SUM([TaxFreeItem].[Qty]) IS NULL " +
                    "THEN 0 " +
                    "ELSE SUM([TaxFreeItem].[Qty]) " +
                    "END AS [Amount] " +
                    "FROM [TaxFreeItem] " +
                    "LEFT JOIN [TaxFree] " +
                    "ON [TaxFree].[ID] = [TaxFreeItem].[TaxFreeID] " +
                    "LEFT JOIN [SupplyOrderUkraineCartItem] " +
                    "ON [SupplyOrderUkraineCartItem].[ID] = [TaxFreeItem].[SupplyOrderUkraineCartItemID] " +
                    "LEFT JOIN [Product] " +
                    "ON [Product].[ID] = [SupplyOrderUkraineCartItem].[ProductID] " +
                    "WHERE [TaxFreeItem].[Deleted] = 0 " +
                    "AND [TaxFree].[Deleted] = 0 " +
                    "AND [SupplyOrderUkraineCartItem].[Deleted] = 0 " +
                    "AND [Product].[NetUID] = @NetId) " +
                    "+ " +
                    "(SELECT " +
                    "CASE " +
                    "WHEN SUM([SadItem].[Qty]) IS NULL " +
                    "THEN 0 " +
                    "ELSE SUM([SadItem].[Qty]) " +
                    "END AS [Amount] " +
                    "FROM [SadItem] " +
                    "LEFT JOIN [Sad] " +
                    "ON [Sad].[ID] = [SadItem].[SadID] " +
                    "LEFT JOIN [SupplyOrderUkraineCartItem] " +
                    "ON [SupplyOrderUkraineCartItem].[ID] = [SadItem].[SupplyOrderUkraineCartItemID] " +
                    "LEFT JOIN [Product] " +
                    "ON [Product].[ID] = [SupplyOrderUkraineCartItem].[ProductID] " +
                    "WHERE [SadItem].[Deleted] = 0 " +
                    "AND [Sad].[Deleted] = 0 " +
                    "AND [SupplyOrderUkraineCartItem].[Deleted] = 0 " +
                    "AND [Product].[NetUID] = @NetId) " +
                    ") AS [Amount] ",
                    (key, value) => new KeyValuePair<TypeProductAvailability, double>(key, value),
                    new {
                        NetId = productNetId,
                        Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower(),
                        ClientAgreementNetId = clientAgreementNetId,
                        SaleNetId = saleNetId
                    },
                    splitOn: "Name,Amount")
                .ToDictionary(x => x.Key, x => x.Value)
        };


        _connection.Query<TypeProductAvailability, Guid, string, double, TypeProductAvailability>(
            "DECLARE @IsVat bit = " +
            "(SELECT [Agreement].[WithVATAccounting] FROM [ClientAgreement] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "WHERE [ClientAgreement].[NetUID] = @ClientAgreementNetId); " +
            "SELECT " +
            "0 AS [Type] " +
            ",[Sale].[NetUID] AS [NetId] " +
            ",[SaleNumber].[Value] AS [Value] " +
            ",CASE " +
            "WHEN SUM([ProductReservation].[Qty]) IS NULL " +
            "THEN 0 " +
            "ELSE SUM([ProductReservation].[Qty]) " +
            "END AS [Amount] " +
            "FROM [ProductReservation] " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].[ID] = [ProductReservation].[OrderItemID] " +
            "LEFT JOIN [Product] " +
            "ON [Product].[ID] = [OrderItem].[ProductID] " +
            "LEFT JOIN [Order] " +
            "ON [Order].[ID] = [OrderItem].[OrderID] " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].[OrderID] = [Order].[ID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [Order].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].[ID] = [Agreement].[OrganizationID] " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].[ID] = [Sale].[SaleNumberID] " +
            "WHERE [ProductReservation].[Deleted] = 0 " +
            "AND [Product].[NetUID] = @NetId " +
            "AND [Organization].[Culture] = @Culture " +
            "AND [Agreement].[WithVATAccounting] = @IsVat " +
            "AND [Sale].[ChangedToInvoice] IS NULL " +
            "AND [Sale].[NetUID] != @SaleNetId " +
            "GROUP BY [Sale].[NetUID] " +
            ",[SaleNumber].[Value] " +
            "UNION " +
            "SELECT " +
            "1 AS [Type] " +
            ",[Storage].[NetUID] AS [NetId] " +
            ",[Storage].[Name] AS [Value] " +
            ", CASE " +
            "WHEN SUM([ProductAvailability].[Amount]) IS NULL " +
            "THEN 0 " +
            "ELSE SUM([ProductAvailability].[Amount]) " +
            "END AS [Amount] " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].[ID] = [ProductAvailability].[StorageID] " +
            "LEFT JOIN [Product] " +
            "ON [Product].[ID] = [ProductAvailability].[ProductID] " +
            "WHERE [Product].[NetUID] = @NetId " +
            "AND [ProductAvailability].[Deleted] = 0 " +
            "AND [ProductAvailability].[Amount] > 0 " +
            "AND [Storage].[Deleted] = 0 " +
            "AND [Storage].[Locale] = 'uk' " +
            "AND [Storage].[ForVatProducts] = 1 " +
            "AND [Storage].[ForDefective] = 0 " +
            "AND [Storage].[AvailableForReSale] = 0 " +
            "GROUP BY [Storage].[NetUID] " +
            ",[Storage].[Name] " +
            "UNION " +
            "SELECT " +
            "2 AS [Type] " +
            ",[Storage].[NetUID] AS [NetId] " +
            ",[Storage].[Name] AS [Value] " +
            ", CASE " +
            "WHEN SUM([ProductAvailability].[Amount]) IS NULL " +
            "THEN 0 " +
            "ELSE SUM([ProductAvailability].[Amount]) " +
            "END AS [Amount] " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].[ID] = [ProductAvailability].[StorageID] " +
            "LEFT JOIN [Product] " +
            "ON [Product].[ID] = [ProductAvailability].[ProductID] " +
            "WHERE [Product].[NetUID] = @NetId " +
            "AND [ProductAvailability].[Deleted] = 0 " +
            "AND [ProductAvailability].[Amount] > 0 " +
            "AND [Storage].[Deleted] = 0 " +
            "AND [Storage].[Locale] = 'uk' " +
            "AND [Storage].[ForVatProducts] = 0 " +
            "AND [Storage].[ForDefective] = 0 " +
            "GROUP BY [Storage].[NetUID] " +
            ",[Storage].[Name] " +
            "UNION " +
            "SELECT " +
            "3 AS [Type] " +
            ",[Storage].[NetUID] AS [NetId] " +
            ",[Storage].[Name] AS [Value] " +
            ", CASE " +
            "WHEN SUM([ProductAvailability].[Amount]) IS NULL " +
            "THEN 0 " +
            "ELSE SUM([ProductAvailability].[Amount]) " +
            "END AS [Amount] " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].[ID] = [ProductAvailability].[StorageID] " +
            "LEFT JOIN [Product] " +
            "ON [Product].[ID] = [ProductAvailability].[ProductID] " +
            "WHERE [Product].[NetUID] = @NetId " +
            "AND [ProductAvailability].[Deleted] = 0 " +
            "AND [ProductAvailability].[Amount] > 0 " +
            "AND [Storage].[Deleted] = 0 " +
            "AND [Storage].[Locale] = 'pl' " +
            "AND [Storage].[ForDefective] = 0 " +
            "GROUP BY [Storage].[NetUID] " +
            ",[Storage].[Name] " +
            "UNION " +
            "SELECT " +
            "4 AS [Type] " +
            ",[SupplyInvoice].[NetUID] AS [NetId] " +
            ", N'Інвойс ' + [SupplyInvoice].[Number] AS [Value] " +
            ", CASE " +
            "WHEN SUM([PackingListPackageOrderItem].[Qty]) IS NULL " +
            "THEN 0 " +
            "ELSE SUM([PackingListPackageOrderItem].[Qty]) " +
            "END AS [Amount] " +
            "FROM [PackingListPackageOrderItem] " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].[ID] = [PackingListPackageOrderItem].[PackingListID] " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [PackingList].[SupplyInvoiceID] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].[ID] = [SupplyOrder].[OrganizationID] " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].[SupplyInvoiceID] = [SupplyInvoice].[ID] " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].[ID] = [SupplyInvoiceOrderItem].[SupplyOrderItemID] " +
            "LEFT JOIN [Product] " +
            "ON [Product].[ID] = [SupplyInvoiceOrderItem].[ProductID] " +
            "LEFT JOIN [DeliveryProductProtocol] " +
            "ON [DeliveryProductProtocol].[ID] = [SupplyInvoice].[DeliveryProductProtocolID] " +
            "WHERE [PackingListPackageOrderItem].[Deleted] = 0 " +
            "AND [SupplyOrder].[Deleted] = 0 " +
            "AND [PackingList].[Deleted] = 0 " +
            "AND [SupplyInvoice].[Deleted] = 0 " +
            "AND [SupplyInvoiceOrderItem].[Deleted] = 0 " +
            "AND [SupplyOrderItem].[Deleted] = 0 " +
            "AND ([SupplyOrder].[IsOrderShipped] = 1 OR " +
            "[DeliveryProductProtocol].[IsShipped] = 1) " +
            "AND [Product].[NetUID] = @NetId " +
            "AND [Organization].[Culture] = 'pl' " +
            "AND [PackingListPackageOrderItem].[IsPlaced] = 0 " +
            "GROUP BY [SupplyInvoice].[NetUID] " +
            ",[SupplyInvoice].[Number] " +
            "UNION " +
            "SELECT " +
            "5 AS [Type] " +
            ",[SupplyInvoice].[NetUID] AS [NetId] " +
            ",N'Інвойс ' + [SupplyInvoice].[Number] AS [Value] " +
            ", CASE " +
            "WHEN SUM([PackingListPackageOrderItem].[Qty]) IS NULL " +
            "THEN 0 " +
            "ELSE SUM([PackingListPackageOrderItem].[Qty]) " +
            "END AS [Amount] " +
            "FROM [PackingListPackageOrderItem] " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].[ID] = [PackingListPackageOrderItem].[PackingListID] " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].[ID] = [PackingList].[SupplyInvoiceID] " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].[ID] = [SupplyInvoice].[SupplyOrderID] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].[ID] = [SupplyOrder].[OrganizationID] " +
            "LEFT JOIN [SupplyInvoiceOrderItem] " +
            "ON [SupplyInvoiceOrderItem].[SupplyInvoiceID] = [SupplyInvoice].[ID] " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].[ID] = [SupplyInvoiceOrderItem].[SupplyOrderItemID] " +
            "LEFT JOIN [Product] " +
            "ON [Product].[ID] = [SupplyInvoiceOrderItem].[ProductID] " +
            "LEFT JOIN [DeliveryProductProtocol] " +
            "ON [DeliveryProductProtocol].[ID] = [SupplyInvoice].[DeliveryProductProtocolID] " +
            "WHERE [PackingListPackageOrderItem].[Deleted] = 0 " +
            "AND [SupplyOrder].[Deleted] = 0 " +
            "AND [PackingList].[Deleted] = 0 " +
            "AND [SupplyInvoice].[Deleted] = 0 " +
            "AND [SupplyInvoiceOrderItem].[Deleted] = 0 " +
            "AND [SupplyOrderItem].[Deleted] = 0 " +
            "AND ([SupplyOrder].[IsOrderShipped] = 1 OR " +
            "[DeliveryProductProtocol].[IsShipped] = 1) " +
            "AND [Product].[NetUID] = @NetId " +
            "AND [Organization].[Culture] = 'uk' " +
            "AND [PackingListPackageOrderItem].[IsPlaced] = 0 " +
            "GROUP BY [SupplyInvoice].[NetUID] " +
            ",[SupplyInvoice].[Number] " +
            "UNION " +
            "SELECT " +
            "5 AS [Type] " +
            ",[TaxFree].[NetUID] AS [NetId] " +
            ",'TaxFree ' + [TaxFree].[Number] AS [Value] " +
            ",CASE " +
            "WHEN SUM([TaxFreeItem].[Qty]) IS NULL " +
            "THEN 0 " +
            "ELSE SUM([TaxFreeItem].[Qty]) " +
            "END AS [Amount] " +
            "FROM [TaxFreeItem] " +
            "LEFT JOIN [TaxFree] " +
            "ON [TaxFree].[ID] = [TaxFreeItem].[TaxFreeID] " +
            "LEFT JOIN [SupplyOrderUkraineCartItem] " +
            "ON [SupplyOrderUkraineCartItem].[ID] = [TaxFreeItem].[SupplyOrderUkraineCartItemID] " +
            "LEFT JOIN [Product] " +
            "ON [Product].[ID] = [SupplyOrderUkraineCartItem].[ProductID] " +
            "WHERE [TaxFreeItem].[Deleted] = 0 " +
            "AND [TaxFree].[Deleted] = 0 " +
            "AND [SupplyOrderUkraineCartItem].[Deleted] = 0 " +
            "AND [Product].[NetUID] = @NetId " +
            "GROUP BY [TaxFree].[NetUID] " +
            ",[TaxFree].[Number] " +
            "UNION " +
            "SELECT " +
            "5 AS [Type] " +
            ",[Sad].[NetUID] AS [NetId] " +
            ",'Sad ' + [Sad].[Number] AS [Value] " +
            ",CASE " +
            "WHEN SUM([SadItem].[Qty]) IS NULL " +
            "THEN 0 " +
            "ELSE SUM([SadItem].[Qty]) " +
            "END AS [Amount] " +
            "FROM [SadItem] " +
            "LEFT JOIN [Sad] " +
            "ON [Sad].[ID] = [SadItem].[SadID] " +
            "LEFT JOIN [SupplyOrderUkraineCartItem] " +
            "ON [SupplyOrderUkraineCartItem].[ID] = [SadItem].[SupplyOrderUkraineCartItemID] " +
            "LEFT JOIN [Product] " +
            "ON [Product].[ID] = [SupplyOrderUkraineCartItem].[ProductID] " +
            "WHERE [SadItem].[Deleted] = 0 " +
            "AND [Sad].[Deleted] = 0 " +
            "AND [SupplyOrderUkraineCartItem].[Deleted] = 0 " +
            "AND [Product].[NetUID] = @NetId " +
            "GROUP BY [Sad].[NetUID] " +
            ",[Sad].[Number] " +
            "UNION " +
            "SELECT " +
            "6 AS [Type] " +
            ",[Storage].[NetUID] AS [NetId] " +
            ",[Storage].[Name] AS [Value] " +
            ", CASE " +
            "WHEN SUM([ProductAvailability].[Amount]) IS NULL " +
            "THEN 0 " +
            "ELSE SUM([ProductAvailability].[Amount]) " +
            "END AS [Amount] " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].[ID] = [ProductAvailability].[StorageID] " +
            "LEFT JOIN [Product] " +
            "ON [Product].[ID] = [ProductAvailability].[ProductID] " +
            "WHERE [Product].[NetUID] = @NetId " +
            "AND [ProductAvailability].[Deleted] = 0 " +
            "AND [ProductAvailability].[Amount] > 0 " +
            "AND [Storage].[Deleted] = 0 " +
            "AND [Storage].[ForDefective] = 0 " +
            "AND [Storage].[ForVatProducts] = 1 " +
            "AND [Storage].[AvailableForReSale] = 1 " +
            "GROUP BY [Storage].[NetUID] " +
            ",[Storage].[Name] ",
            (type, netId, name, amount) => {
                AvailabilityModel availability =
                    new() {
                        Amount = amount,
                        Name = name,
                        NetId = netId
                    };

                switch (type) {
                    case TypeProductAvailability.InAccount:
                        toReturn.InAccounts.Add(availability);
                        break;
                    case TypeProductAvailability.StoragePl:
                        toReturn.InStoragePl.Add(availability);
                        break;
                    case TypeProductAvailability.StorageUkrVat:
                        toReturn.InStorageUkrVat.Add(availability);
                        break;
                    case TypeProductAvailability.OnWayToPl:
                        toReturn.OnWayToPl.Add(availability);
                        break;
                    case TypeProductAvailability.OnWayToUkr:
                        toReturn.OnWayToUkr.Add(availability);
                        break;
                    case TypeProductAvailability.StorageUkrNotVat:
                        toReturn.InStorageUkrNotVat.Add(availability);
                        break;
                    case TypeProductAvailability.AvailableQtyUkReSale:
                        toReturn.AvailableQtyUkReSale.Add(availability);
                        break;
                }

                return type;
            }, new {
                NetId = productNetId,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower(),
                ClientAgreementNetId = clientAgreementNetId,
                SaleNetId = saleNetId
            }, splitOn: "Type,NetId,Value,Amount");

        _connection.Query<Guid, string, OrderItem, User, Guid>(
            "DECLARE @IsVat bit = " +
            "(SELECT [Agreement].[WithVATAccounting] FROM [ClientAgreement] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "WHERE [ClientAgreement].[NetUID] = @ClientAgreementNetId); " +
            ";WITH [AVAILABLE_ITEM_QTY] AS ( " +
            "SELECT " +
            "[Sale].[NetUID] AS [NetId] " +
            ",CASE " +
            "WHEN SUM([ProductReservation].[Qty]) IS NULL " +
            "THEN 0 " +
            "ELSE SUM([ProductReservation].[Qty]) " +
            "END AS [Amount] " +
            ", [OrderItem].[ID] " +
            ", [RegionCode].[Value] AS [RegionCode] " +
            "FROM [ProductReservation] " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].[ID] = [ProductReservation].[OrderItemID] " +
            "LEFT JOIN [Product] " +
            "ON [Product].[ID] = [OrderItem].[ProductID] " +
            "LEFT JOIN [Order] " +
            "ON [Order].[ID] = [OrderItem].[OrderID] " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].[OrderID] = [Order].[ID] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].[ID] = [Order].[ClientAgreementID] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "LEFT JOIN  [Client] " +
            "ON [Client].[ID] = [ClientAgreement].[ClientID] " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].[ID] = [Client].[RegionCodeID] " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].[ID] = [Agreement].[OrganizationID] " +
            "WHERE [ProductReservation].[Deleted] = 0 " +
            "AND [Product].[NetUID] = @NetId " +
            "AND [Organization].[Culture] = @Culture " +
            "AND [Agreement].[WithVATAccounting] = @IsVat " +
            "AND [Sale].[ChangedToInvoice] IS NULL " +
            "AND [Sale].[NetUID] != @SaleNetId " +
            "GROUP BY [Sale].[NetUID] " +
            ", [OrderItem].[ID] " +
            ", [RegionCode].[Value] " +
            ") " +
            "SELECT " +
            "[AVAILABLE_ITEM_QTY].[NetId] " +
            ", [AVAILABLE_ITEM_QTY].[RegionCode] " +
            ", [OrderItem].* " +
            ", [User].* " +
            "FROM [AVAILABLE_ITEM_QTY] " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].[NetUID] = [AVAILABLE_ITEM_QTY].[NetId] " +
            "LEFT JOIN [SaleNumber] " +
            "ON [SaleNumber].[ID] = [Sale].[SaleNumberID] " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].[ID] = [AVAILABLE_ITEM_QTY].[ID] " +
            "LEFT JOIN [User] " +
            "ON [User].[ID] = [OrderItem].[UserId] ",
            (netId, regionCode, orderItem, user) => {
                AvailabilityModel model = toReturn.InAccounts.FirstOrDefault(x => x.NetId.Equals(netId));

                if (model == null) return netId;

                orderItem.User = user;

                model.OrderItem = orderItem;

                model.RegionCode = regionCode;
                return netId;
            }, new {
                NetId = productNetId,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower(),
                ClientAgreementNetId = clientAgreementNetId,
                SaleNetId = saleNetId
            }, splitOn: "NetId,RegionCode,ID,ID");
        List<AvailabilityModel> availabilityInvoiceModel = new();

        var SupplyOrderItemData = _connection.Query(
            "SELECT " +
            "[SupplyOrderItem].Qty as [Amount], " +
            "[SupplyOrderNumber].Number as [SONumber], " +
            "[SupplyInvoice].Number as [SINumber], " +
            "FORMAT([SupplyInvoice].Created, 'dd.MM.yyyy') as [Created] " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON [SupplyOrderItem].SupplyOrderID = [SupplyOrder].ID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].SupplyOrderNumberID " +
            "INNER JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].SupplyOrderID = [SupplyOrder].ID " +
            "LEFT JOIN Product " +
            "ON Product.ID = SupplyOrderItem.ProductID " +
            "WHERE SupplyOrder.IsFullyPlaced = 0 " +
            "AND [Product].[NetUID] = @NetId ",
            new {
                NetId = productNetId
            }).Select(x => new {
            Amount = (double)x.Amount,
            SONumber = (string)x.SONumber,
            SINumber = (string)x.SINumber,
            Created = (string)x.Created
        }).ToList();
        foreach (var item in SupplyOrderItemData)
            availabilityInvoiceModel.Add(new AvailabilityModel {
                Amount = item.Amount,
                Name = item.SINumber + " , " + item.Created
            });
        var SupplyOrderUkraineItemData = _connection.Query(
            "SELECT [SupplyOrderUkraineItem].Qty as [Amount]," +
            "[SupplyOrderUkraine].InvNumber as [SONumber], " +
            "FORMAT([SupplyOrderUkraine].Created, 'dd.MM.yyyy') as [Created] " +
            "FROM [SupplyOrderUkraine] " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].SupplyOrderUkraineID = [SupplyOrderUkraine].ID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [SupplyOrderUkraineItem].ProductID " +
            "LEFT JOIN [Client] AS [Supplier] " +
            "ON [Supplier].ID = [SupplyOrderUkraineItem].SupplierID " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].[ID] = [SupplyOrderUkraineItem].[ProductSpecificationID] " +
            "LEFT JOIN [MeasureUnit] " +
            "ON [MeasureUnit].[ID] = [Product].[MeasureUnitID] " +
            "WHERE [SupplyOrderUkraineItem].Deleted = 0 " +
            "AND [SupplyOrderUkraine].IsPlaced = 0 " +
            "AND [Product].[NetUID] = @NetId",
            new {
                NetId = productNetId
            }).Select(x => new {
            Amount = (double)x.Amount,
            SONumber = (string)x.SONumber,
            Created = (string)x.Created
        }).ToList();

        foreach (var item in SupplyOrderUkraineItemData)
            availabilityInvoiceModel.Add(new AvailabilityModel {
                Amount = item.Amount,
                Name = item.SONumber + " , " + item.Created
            });

        toReturn.AvailabilityInvoiceModel = availabilityInvoiceModel;
        toReturn.TotalAvailabilities.Add(TypeProductAvailability.AvailabilityInvoice, toReturn.AvailabilityInvoiceModel.Sum(x => x.Amount));
        return toReturn;
    }

    public Product GetProductByVendorCodeWithWriteOffRule(string vendorCode) {
        Product product = null;

        _connection.Query<Product, ProductWriteOffRule, Product>(
            "SELECT * FROM [Product] " +
            "LEFT JOIN [ProductWriteOffRule] " +
            "ON [ProductWriteOffRule].[ProductID] = [Product].[ID] " +
            "WHERE [Product].[VendorCode] = @VendorCode " +
            "OR [Product].[SearchVendorCode] = @VendorCode ",
            (productFromDb, productWriteRule) => {
                if (product == null)
                    product = productFromDb;

                if (productWriteRule != null)
                    product.ProductWriteOffRules.Add(productWriteRule);
                return productFromDb;
            },
            new { VendorCode = vendorCode }
        );

        if (product == null) return null;

        _connection.Query<ProductProductGroup, ProductGroup, ProductWriteOffRule, ProductProductGroup>(
            "SELECT * FROM [ProductProductGroup] " +
            "LEFT JOIN [ProductGroup] " +
            "ON [ProductGroup].[ID] = [ProductProductGroup].[ProductGroupID] " +
            "LEFT JOIN [ProductWriteOffRule] " +
            "ON [ProductWriteOffRule].[ProductGroupID] = [ProductGroup].[ID] " +
            "WHERE [ProductProductGroup].[ProductID] = @ProductID ",
            (productProductGroup, productGroup, productWriteOffRule) => {
                if (!product.ProductProductGroups.Any(x => x.Id.Equals(productProductGroup.Id))) {
                    product.ProductProductGroups.Add(productProductGroup);

                    productProductGroup.ProductGroup = productGroup;
                } else {
                    productProductGroup = product.ProductProductGroups.First(x => x.Id.Equals(productProductGroup.Id));
                }

                if (productWriteOffRule != null && !productGroup.ProductWriteOffRules.Any(x => x.Id.Equals(productWriteOffRule.Id)))
                    productGroup.ProductWriteOffRules.Add(productWriteOffRule);

                return productProductGroup;
            },
            new { ProductID = product.Id }
        );

        return product;
    }
}