using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Dapper;
using GBA.Common.Extensions;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers;
using GBA.Domain.EntityHelpers.ProductModels;
using GBA.Domain.FilterEntities;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class GetMultipleProductsRepository : IGetMultipleProductsRepository {
    private static readonly Regex SpecialCharactersReplace = new("[$&+,:;=?@#|/\\\\'\"’<>. ^*()%!\\-]", RegexOptions.Compiled);

    private readonly IDbConnection _connection;

    public GetMultipleProductsRepository(IDbConnection connection) {
        _connection = connection;
    }

    public List<OrderItem> GetAllOrderedProductsFiltered(
        DateTime from,
        DateTime to,
        long limit,
        long offset,
        Guid clientNetId,
        Guid activeClientAgreementNetId,
        long? currencyId,
        long? organizationId,
        bool withVat) {
        List<OrderItem> orderItems = new();

        string sqlExpression =
            ";WITH [GroupedOrderedProducts_CTE] " +
            "AS ( " +
            "SELECT [Product].ID " +
            ", SUM([OrderItem].Qty) AS [OrderedQty] " +
            "FROM [Product] " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ProductID = [Product].ID " +
            "AND [OrderItem].Qty > 0 " +
            "AND [OrderItem].Deleted = 0 " +
            "LEFT JOIN [Order] " +
            "ON [Order].ID = [OrderItem].OrderID " +
            "LEFT JOIN [Sale] " +
            "ON [Sale].OrderID = [Order].ID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [Sale].ClientAgreementID = [ClientAgreement].ID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "WHERE [Sale].ChangedToInvoice IS NOT NULL " +
            "AND [Sale].ChangedToInvoice >= @From " +
            "AND [Sale].ChangedToInvoice <= @To " +
            "AND [Client].NetUID = @NetId " +
            "GROUP BY [Product].ID " +
            "), " +
            "[RowedOrderedProducts_CTE] " +
            "AS ( " +
            "SELECT [GroupedOrderedProducts_CTE].ID " +
            ", [GroupedOrderedProducts_CTE].OrderedQty " +
            ", ROW_NUMBER() OVER(ORDER BY [GroupedOrderedProducts_CTE].OrderedQty DESC) AS [RowNumber] " +
            "FROM [GroupedOrderedProducts_CTE] " +
            ") " +
            "SELECT 0 AS ID " +
            ", [RowedOrderedProducts_CTE].OrderedQty AS [Qty] " +
            ", [Product].ID " +
            ", [Product].Created " +
            ", [Product].Deleted ";

        sqlExpression += ProductSqlFragments.ProductCultureColumns;

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
            ", [Product].Size " +
            ", [Product].[Top] " +
            ", [Product].UCGFEA " +
            ", [Product].Updated " +
            ", [Product].VendorCode " +
            ", [Product].Volume " +
            ", [Product].[Weight] " +
            ", dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS [CurrentPrice] " +
            ", dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS [CurrentLocalPrice] " +
            ", (SELECT Code FROM Currency " +
            "WHERE ID = @CurrencyId " +
            "AND Deleted = 0) AS CurrencyCode " +
            ", [ProductAvailability].* " +
            ", [Storage].* " +
            ", [MeasureUnit].* " +
            ", [ProductSlug].* " +
            ", [ProductImage].* " +
            "FROM [RowedOrderedProducts_CTE] " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [RowedOrderedProducts_CTE].ID " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [ProductSlug] " +
            "ON [ProductSlug].ID = (" +
            "SELECT TOP(1) ID " +
            "FROM [ProductSlug] " +
            "WHERE [ProductSlug].Deleted = 0 " +
            "AND [ProductSlug].[Locale] = @Culture " +
            "AND [ProductSlug].ProductID = [Product].ID " +
            ") " +
            "LEFT JOIN [ProductImage] " +
            "ON [ProductImage].ProductID = [Product].ID " +
            "WHERE [RowedOrderedProducts_CTE].RowNumber > @Offset " +
            "AND [RowedOrderedProducts_CTE].RowNumber <= @Limit + @Offset ";
        if (withVat)
            sqlExpression +=
                "AND [Storage].Locale = @Culture " +
                "AND [Storage].ForDefective = 0 " +
                "AND [Storage].OrganizationID = @OrganizationId ";
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
                "AND OrganizationID = @OrganizationId) ";

        sqlExpression +=
            "ORDER BY [RowedOrderedProducts_CTE].OrderedQty DESC, " +
            "CASE WHEN [Storage].Locale = @Culture THEN 0 ELSE 1 END";

        Type[] types = {
            typeof(OrderItem),
            typeof(Product),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(MeasureUnit),
            typeof(ProductSlug),
            typeof(ProductImage)
        };

        Func<object[], Product> mapper = objects => {
            OrderItem orderItem = (OrderItem)objects[0];
            Product product = (Product)objects[1];
            ProductAvailability productAvailability = (ProductAvailability)objects[2];
            Storage storage = (Storage)objects[3];
            MeasureUnit measureUnit = (MeasureUnit)objects[4];
            ProductSlug productSlug = (ProductSlug)objects[5];
            ProductImage image = (ProductImage)objects[6];

            product.MeasureUnit = measureUnit;
            product.ProductSlug = productSlug;

            if (image != null && !image.Deleted) {
                product.ProductImages.Add(image);
                product.Image = image.ImageUrl;
            }

            if (orderItems.Any(p => p.Product.Id.Equals(product.Id))) {
                if (productAvailability == null) return product;

                productAvailability.Storage = storage;

                Product current = orderItems.First(p => p.Product.Id.Equals(product.Id)).Product;

                if (current.ProductAvailabilities.Any(p => p.Id.Equals(productAvailability.Id))) return product;

                current.ProductAvailabilities.Add(productAvailability);
                current.AvailableQtyUk += productAvailability.Amount;
            } else {
                if (productAvailability != null) {
                    productAvailability.Storage = storage;

                    product.ProductAvailabilities.Add(productAvailability);

                    product.AvailableQtyUk += productAvailability.Amount;
                }

                product.MeasureUnit = measureUnit;

                product.CurrentLocalPrice = decimal.Round(product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                product.ProductAvailabilities.Add(productAvailability);

                orderItem.Product = product;

                orderItems.Add(orderItem);
            }

            return product;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new {
                From = from,
                To = to,
                Limit = limit,
                Offset = offset,
                NetId = clientNetId,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                ClientAgreementNetId = activeClientAgreementNetId,
                OrganizationId = organizationId,
                CurrencyId = currencyId,
                WithVat = withVat
            }
        );

        return orderItems;
    }

    public IEnumerable<Product> GetAllWithoutActiveSpecificationByLocale(string locale) {
        return _connection.Query<Product>(
            "SELECT [Product].* " +
            "FROM [Product] " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].ProductID = [Product].ID " +
            "AND [ProductSpecification].Deleted = 0 " +
            "AND [ProductSpecification].Locale = @Locale " +
            "WHERE [ProductSpecification].ID IS NULL " +
            "AND [Product].Deleted = 0",
            new { Locale = locale }
        );
    }

    public IEnumerable<Product> GetAllFilteredByActiveSpecificationNameByLocale(string name, string locale) {
        return _connection.Query<Product>(
            "SELECT [Product].* " +
            "FROM [Product] " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].ProductID = [Product].ID " +
            "AND [ProductSpecification].Deleted = 0 " +
            "AND [ProductSpecification].Locale = @Locale " +
            "WHERE [ProductSpecification].[Name] = @Name " +
            "AND [Product].Deleted = 0",
            new { Name = name, Locale = locale }
        );
    }

    public IEnumerable<Product> GetAllFilteredByActiveSpecificationCodeByLocale(string code, string locale) {
        return _connection.Query<Product>(
            "SELECT [Product].* " +
            "FROM [Product] " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].ProductID = [Product].ID " +
            "AND [ProductSpecification].Deleted = 0 " +
            "AND [ProductSpecification].Locale = @Locale " +
            "WHERE [ProductSpecification].[SpecificationCode] = @Code " +
            "AND [Product].Deleted = 0",
            new { Code = code, Locale = locale }
        );
    }

    public List<Product> GetAllFromSearch(string value, long limit, long offset, Guid clientAgreementNetId, bool withVat = false) {
        List<Product> products = new();

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS ( " +
            "SELECT [Product].ID " +
            ", MAX([ProductAvailability].Amount) AS [Qty] " +
            ", [Product].SearchVendorCode " +
            "FROM [Product] " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "LEFT JOIN [ProductOriginalNumber] " +
            "ON [ProductOriginalNumber].ProductID = [Product].ID " +
            "AND [ProductOriginalNumber].Deleted = 0 " +
            "LEFT JOIN [OriginalNumber] " +
            "ON [OriginalNumber].ID = [ProductOriginalNumber].OriginalNumberID " +
            "WHERE [Storage].Locale = @Culture " +
            "AND [Product].Deleted = 0 " +
            "AND ( " +
            "PATINDEX(@Value, [Product].SearchVendorCode) > 0 " +
            "OR " +
            "PATINDEX(@Value, [Product].SearchNameUA) > 0 " +
            "OR " +
            "PATINDEX(@Value, [OriginalNumber].[Number]) > 0 " +
            ") " +
            "GROUP BY [Product].ID " +
            ", [Product].SearchVendorCode " +
            "), " +
            "[Rowed_CTE] " +
            "AS ( " +
            "SELECT [Product].ID " +
            ", ROW_NUMBER() OVER(ORDER BY CASE WHEN [Product].Qty <> 0 THEN 0 ELSE 1 END, [Product].SearchVendorCode) AS [RowNumber] " +
            "FROM [Search_CTE] AS [Product] " +
            ") " +
            "SELECT " +
            "[Product].ID " +
            ", [Product].Created " +
            ", [Product].Deleted ";

        sqlExpression += ProductSqlFragments.ProductCultureColumns;

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
            ",[ProductPricing].ID " +
            ",[ProductPricing].Price " +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[MeasureUnit].* " +
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
            "LEFT JOIN [Rowed_CTE] " +
            "ON [Rowed_CTE].ID = [Product].ID " +
            "WHERE [Rowed_CTE].RowNumber > @Offset " +
            "AND [Rowed_CTE].RowNumber <= @Limit + @Offset " +
            "AND [Storage].ForDefective = 0 " +
            "ORDER BY [Rowed_CTE].RowNumber, CASE WHEN [Storage].Locale = @Culture THEN 0 ELSE 1 END, [ProductAvailability].Amount DESC";

        Type[] types = {
            typeof(Product),
            typeof(ProductPricing),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(MeasureUnit)
        };

        Func<object[], Product> productsMapper = objects => {
            Product product = (Product)objects[0];
            ProductPricing productPricing = (ProductPricing)objects[1];
            ProductAvailability productAvailability = (ProductAvailability)objects[2];
            Storage storage = (Storage)objects[3];
            MeasureUnit measureUnit = (MeasureUnit)objects[4];

            if (products.Any(p => p.Id.Equals(product.Id))) {
                if (productAvailability == null) return product;

                Product fromList = products.First(p => p.Id.Equals(product.Id));

                productAvailability.Storage = storage;

                if (!storage.ForDefective) {
                    if (storage.Locale.ToLower().Equals("pl")) {
                        if (storage.ForVatProducts)
                            fromList.AvailableQtyPlVAT += productAvailability.Amount;
                        else
                            fromList.AvailableQtyPl += productAvailability.Amount;
                    } else {
                        if (storage.ForVatProducts) {
                            fromList.AvailableQtyUkVAT += productAvailability.Amount;

                            if (storage.AvailableForReSale)
                                fromList.AvailableQtyUk += productAvailability.Amount;
                        } else {
                            fromList.AvailableQtyUk += productAvailability.Amount;
                        }
                    }
                }

                fromList.ProductAvailabilities.Add(productAvailability);
            } else {
                if (productPricing != null) {
                    productPricing.Price = product.CurrentPrice;

                    product.ProductPricings.Add(productPricing);
                }

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
                                    product.AvailableQtyUk += productAvailability.Amount;
                            } else {
                                product.AvailableQtyUk += productAvailability.Amount;
                            }
                        }
                    }
                }

                product.MeasureUnit = measureUnit;

                product.CurrentLocalPrice = decimal.Round(product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                product.ProductAvailabilities.Add(productAvailability);

                products.Add(product);
            }

            return product;
        };

        _connection.Query(
            sqlExpression,
            types,
            productsMapper,
            new {
                Value = $"%{value}%",
                ClientAgreementNetId = clientAgreementNetId,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Offset = offset,
                Limit = limit,
                WithVat = withVat
            }
        );

        if (products.Any(p => p.HasAnalogue)) {
            types = new[] {
                typeof(Product),
                typeof(ProductAnalogue),
                typeof(Product),
                typeof(ProductPricing),
                typeof(ProductAvailability),
                typeof(Storage),
                typeof(MeasureUnit)
            };

            Func<object[], Product> analogueMapper = objects => {
                Product product = (Product)objects[0];
                ProductAnalogue productAnalogue = (ProductAnalogue)objects[1];
                Product analogue = (Product)objects[2];
                ProductPricing productPricing = (ProductPricing)objects[3];
                ProductAvailability productAvailability = (ProductAvailability)objects[4];
                Storage storage = (Storage)objects[5];
                MeasureUnit measureUnit = (MeasureUnit)objects[6];

                Product fromList = products.First(p => p.Id.Equals(product.Id));

                if (!fromList.AnalogueProducts.Any(a => a.Id.Equals(productAnalogue.Id))) {
                    if (productPricing != null) {
                        productPricing.Price = analogue.CurrentPrice;

                        analogue.ProductPricings.Add(productPricing);
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
                    }

                    analogue.MeasureUnit = measureUnit;

                    analogue.CurrentLocalPrice = decimal.Round(analogue.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                    analogue.ProductAvailabilities.Add(productAvailability);

                    productAnalogue.AnalogueProduct = analogue;

                    fromList.AnalogueProducts.Add(productAnalogue);
                } else {
                    if (productAvailability == null) return product;

                    ProductAnalogue analogueFromList = fromList.AnalogueProducts.First(a => a.Id.Equals(productAnalogue.Id));

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

            string analoguesExpression =
                "SELECT " +
                "[Product].ID " +
                ",[ProductAnalogue].* " +
                ", [Analogue].ID " +
                ", [Analogue].Created " +
                ", [Analogue].Deleted ";

            analoguesExpression += ProductSqlFragments.AnalogueCultureColumns;

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
                ",dbo.GetCalculatedProductPriceWithSharesAndVat(Analogue.NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentPrice " +
                ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Analogue.NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentLocalPrice " +
                ",[ProductPricing].* " +
                ",[ProductAvailability].* " +
                ",[Storage].* " +
                ",[MeasureUnit].* " +
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
                "WHERE [Product].ID IN @Ids " +
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
                    Ids = products.Where(p => p.HasAnalogue).Select(p => p.Id),
                    ClientAgreementNetId = clientAgreementNetId,
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                    WithVat = withVat
                }
            );
        }

        if (!products.Any(p => p.HasComponent)) return products;

        types = new[] {
            typeof(Product),
            typeof(ProductSet),
            typeof(Product),
            typeof(ProductPricing),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(MeasureUnit)
        };

        Func<object[], Product> componentMapper = objects => {
            Product product = (Product)objects[0];
            ProductSet productSet = (ProductSet)objects[1];
            Product component = (Product)objects[2];
            ProductPricing productPricing = (ProductPricing)objects[3];
            ProductAvailability productAvailability = (ProductAvailability)objects[4];
            Storage storage = (Storage)objects[5];
            MeasureUnit measureUnit = (MeasureUnit)objects[6];

            Product fromList = products.First(p => p.Id.Equals(product.Id));

            if (!fromList.ComponentProducts.Any(a => a.Id.Equals(productSet.Id))) {
                if (productPricing != null) {
                    productPricing.Price = component.CurrentPrice;

                    component.ProductPricings.Add(productPricing);
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
                }

                component.MeasureUnit = measureUnit;

                component.CurrentLocalPrice = decimal.Round(component.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                component.ProductAvailabilities.Add(productAvailability);

                productSet.ComponentProduct = component;

                fromList.ComponentProducts.Add(productSet);
            } else {
                if (productAvailability == null) return product;

                ProductSet componentFromList = fromList.ComponentProducts.First(a => a.Id.Equals(productSet.Id));

                productAvailability.Storage = storage;

                if (!storage.ForDefective) {
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

        componentsExpression += ProductSqlFragments.ComponentCultureColumns;

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
            ",dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentLocalPrice " +
            ",[ProductPricing].* " +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[MeasureUnit].* " +
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
            "WHERE [Product].ID IN @Ids " +
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
                Ids = products.Where(p => p.HasComponent).Select(p => p.Id),
                ClientAgreementNetId = clientAgreementNetId,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                WithVat = withVat
            }
        );

        return products;
    }

    public List<Product> GetAllFromAdvancedSearch(
        string value,
        long limit,
        long offset,
        Guid clientAgreementNetId,
        ProductAdvancedSearchMode mode,
        ProductAdvancedSortMode sortMode,
        bool withCalculatedPrices,
        long? organizationId,
        bool withVat = false) {
        List<Product> products = new();

        string currentCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower();
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

        dynamic Testprops = new ExpandoObject();
        Testprops.Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        Testprops.Limit = limit;
        Testprops.Offset = offset;
        Testprops.Value = SpecialCharactersReplace.Replace(value, string.Empty);
        string[] concreteValues = value.Split(' ');

        if (concreteValues.Length > 1)
            for (int i = 0; i < concreteValues.Length; i++)
                (Testprops as ExpandoObject).AddProperty($"Var{i}", SpecialCharactersReplace.Replace(concreteValues[i], string.Empty));
        string orderByExpression;
        string orderByExpressionAll;
        switch (sortMode) {
            case ProductAdvancedSortMode.Name:
                orderByExpression =
                    currentCulture.ToLower().Equals("uk")
                        ? "ORDER BY [Product].[NameUA]"
                        : "ORDER BY [Product].[NameUA]";
                orderByExpressionAll =
                    currentCulture.ToLower().Equals("uk")
                        ? "[NameUA]"
                        : "[NameUA]";
                break;
            case ProductAdvancedSortMode.VendorCode:
                orderByExpression = "ORDER BY [Product].[VendorCode]";
                orderByExpressionAll = "[VendorCode]";
                break;
            case ProductAdvancedSortMode.Top:
            default:
                orderByExpression = "ORDER BY [Product].[Top]";
                orderByExpressionAll = "[Top]";
                break;
        }

        string sqlExpressionIds = string.Empty;

        switch (mode) {
            case ProductAdvancedSearchMode.VendorCode:
                sqlExpressionIds +=
                    ";WITH [SearchStage_Zero] AS ( " +
                    "SELECT [Product].ID, " +
                    $"[Product].{orderByExpressionAll}, " +
                    "[Product].SearchName, " +
                    "[Product].SearchVendorCode, " +
                    "(CASE WHEN ([ProductAvailability].Amount > 0) THEN 1 ELSE 0 END) AS [Available] " +
                    "FROM [Product] " +
                    "LEFT JOIN [ProductAvailability] " +
                    "ON [ProductAvailability].ProductID = [Product].ID " +
                    "LEFT JOIN [Storage] " +
                    "ON [Storage].ID = [ProductAvailability].StorageID " +
                    "AND [Storage].Locale = @Culture " +
                    "WHERE [Product].Deleted = 0 " +
                    "), " +
                    "[Search_FullValue] AS ( " +
                    "SELECT [Product].ID, " +
                    "[Product].Available, " +
                    $"[Product].{orderByExpressionAll}, " +
                    "[Product].SearchName " +
                    "FROM [SearchStage_Zero] AS [Product] " +
                    "WHERE  PATINDEX('%' + @Value + '%', [Product].SearchVendorCode) > 0 " +
                    "), " +
                    "[Search_CTE] AS " +
                    "( " +
                    "SELECT " +
                    "[Search_FullValue].ID, " +
                    "[Search_FullValue].Available, " +
                    $"[Search_FullValue].{orderByExpressionAll}, " +
                    "[Search_FullValue].SearchName " +
                    "FROM [Search_FullValue] " +
                    "), ";
                break;
            case ProductAdvancedSearchMode.OriginalNumber:
                sqlExpressionIds +=
                    ";WITH [Search_CTE] " +
                    "AS ( " +
                    "SELECT [Product].ID ," +
                    "[Product].SearchName, " +
                    $"[Product].{orderByExpressionAll}, " +
                    "(CASE WHEN ([ProductAvailability].Amount > 0) THEN 1 ELSE 0 END) AS [Available]" +
                    "FROM [Product] " +
                    "LEFT JOIN [ProductAvailability] " +
                    "ON [ProductAvailability].ProductID = [Product].ID " +
                    "LEFT JOIN [ProductOriginalNumber] " +
                    "ON [ProductOriginalNumber].ProductID = [Product].ID " +
                    "AND [ProductOriginalNumber].Deleted = 0 " +
                    "LEFT JOIN [OriginalNumber] " +
                    "ON [OriginalNumber].ID = [ProductOriginalNumber].OriginalNumberID " +
                    "WHERE [Product].Deleted = 0 " +
                    "AND PATINDEX('%' + @Value + '%', [OriginalNumber].[Number]) = 1 " +
                    "), ";
                break;
            case ProductAdvancedSearchMode.Size:
                sqlExpressionIds +=
                    ";WITH [SearchStage_Zero] AS ( " +
                    "SELECT [Product].ID, " +
                    $"[Product].{orderByExpressionAll}, " +
                    "[Product].SearchSize, " +
                    "[Product].SearchName, " +
                    "(CASE WHEN ([ProductAvailability].Amount > 0) THEN 1 ELSE 0 END) AS [Available] " +
                    "FROM [Product] " +
                    "LEFT JOIN [ProductAvailability] " +
                    "ON [ProductAvailability].ProductID = [Product].ID " +
                    "LEFT JOIN [Storage] " +
                    "ON [Storage].ID = [ProductAvailability].StorageID " +
                    "AND [Storage].Locale = @Culture " +
                    "WHERE [Product].Deleted = 0 " +
                    "), " +
                    "[Search_FullValue] AS ( " +
                    "SELECT [Product].ID, " +
                    "[Product].Available, " +
                    $"[Product].{orderByExpressionAll}, " +
                    "[Product].SearchName " +
                    "FROM [SearchStage_Zero] AS [Product] " +
                    "WHERE  PATINDEX('%' + @Value + '%', [Product].SearchSize) > 0 " +
                    "), " +
                    "[Search_CTE] AS " +
                    "( " +
                    "SELECT " +
                    "[Search_FullValue].ID, " +
                    "[Search_FullValue].Available, " +
                    $"[Search_FullValue].{orderByExpressionAll}, " +
                    "[Search_FullValue].SearchName " +
                    "FROM [Search_FullValue] " +
                    "), ";
                break;
            case ProductAdvancedSearchMode.Name:
                sqlExpressionIds +=
                    ";WITH [SearchStage_Zero] AS ( " +
                    "SELECT [Product].ID, ";
                if (sortMode != ProductAdvancedSortMode.Name) sqlExpressionIds += $"[Product].{orderByExpressionAll}, ";

                sqlExpressionIds +=
                    "[Product].[SearchNameUA], " +
                    "[Product].[NameUA], ";

                sqlExpressionIds += "[Product].SearchName, " +
                                    "(CASE WHEN ([ProductAvailability].Amount > 0) THEN 1 ELSE 0 END) AS [Available] " +
                                    "FROM [Product] " +
                                    "LEFT JOIN [ProductAvailability] " +
                                    "ON [ProductAvailability].ProductID = [Product].ID " +
                                    "LEFT JOIN [Storage] " +
                                    "ON [Storage].ID = [ProductAvailability].StorageID " +
                                    "AND [Storage].Locale = @Culture " +
                                    "WHERE [Product].Deleted = 0 " +
                                    "), " +
                                    "[Search_FullValue] AS ( " +
                                    "SELECT [Product].ID, " +
                                    "[Product].Available," +
                                    "[Product].SearchName, " +
                                    $"[Product].{orderByExpressionAll} " +
                                    "FROM [SearchStage_Zero] AS [Product] ";
                sqlExpressionIds +=
                    "WHERE " +
                    "PATINDEX(@Value, [Product].[SearchNameUA]) = 1 " +
                    "OR " +
                    "CHARINDEX(@Value, [Product].[NameUA]) > 0 ";

                sqlExpressionIds +=
                    "OR PATINDEX('%' + @Value + '%', [Product].SearchName) > 0 " +
                    "GROUP BY [Product].ID , " +
                    $"[Product].{orderByExpressionAll}, " +
                    "[Product].SearchName, " +
                    "Available " +
                    "), " +
                    "[Search_CTE] AS " +
                    "( " +
                    "SELECT " +
                    "[Search_FullValue].ID, " +
                    "[Search_FullValue].Available , " +
                    $"[Search_FullValue].{orderByExpressionAll} , " +
                    "[Search_FullValue].SearchName " +
                    "FROM [Search_FullValue] " +
                    "), ";
                break;
            case ProductAdvancedSearchMode.Description:
                sqlExpressionIds +=
                    ";WITH [SearchStage_Zero] AS ( " +
                    "SELECT [Product].ID, " +
                    $"[Product].{orderByExpressionAll}, " +
                    "[Product].[SearchDescription], ";
                sqlExpressionIds +=
                    "[Product].[SearchDescriptionUA], ";

                sqlExpressionIds += "[Product].SearchName, " +
                                    "(CASE WHEN ([ProductAvailability].Amount > 0) THEN 1 ELSE 0 END) AS [Available] " +
                                    "FROM [Product] " +
                                    "LEFT JOIN [ProductAvailability] " +
                                    "ON [ProductAvailability].ProductID = [Product].ID " +
                                    "LEFT JOIN [Storage] " +
                                    "ON [Storage].ID = [ProductAvailability].StorageID " +
                                    "AND [Storage].Locale = @Culture " +
                                    "WHERE [Product].Deleted = 0 " +
                                    "), " +
                                    "[Search_FullValue] AS ( " +
                                    "SELECT [Product].ID, " +
                                    "[Product].Available, " +
                                    $"[Product].{orderByExpressionAll}, " +
                                    "[Product].SearchName " +
                                    "FROM [SearchStage_Zero] AS [Product] ";
                sqlExpressionIds +=
                    "WHERE " +
                    "PATINDEX(@Value, [Product].[SearchDescriptionUA]) > 0 ";

                sqlExpressionIds +=
                    "OR PATINDEX('%' + @Value + '%', [Product].SearchDescription) > 0 " +
                    "), " +
                    "[Search_CTE] AS " +
                    "( " +
                    "SELECT " +
                    "[Search_FullValue].ID, " +
                    "[Search_FullValue].Available, " +
                    $"[Search_FullValue].{orderByExpressionAll}, " +
                    "[Search_FullValue].SearchName " +
                    "FROM [Search_FullValue] " +
                    "), ";
                break;
            case ProductAdvancedSearchMode.All:
            default:
                string lastProductsStageName = "[SearchStage_Zero] ";
                string lastOriginalNumbersStageName = string.Empty;

                if (concreteValues.Length > 1) {
                    sqlExpressionIds +=
                        ";WITH [SearchStage_Zero] AS ( " +
                        "SELECT [Product].ID , " +
                        "[Product].SearchName ,  " +
                        "[Product].SearchDescription , " +
                        "[Product].SearchNameUA , " +
                        "[Product].SearchDescriptionUA , " +
                        "[Product].SearchVendorCode , " +
                        "[Product].SearchSize , " +
                        "[Product].MainOriginalNumber , " +
                        $"[Product].{orderByExpressionAll} , " +
                        "( CASE WHEN ([ProductAvailability].Amount > 0) THEN 1 ELSE 0 END ) AS [Available] " +
                        "FROM [Product] " +
                        "LEFT JOIN [ProductAvailability] ON [ProductAvailability].ProductID = [Product].ID " +
                        "LEFT JOIN [Storage] ON [Storage].ID = [ProductAvailability].StorageID " +
                        "WHERE [Product].Deleted = 0 " +
                        "AND [Storage].Locale = @Culture ), ";
                    for (int i = 0; i < concreteValues.Length; i++) {
                        string currentProductsStageName = $"[Search_Stage{i}] ";

                        sqlExpressionIds +=
                            $"{currentProductsStageName} AS ( " +
                            "SELECT [Product].ID , " +
                            "[Product].SearchName , " +
                            "[Product].SearchDescription , ";
                        sqlExpressionIds +=
                            "[Product].SearchNameUA , " +
                            "[Product].SearchDescriptionUA , " +
                            "[Product].SearchVendorCode , " +
                                            "[Product].SearchSize , " +
                                            "[Product].MainOriginalNumber , " +
                                            $"[Product].{orderByExpressionAll} , " +
                                            $"[Product].[Available] FROM {lastProductsStageName} AS [Product] " +
                                            $"WHERE PATINDEX('%' + @Var{i} + '%', [Product].SearchName) > 0 " +
                                            $"OR PATINDEX('%' + @Var{i} + '%', [Product].SearchDescription) > 0 ";
                        sqlExpressionIds +=
                            $"OR PATINDEX('%' + @Var{i} + '%', [Product].SearchNameUA) > 0  " +
                            $"OR PATINDEX('%' + @Var{i} + '%', [Product].SearchDescriptionUA) > 0 ";

                        sqlExpressionIds +=
                            $"OR PATINDEX('%' + @Var{i} + '%', [Product].SearchVendorCode) > 0  " +
                            $"OR PATINDEX('%' + @Var{i} + '%', [Product].SearchSize) > 0 " +
                            $"OR PATINDEX('%' + @Var{i} + '%', [Product].MainOriginalNumber) > 0 ), ";
                        lastProductsStageName = currentProductsStageName;
                        if (i.Equals(concreteValues.Length - 1))
                            sqlExpressionIds +=
                                "[Search_CTE] AS (  " +
                                "SELECT [LastStageByProduct].ID , " +
                                "[LastStageByProduct].Available , " +
                                $"[LastStageByProduct].{orderByExpressionAll} , " +
                                "[LastStageByProduct].SearchName " +
                                $"FROM {lastProductsStageName} AS [LastStageByProduct] " +
                                "), ";
                    }
                } else {
                    sqlExpressionIds +=
                        ";WITH [SearchStage_Zero] AS ( " +
                        "SELECT [Product].ID, " +
                        "[Product].SearchName, " +
                        "[Product].SearchDescription, " +
                        "[Product].SearchNameUA, " +
                        "[Product].SearchDescriptionUA, " +
                        "[Product].SearchVendorCode, " +
                        "[Product].SearchSize, " +
                        "[Product].MainOriginalNumber, " +
                        $"[Product].{orderByExpressionAll}, " +
                        "(CASE WHEN ([ProductAvailability].Amount > 0) THEN 1 ELSE 0 END) AS [Available] " +
                        "FROM [Product] " +
                        "LEFT JOIN [ProductAvailability] ON [ProductAvailability].ProductID = [Product].ID " +
                        "LEFT JOIN [Storage] ON [Storage].ID = [ProductAvailability].StorageID " +
                        "AND [Storage].Locale = @Culture " +
                        "WHERE [Product].Deleted = 0 " +
                        "), " +
                        "[Search_FullValue] AS ( " +
                        "SELECT [Product].ID, " +
                        "[Product].Available, " +
                        $"[Product].{orderByExpressionAll}, " +
                        "[Product].SearchNameUA AS [SearchName] " +
                        "FROM [SearchStage_Zero] AS [Product] " +
                        "WHERE PATINDEX('%' + @Value + '%', [Product].SearchName) > 0 OR " +
                        "PATINDEX('%' + @Value + '%', [Product].SearchDescription) > 0 OR " +
                        "PATINDEX('%' + @Value + '%', [Product].SearchNameUA) > 0 OR " +
                        "PATINDEX('%' + @Value + '%', [Product].SearchDescriptionUA) > 0 OR " +
                        "PATINDEX('%' + @Value + '%', [Product].SearchVendorCode) > 0 OR " +
                        "PATINDEX('%' + @Value + '%', [Product].SearchSize) > 0 OR " +
                        "PATINDEX('%' + @Value + '%', [Product].MainOriginalNumber) > 0 " +
                        "), " +
                        "[Search_CTE] AS  " +
                        "( " +
                        "SELECT [Search_FullValue].ID, " +
                        "[Search_FullValue].Available, " +
                        $"[Search_FullValue].{orderByExpressionAll}, " +
                        "[Search_FullValue].SearchName " +
                        "FROM [Search_FullValue] " +
                        "), ";
                }

                break;
        }

        sqlExpressionIds +=
            "[Rowed_CTE] AS ( " +
            "SELECT [Product].ID, " +
            "MAX([Product].Available) AS [Available], " +
            "ROW_NUMBER() OVER ( ORDER BY MAX([Product].Available) DESC, [Product].SearchName ) AS [RowNumber] " +
            "FROM [Search_CTE] AS [Product] " +
            "GROUP BY " +
            "[Product].ID, " +
            $"[Product].{orderByExpressionAll}, " +
            "[Product].SearchName " +
            ") " +
            "SELECT[Rowed_CTE].ID " +
            "FROM [Rowed_CTE] " +
            "WHERE [Rowed_CTE].RowNumber > @Offset " +
            "AND [Rowed_CTE].RowNumber <= @Limit + @Offset " +
            "ORDER BY [Rowed_CTE].RowNumber; ";

        var props = new { Offset = offset, Limit = limit, Culture = currentCulture, Value = SpecialCharactersReplace.Replace(value, string.Empty) };
        IEnumerable<long> productIds =
            _connection.Query<long>(
                sqlExpressionIds,
                (object)Testprops
            );

        string sqlExpression = string.Empty;
        sqlExpression +=
            "SELECT " +
            "[Product].ID " +
            ", [Product].Created " +
            ", [Product].Deleted ";

        sqlExpression += ProductSqlFragments.ProductCultureColumns;

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
            ", [Product].[Weight] ";

        if (withCalculatedPrices)
            sqlExpression +=
                ",dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentPrice " +
                ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentLocalPrice " +
                ",dbo.GetCalculatedProductPriceWithShares_ReSale(Product.NetUID, @ClientAgreementNetId, @Culture, NULL) AS CurrentPriceReSale " +
                ",dbo.GetCalculatedProductLocalPriceWithShares_ReSale(Product.NetUID, @ClientAgreementNetId, @Culture, NULL) AS CurrentLocalPriceReSale ";

        sqlExpression +=
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
            "WHERE Product.ID IN @Ids " +
            "AND (Storage.ForDefective = 0 " +
            "OR Storage.ForDefective IS NULL) " +
            "ORDER BY  CASE WHEN Storage.Locale = @Culture THEN 0 ELSE 1 END, ProductAvailability.Amount DESC;";
        // "LEFT JOIN [Rowed_CTE] " +
        // "ON [Rowed_CTE].ID = [Product].ID " +
        // "WHERE [Rowed_CTE].RowNumber > @Offset " +
        // "AND [Rowed_CTE].RowNumber <= @Limit + @Offset " +
        // "AND (" +
        // "[Storage].ForDefective = 0 " +
        // "OR " +
        // "[Storage].ForDefective IS NULL" +
        // ") " +
        // "ORDER BY [Rowed_CTE].RowNumber, CASE WHEN [Storage].Locale = @Culture THEN 0 ELSE 1 END, [ProductAvailability].Amount DESC";

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

            if (products.Any(p => p.Id.Equals(product.Id))) {
                Product fromList = products.First(p => p.Id.Equals(product.Id));

                if (productAvailability != null && !fromList.ProductAvailabilities.Any(a => productAvailability.Id.Equals(a.Id))) {
                    productAvailability.Storage = storage;

                    if (!storage.ForDefective) {
                        if (storage.OrganizationId.Equals(organizationId))
                            if (storage.Locale.ToLower().Equals("pl")) {
                                if (storage.ForVatProducts)
                                    fromList.AvailableQtyPlVAT += productAvailability.Amount;
                                else
                                    fromList.AvailableQtyPl += productAvailability.Amount;
                            } else {
                                if (storage.ForVatProducts) {
                                    fromList.AvailableQtyUkVAT += productAvailability.Amount;

                                    if (storage.AvailableForReSale)
                                        fromList.AvailableQtyUkReSale += productAvailability.Amount;
                                } else {
                                    fromList.AvailableQtyUk += productAvailability.Amount;
                                }
                            }
                        else if (storage.AvailableForReSale && !storage.Locale.ToLower().Equals("pl"))
                            fromList.AvailableQtyUkReSale += productAvailability.Amount;
                        else if (organization != null && organization.StorageId.Equals(storage.Id))
                            fromList.AvailableQtyUk += productAvailability.Amount;
                    }

                    fromList.ProductAvailabilities.Add(productAvailability);
                }

                if (productImage != null && !fromList.ProductImages.Any(i => i.Id.Equals(productImage.Id))) fromList.ProductImages.Add(productImage);
            } else {
                if (productPricing != null) {
                    productPricing.Price = product.CurrentPrice;

                    product.ProductPricings.Add(productPricing);
                }

                //product.CurrentPriceEurToUah = product.CurrentPrice * currentExchangeRateEurToUah;
                // product.CurrentPriceReSaleEurToUah = product.CurrentPriceReSale * currentExchangeRateEurToUah;

                if (productAvailability != null) {
                    productAvailability.Storage = storage;

                    if (!storage.ForDefective) {
                        if (storage.OrganizationId.Equals(organizationId))
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
                                } else {
                                    product.AvailableQtyUk += productAvailability.Amount;
                                }
                            }
                        else if (storage.AvailableForReSale && !storage.Locale.ToLower().Equals("pl"))
                            product.AvailableQtyUkReSale += productAvailability.Amount;
                        else if (organization != null && organization.StorageId.Equals(storage.Id))
                            product.AvailableQtyUk += productAvailability.Amount;
                    }

                    product.ProductAvailabilities.Add(productAvailability);
                }

                if (productImage != null) {
                    product.ProductImages.Add(productImage);

                    product.HasImage = true;
                    //product.Image = productImage.ImageUrl;
                }

                product.MeasureUnit = measureUnit;

                products.Add(product);
            }

            return product;
        };

        _connection.Query(
            sqlExpression,
            types,
            productsMapper,
            new {
                Value = $"{value}%",
                ClientAgreementNetId = clientAgreementNetId,
                Culture = currentCulture,
                Offset = offset,
                Limit = limit,
                OrganizationId = organizationId ?? 0,
                WithVat = withVat,
                Ids = productIds
            }
        );

        foreach (Product product in products) {
            decimal currentExchangeRateEurToUah = _connection.Query<decimal>(
                "SELECT dbo.GetCurrentEuroExchangeRateFiltered(@ProductNetId, @CurrencyId, @WithVat, NULL)",
                new { ProductNetId = product.NetUid, CurrencyID = uahCurrencyId, WithVat = withVat }).FirstOrDefault();

            product.CurrentPriceEurToUah = decimal.Round(product.CurrentPrice * currentExchangeRateEurToUah, 2, MidpointRounding.AwayFromZero);
            product.CurrentPriceReSaleEurToUah = decimal.Round(product.CurrentPriceReSale * currentExchangeRateEurToUah, 2, MidpointRounding.AwayFromZero);
        }

        if (products.Any(p => p.HasAnalogue)) {
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

                Product fromList = products.First(p => p.Id.Equals(product.Id));

                if (!fromList.AnalogueProducts.Any(a => a.Id.Equals(productAnalogue.Id))) {
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
                                            analogue.AvailableQtyUkReSale += productAvailability.Amount;
                                    } else {
                                        analogue.AvailableQtyUk += productAvailability.Amount;
                                    }
                                }
                            else if (storage.AvailableForReSale && !storage.Locale.ToLower().Equals("pl"))
                                analogue.AvailableQtyUkReSale += productAvailability.Amount;
                            else if (organization != null && organization.StorageId.Equals(storage.Id))
                                analogue.AvailableQtyUk += productAvailability.Amount;
                            else return product;
                        }

                        analogue.ProductAvailabilities.Add(productAvailability);
                    }

                    if (productPricing != null) {
                        productPricing.Price = analogue.CurrentPrice;

                        analogue.ProductPricings.Add(productPricing);
                    }

                    analogue.MeasureUnit = measureUnit;

                    // analogue.CurrentLocalPrice = decimal.Round(analogue.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);
                    // analogue.CurrentPriceEurToUah = decimal.Round(analogue.CurrentPrice * currentExchangeRateEurToUah, 2, MidpointRounding.AwayFromZero);

                    productAnalogue.AnalogueProduct = analogue;

                    fromList.AnalogueProducts.Add(productAnalogue);
                } else {
                    if (productAvailability == null) return product;

                    ProductAnalogue analogueFromList = fromList.AnalogueProducts.First(a => a.Id.Equals(productAnalogue.Id));

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
                                        analogueFromList.AnalogueProduct.AvailableQtyUkReSale += productAvailability.Amount;
                                } else {
                                    analogueFromList.AnalogueProduct.AvailableQtyUk += productAvailability.Amount;
                                }
                            }
                        else if (storage.AvailableForReSale && !storage.Locale.ToLower().Equals("pl"))
                            analogueFromList.AnalogueProduct.AvailableQtyUkReSale += productAvailability.Amount;
                        else if (organization != null && organization.StorageId.Equals(storage.Id))
                            analogueFromList.AnalogueProduct.AvailableQtyUk += productAvailability.Amount;
                        else return product;
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

            analoguesExpression += ProductSqlFragments.AnalogueCultureColumns;

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
                ",dbo.GetCalculatedProductPriceWithSharesAndVat(Analogue.NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentPrice " +
                ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Analogue.NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentLocalPrice " +
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
                "WHERE [Product].ID IN @Ids " +
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
                    Ids = products.Where(p => p.HasAnalogue).Select(p => p.Id),
                    ClientAgreementNetId = clientAgreementNetId,
                    Culture = currentCulture,
                    WithVat = withVat
                }
            );
        }

        foreach (Product product in products.SelectMany(p => p.AnalogueProducts.Select(a => a.AnalogueProduct))) {
            decimal currentExchangeRateEurToUah = _connection.Query<decimal>(
                "SELECT dbo.GetCurrentEuroExchangeRateFiltered(@ProductNetId, @CurrencyId, @WithVat, NULL)",
                new { ProductNetId = product.NetUid, CurrencyID = uahCurrencyId, WithVat = withVat }).FirstOrDefault();

            product.CurrentPriceEurToUah = decimal.Round(product.CurrentPrice * currentExchangeRateEurToUah, 2, MidpointRounding.AwayFromZero);
            product.CurrentPriceReSaleEurToUah = decimal.Round(product.CurrentPriceReSale * currentExchangeRateEurToUah, 2, MidpointRounding.AwayFromZero);
        }

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

            Product fromList = products.First(p => p.Id.Equals(product.Id));

            if (!fromList.ComponentProducts.Any(a => a.Id.Equals(productSet.Id))) {
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
                                        component.AvailableQtyUkReSale += productAvailability.Amount;
                                } else {
                                    component.AvailableQtyUk += productAvailability.Amount;
                                }
                            }
                        else if (storage.AvailableForReSale && !storage.Locale.ToLower().Equals("pl"))
                            component.AvailableQtyUkReSale += productAvailability.Amount;
                        else if (organization != null && organization.StorageId.Equals(storage.Id))
                            component.AvailableQtyUk += productAvailability.Amount;
                        //else return product;
                    }

                    component.ProductAvailabilities.Add(productAvailability);
                }

                if (productPricing != null) {
                    productPricing.Price = component.CurrentPrice;

                    component.ProductPricings.Add(productPricing);
                }

                component.MeasureUnit = measureUnit;

                // component.CurrentLocalPrice = decimal.Round(component.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);
                // component.CurrentPriceEurToUah = decimal.Round(component.CurrentPrice * currentExchangeRateEurToUah, 2, MidpointRounding.AwayFromZero);

                productSet.ComponentProduct = component;

                fromList.ComponentProducts.Add(productSet);
            } else {
                if (productAvailability == null) return product;

                ProductSet componentFromList = fromList.ComponentProducts.First(a => a.Id.Equals(productSet.Id));

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
                                    componentFromList.ComponentProduct.AvailableQtyUkReSale += productAvailability.Amount;
                            } else {
                                componentFromList.ComponentProduct.AvailableQtyUk += productAvailability.Amount;
                            }
                        }
                    else if (storage.AvailableForReSale && !storage.Locale.ToLower().Equals("pl"))
                        componentFromList.ComponentProduct.AvailableQtyUkReSale += productAvailability.Amount;
                    else if (organization != null && organization.StorageId.Equals(storage.Id))
                        componentFromList.ComponentProduct.AvailableQtyUk += productAvailability.Amount;
                    //else return product;
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

        componentsExpression += ProductSqlFragments.ComponentCultureColumns;

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
            "WHERE [Product].ID IN @Ids " +
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
                Ids = products.Where(p => p.HasComponent).Select(p => p.Id),
                ClientAgreementNetId = clientAgreementNetId,
                Culture = currentCulture,
                WithVat = withVat
            }
        );

        foreach (Product product in products.SelectMany(p => p.ComponentProducts.Select(c => c.ComponentProduct))) {
            decimal currentExchangeRateEurToUah = _connection.Query<decimal>(
                "SELECT dbo.GetCurrentEuroExchangeRateFiltered(@ProductNetId, @CurrencyId, @WithVat, NULL)",
                new { ProductNetId = product.NetUid, CurrencyID = uahCurrencyId, WithVat = withVat }).FirstOrDefault();

            product.CurrentPriceEurToUah = decimal.Round(product.CurrentPrice * currentExchangeRateEurToUah, 2, MidpointRounding.AwayFromZero);
            product.CurrentPriceReSaleEurToUah = decimal.Round(product.CurrentPriceReSale * currentExchangeRateEurToUah, 2, MidpointRounding.AwayFromZero);
        }

        if (!products.Any(p => p.HasComponent)) {
            types = new[] {
                typeof(ProductSet),
                typeof(Product),
                typeof(ProductPricing),
                typeof(ProductAvailability),
                typeof(Storage),
                typeof(MeasureUnit),
                typeof(Organization)
            };

            Func<object[], Product> productSetsMapper = objects => {
                ProductSet productSet = (ProductSet)objects[0];
                Product baseProduct = (Product)objects[1];
                ProductPricing productPricing = (ProductPricing)objects[2];
                ProductAvailability productAvailability = (ProductAvailability)objects[3];
                Storage storage = (Storage)objects[4];
                MeasureUnit measureUnit = (MeasureUnit)objects[5];
                Organization organization = (Organization)objects[6];

                Product fromList = products.First(p => p.Id.Equals(productSet.ComponentProductId));

                if (!fromList.BaseSetProducts.Any(a => a.Id.Equals(productSet.Id))) {
                    if (productAvailability != null) {
                        productAvailability.Storage = storage;

                        if (!storage.ForDefective) {
                            if (storage.OrganizationId.Equals(organizationId))
                                if (storage.Locale.ToLower().Equals("pl")) {
                                    if (storage.ForVatProducts)
                                        baseProduct.AvailableQtyPlVAT += productAvailability.Amount;
                                    else
                                        baseProduct.AvailableQtyPl += productAvailability.Amount;
                                } else {
                                    if (storage.ForVatProducts) {
                                        baseProduct.AvailableQtyUkVAT += productAvailability.Amount;

                                        if (storage.AvailableForReSale)
                                            baseProduct.AvailableQtyUkReSale += productAvailability.Amount;
                                    } else {
                                        baseProduct.AvailableQtyUk += productAvailability.Amount;
                                    }
                                }
                            else if (storage.AvailableForReSale && !storage.Locale.ToLower().Equals("pl"))
                                baseProduct.AvailableQtyUkReSale += productAvailability.Amount;
                            else if (organization != null && organization.StorageId.Equals(storage.Id))
                                baseProduct.AvailableQtyUk += productAvailability.Amount;
                            else return baseProduct;
                        }

                        baseProduct.ProductAvailabilities.Add(productAvailability);
                    }

                    if (productPricing != null) {
                        productPricing.Price = baseProduct.CurrentPrice;

                        baseProduct.ProductPricings.Add(productPricing);
                    }

                    baseProduct.MeasureUnit = measureUnit;

                    // baseProduct.CurrentLocalPrice = decimal.Round(baseProduct.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);
                    // baseProduct.CurrentPriceEurToUah = decimal.Round(baseProduct.CurrentPrice * currentExchangeRateEurToUah, 2, MidpointRounding.AwayFromZero);

                    productSet.SetComponentsQty = 1;
                    productSet.BaseProduct = baseProduct;

                    fromList.BaseSetProducts.Add(productSet);
                } else {
                    if (productAvailability == null) return baseProduct;

                    ProductSet baseSetProduct = fromList.BaseSetProducts.First(a => a.Id.Equals(productSet.Id));

                    if (baseSetProduct.BaseProduct.ProductAvailabilities.Any(e => e.Id.Equals(productAvailability.Id))) return baseProduct;

                    productAvailability.Storage = storage;

                    if (!storage.ForDefective) {
                        if (storage.OrganizationId.Equals(organizationId))
                            if (storage.Locale.ToLower().Equals("pl")) {
                                if (storage.ForVatProducts)
                                    baseSetProduct.BaseProduct.AvailableQtyPlVAT += productAvailability.Amount;
                                else
                                    baseSetProduct.BaseProduct.AvailableQtyPl += productAvailability.Amount;
                            } else {
                                if (storage.ForVatProducts) {
                                    baseSetProduct.BaseProduct.AvailableQtyUkVAT += productAvailability.Amount;

                                    if (storage.AvailableForReSale)
                                        baseSetProduct.BaseProduct.AvailableQtyUkReSale += productAvailability.Amount;
                                } else {
                                    baseSetProduct.BaseProduct.AvailableQtyUk += productAvailability.Amount;
                                }
                            }
                        else if (storage.AvailableForReSale && !storage.Locale.ToLower().Equals("pl"))
                            baseSetProduct.BaseProduct.AvailableQtyUkReSale += productAvailability.Amount;
                        else if (organization != null && organization.StorageId.Equals(storage.Id))
                            baseSetProduct.BaseProduct.AvailableQtyUk += productAvailability.Amount;
                        else return baseProduct;
                    }

                    baseSetProduct.BaseProduct.ProductAvailabilities.Add(productAvailability);
                }

                return baseProduct;
            };

            string productSetsExpression =
                "SELECT " +
                "[ProductSet].* " +
                ", [BaseProduct].ID " +
                ", [BaseProduct].Created " +
                ", [BaseProduct].Deleted ";

            productSetsExpression += ProductSqlFragments.BaseProductCultureColumns;

            productSetsExpression +=
                ", [BaseProduct].HasAnalogue " +
                ", [BaseProduct].HasComponent " +
                ", [BaseProduct].HasImage " +
                ", [BaseProduct].[Image] " +
                ", [BaseProduct].IsForSale " +
                ", [BaseProduct].IsForWeb " +
                ", [BaseProduct].IsForZeroSale " +
                ", [BaseProduct].MainOriginalNumber " +
                ", [BaseProduct].MeasureUnitID " +
                ", [BaseProduct].NetUID " +
                ", [BaseProduct].OrderStandard " +
                ", [BaseProduct].PackingStandard " +
                ", [BaseProduct].Standard " +
                ", [BaseProduct].Size " +
                ", [BaseProduct].[Top] " +
                ", [BaseProduct].UCGFEA " +
                ", [BaseProduct].Updated " +
                ", [BaseProduct].VendorCode " +
                ", [BaseProduct].Volume " +
                ", [BaseProduct].[Weight] " +
                ",dbo.GetCalculatedProductPriceWithSharesAndVat([BaseProduct].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentPrice " +
                ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([BaseProduct].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentLocalPrice " +
                ",[ProductPricing].* " +
                ",[ProductAvailability].* " +
                ",[Storage].* " +
                ",[MeasureUnit].* " +
                ",[Organization].* " +
                "FROM [Product] AS [BaseProduct] " +
                "LEFT JOIN [ProductSet] " +
                "ON [ProductSet].BaseProductID = [BaseProduct].ID " +
                "LEFT JOIN [ProductPricing] " +
                "ON [ProductPricing].ProductID = [BaseProduct].ID " +
                "AND [ProductPricing].Deleted = 0 " +
                "LEFT JOIN [ProductAvailability] " +
                "ON [ProductAvailability].ProductID = [BaseProduct].ID " +
                "AND [ProductAvailability].Deleted = 0 " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductAvailability].StorageID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [BaseProduct].MeasureUnitID " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].StorageID = [Storage].ID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "WHERE [ProductSet].ComponentProductID IN @Ids " +
                "AND [Storage].ForDefective = 0 " +
                "AND [BaseProduct].ID IS NOT NULL " +
                "ORDER BY " +
                "CASE WHEN ([ProductAvailability].Amount <> 0 AND [Storage].Locale = @Culture) THEN 0 ELSE 1 END, " +
                "[BaseProduct].VendorCode, [BaseProduct].Name, " +
                "CASE WHEN [Storage].Locale = @Culture THEN 0 ELSE 1 END ";

            _connection.Query(
                productSetsExpression,
                types,
                productSetsMapper,
                new {
                    Ids = products.Where(p => !p.HasComponent).Select(p => p.Id),
                    ClientAgreementNetId = clientAgreementNetId,
                    Culture = currentCulture,
                    WithVat = withVat
                }
            );
        }

        foreach (Product product in products.SelectMany(p => p.BaseSetProducts.Select(a => a.BaseProduct))) {
            decimal currentExchangeRateEurToUah = _connection.Query<decimal>(
                "SELECT dbo.GetCurrentEuroExchangeRateFiltered(@ProductNetId, @CurrencyId, @WithVat, NULL)",
                new { ProductNetId = product.NetUid, CurrencyID = uahCurrencyId, WithVat = withVat }).FirstOrDefault();

            product.CurrentPriceEurToUah = decimal.Round(product.CurrentPrice * currentExchangeRateEurToUah, 2, MidpointRounding.AwayFromZero);
            product.CurrentPriceReSaleEurToUah = decimal.Round(product.CurrentPriceReSale * currentExchangeRateEurToUah, 2, MidpointRounding.AwayFromZero);
        }

        return products;
    }

    public List<FromSearchProduct> GetAllFromIdsInTempTable(
        string preDefinedQuery,
        Guid clientAgreementNetId,
        long? currencyId,
        long? organizationId,
        bool withVat = false,
        bool isDefault = false) {
        // Use Dictionary for O(1) lookup instead of O(n) List.Any/First
        Dictionary<long, FromSearchProduct> productDict = new();
        HashSet<(long, long)> productAvailabilityIds = new();

        Type[] types = {
            typeof(FromSearchProduct),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(ProductSlug),
            typeof(ProductImage),
            typeof(SupplyOrderItem),
            typeof(SupplyOrder),
            typeof(SupplyOrderUkraineItem),
            typeof(SupplyOrderUkraine)
        };

        Func<object[], FromSearchProduct> productsMapper = objects => {
            FromSearchProduct product = (FromSearchProduct)objects[0];
            ProductAvailability productAvailability = (ProductAvailability)objects[1];
            Storage storage = (Storage)objects[2];
            ProductSlug productSlug = (ProductSlug)objects[3];
            ProductImage image = (ProductImage)objects[4];
            SupplyOrderItem SupplyOrderItem = (SupplyOrderItem)objects[5];
            SupplyOrder SupplyOrder = (SupplyOrder)objects[6];
            SupplyOrderUkraineItem SupplyOrderUkraineItem = (SupplyOrderUkraineItem)objects[7];
            SupplyOrderUkraine SupplyOrderUkraine = (SupplyOrderUkraine)objects[8];

            // O(1) lookup with TryGetValue instead of O(n) Any + First
            if (!productDict.TryGetValue(product.Id, out FromSearchProduct existingProduct)) {
                product.ProductSlug = productSlug;
                if (SupplyOrderUkraine != null)
                    if (!SupplyOrderUkraine.IsPlaced)
                        product.AvailableQtyRoad += SupplyOrderUkraineItem.Qty;

                if (SupplyOrder != null)
                    if (!SupplyOrder.IsFullyPlaced)
                        product.AvailableQtyRoad += SupplyOrderItem.Qty;

                if (productAvailability != null) {
                    productAvailabilityIds.Add((productAvailability.Id, product.Id));
                    if (storage != null)
                        if (!storage.ForDefective) {
                            // Use StringComparison instead of ToLower() allocation
                            if (storage.Locale.Equals("pl", StringComparison.OrdinalIgnoreCase)) {
                                if (storage.ForVatProducts) {
                                    product.AvailableQtyPlVAT += productAvailability.Amount;
                                    product.AvailableQtyPl += productAvailability.Amount;
                                } else {
                                    product.AvailableQtyPl += productAvailability.Amount;
                                }
                            } else {
                                if (storage.ForVatProducts) {
                                    product.AvailableQtyUkVAT += productAvailability.Amount;
                                    product.AvailableQtyUk += productAvailability.Amount;
                                } else {
                                    product.AvailableQtyUk += productAvailability.Amount;
                                }
                            }
                        }
                }

                if (image != null && !image.Deleted) product.Image = image.ImageUrl;

                productDict[product.Id] = product;
            } else if (productAvailability != null) {
                FromSearchProduct fromList = existingProduct;
                if (SupplyOrderUkraine != null)
                    if (!SupplyOrderUkraine.IsPlaced)
                        fromList.AvailableQtyRoad += SupplyOrderUkraineItem.Qty;

                if (SupplyOrder != null)
                    if (!SupplyOrder.IsFullyPlaced)
                        fromList.AvailableQtyRoad += SupplyOrderItem.Qty;

                if (storage != null)
                    if (storage.ForDefective)
                        return product;

                // O(1) HashSet lookup instead of O(n) Any
                if (productAvailabilityIds.Contains((productAvailability.Id, product.Id))) return product;

                productAvailabilityIds.Add((productAvailability.Id, product.Id));
                if (storage != null) {
                    // Use StringComparison instead of ToLower() allocation
                    if (storage.Locale.Equals("pl", StringComparison.OrdinalIgnoreCase)) {
                        if (storage.ForVatProducts) {
                            fromList.AvailableQtyPlVAT += productAvailability.Amount;
                            fromList.AvailableQtyPl += productAvailability.Amount;
                        } else {
                            fromList.AvailableQtyPl += productAvailability.Amount;
                        }
                    } else {
                        if (storage.ForVatProducts) {
                            fromList.AvailableQtyUkVAT += productAvailability.Amount;
                            fromList.AvailableQtyUk += productAvailability.Amount;
                        } else {
                            fromList.AvailableQtyUk += productAvailability.Amount;
                        }
                    }
                }
            }

            return product;
        };

        var props = new {
            ClientAgreementNetId = clientAgreementNetId,
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
            WithVat = withVat,
            CurrencyId = currencyId,
            OrganizationId = organizationId
        };

        string sqlExpression =
            preDefinedQuery +
            "SELECT " +
            "[Product].ID " +
            ", [SearchResult].RowNumber AS SearchRowNumber ";

        sqlExpression += ProductSqlFragments.ProductCultureColumns;

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
            ", [Product].VendorCode " +
            ", [Product].Volume " +
            ", [Product].[Weight] " +
            ", dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS [CurrentPrice] " +
            ", dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS [CurrentLocalPrice] " +
            ", (SELECT Code FROM Currency " +
            "WHERE ID = @CurrencyId " +
            "AND Deleted = 0) AS CurrencyCode " +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[ProductSlug].* " +
            ",[ProductImage].* " +
            ",[SupplyOrderItem].* " +
            ",[SupplyOrder].* " +
            ",[SupplyOrderUkraineItem].* " +
            ",[SupplyOrderUkraine].* " +
            "FROM [Product] " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Product].ID " +
            // "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [ProductImage] " +
            "ON [ProductImage].ProductID = [Product].ID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "AND [Storage].Locale = @Culture " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].OrganizationID = @OrganizationId " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON SupplyOrderItem.ProductID = Product.ID " +
            "LEFT JOIN SupplyOrder " +
            "ON SupplyOrder.ID = [SupplyOrderItem].SupplyOrderID " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].ProductID = Product.ID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "JOIN dbo.#SearchResult AS [SearchResult] " +
            "ON [SearchResult].ID = [Product].ID " +
            "LEFT JOIN [ProductSlug] " +
            "ON [ProductSlug].ID = (" +
            "SELECT TOP(1) ID " +
            "FROM [ProductSlug] " +
            "WHERE [ProductSlug].Deleted = 0 " +
            "AND [ProductSlug].[Locale] = @Culture " +
            "AND [ProductSlug].ProductID = [Product].ID" +
            ") " +
            "WHERE [SearchResult].ID IS NOT NULL " +
            "ORDER BY [SearchResult].RowNumber";

        _connection.Query(
            sqlExpression,
            types,
            productsMapper,
            props
        );

        // Sort by SearchRowNumber to preserve the ranking from ProductSearchServiceOptimized
        return productDict.Values.OrderBy(p => p.SearchRowNumber).ToList();
    }

    public List<FromSearchProduct> GetAllFromIdsInTempTableForRetail(
        string preDefinedQuery,
        ClientAgreement clientAgreement) {
        // Use Dictionary for O(1) lookup instead of O(n) List.Any/First
        Dictionary<long, FromSearchProduct> productDict = new();
        HashSet<(long, long)> productAvailabilityIds = new();

        Type[] types = {
            typeof(FromSearchProduct),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(ProductSlug),
            typeof(ProductImage),
            typeof(SupplyOrderItem),
            typeof(SupplyOrder),
            typeof(SupplyOrderUkraineItem),
            typeof(SupplyOrderUkraine)
        };

        Func<object[], FromSearchProduct> productsMapper = objects => {
            FromSearchProduct product = (FromSearchProduct)objects[0];
            ProductAvailability productAvailability = (ProductAvailability)objects[1];
            Storage storage = (Storage)objects[2];
            ProductSlug productSlug = (ProductSlug)objects[3];
            ProductImage image = (ProductImage)objects[4];
            SupplyOrderItem SupplyOrderItem = (SupplyOrderItem)objects[5];
            SupplyOrder SupplyOrder = (SupplyOrder)objects[6];
            SupplyOrderUkraineItem SupplyOrderUkraineItem = (SupplyOrderUkraineItem)objects[7];
            SupplyOrderUkraine SupplyOrderUkraine = (SupplyOrderUkraine)objects[8];

            // O(1) lookup with TryGetValue instead of O(n) Any + First
            if (!productDict.TryGetValue(product.Id, out FromSearchProduct existingProduct)) {
                product.ProductSlug = productSlug;
                product.CurrentLocalPrice = decimal.Round(product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);
                product.CurrentPrice = decimal.Round(product.CurrentPrice, 2, MidpointRounding.AwayFromZero);

                if (SupplyOrderUkraine != null)
                    if (!SupplyOrderUkraine.IsPlaced)
                        product.AvailableQtyRoad += SupplyOrderUkraineItem.Qty;

                if (SupplyOrder != null)
                    if (!SupplyOrder.IsFullyPlaced)
                        product.AvailableQtyRoad += SupplyOrderItem.Qty;

                if (productAvailability != null) {
                    productAvailabilityIds.Add((productAvailability.Id, product.Id));
                    if (storage != null)
                        if (!storage.ForDefective) {
                            // Use StringComparison instead of ToLower() allocation
                            if (storage.Locale.Equals("pl", StringComparison.OrdinalIgnoreCase)) {
                                if (storage.ForVatProducts) {
                                    product.AvailableQtyPlVAT += productAvailability.Amount;
                                    product.AvailableQtyPl += productAvailability.Amount;
                                } else {
                                    product.AvailableQtyPl += productAvailability.Amount;
                                }
                            } else {
                                if (storage.ForVatProducts) {
                                    product.AvailableQtyUkVAT += productAvailability.Amount;
                                    product.AvailableQtyUk += productAvailability.Amount;
                                } else {
                                    product.AvailableQtyUk += productAvailability.Amount;
                                }
                            }
                        }
                }

                //if (image != null && !image.Deleted) {
                //    product.Image = image.ImageUrl;
                //}

                productDict[product.Id] = product;
            } else if (productAvailability != null) {
                FromSearchProduct fromList = existingProduct;
                if (SupplyOrderUkraine != null)
                    if (!SupplyOrderUkraine.IsPlaced)
                        fromList.AvailableQtyRoad += SupplyOrderUkraineItem.Qty;

                if (SupplyOrder != null)
                    if (!SupplyOrder.IsFullyPlaced)
                        fromList.AvailableQtyRoad += SupplyOrderItem.Qty;

                if (storage != null)
                    if (storage.ForDefective)
                        return product;

                // O(1) HashSet lookup instead of O(n) Any
                if (productAvailabilityIds.Contains((productAvailability.Id, product.Id))) return product;

                productAvailabilityIds.Add((productAvailability.Id, product.Id));

                if (storage != null) {
                    // Use StringComparison instead of ToLower() allocation
                    if (storage.Locale.Equals("pl", StringComparison.OrdinalIgnoreCase)) {
                        if (storage.ForVatProducts) {
                            fromList.AvailableQtyPlVAT += productAvailability.Amount;
                            fromList.AvailableQtyPl += productAvailability.Amount;
                        } else {
                            fromList.AvailableQtyPl += productAvailability.Amount;
                        }
                    } else {
                        if (storage.ForVatProducts) {
                            fromList.AvailableQtyUkVAT += productAvailability.Amount;
                            fromList.AvailableQtyUk += productAvailability.Amount;
                        } else {
                            fromList.AvailableQtyUk += productAvailability.Amount;
                        }
                    }
                }
            }

            return product;
        };

        var props = new {
            ClientAgreementNetId = clientAgreement.NetUid,
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
            WithVat = clientAgreement.Agreement.WithVATAccounting,
            clientAgreement.Agreement.CurrencyId,
            clientAgreement.Agreement.OrganizationId
        };

        string sqlExpression =
            preDefinedQuery +
            "SELECT " +
            "[Product].ID " +
            ", [SearchResult].RowNumber AS SearchRowNumber ";

        sqlExpression += ProductSqlFragments.ProductCultureColumns;

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
            ", [Product].VendorCode " +
            ", [Product].Volume " +
            ", [Product].[Weight] " +
            ", dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS [CurrentPrice] " +
            ", dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS [CurrentLocalPrice] " +
            ", (SELECT Code FROM Currency " +
            "WHERE ID = @CurrencyId " +
            // "AND Deleted = 0" +
            ") AS CurrencyCode " +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[ProductSlug].* " +
            ",[ProductImage].*" +
            ",[SupplyOrderItem].* " +
            ",[SupplyOrder].* " +
            ",[SupplyOrderUkraineItem].* " +
            ",[SupplyOrderUkraine].* " +
            "FROM [Product] " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Product].ID " +
            // "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [ProductImage] " +
            "ON [ProductImage].ProductID = [Product].ID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "AND [Storage].ForEcommerce = 1 " +
            "LEFT JOIN [SupplyOrderItem] " +
            "ON SupplyOrderItem.ProductID = Product.ID " +
            "LEFT JOIN SupplyOrder " +
            "ON SupplyOrder.ID = [SupplyOrderItem].SupplyOrderID " +
            "LEFT JOIN [SupplyOrderUkraineItem] " +
            "ON [SupplyOrderUkraineItem].ProductID = Product.ID " +
            "LEFT JOIN [SupplyOrderUkraine] " +
            "ON [SupplyOrderUkraine].ID = [SupplyOrderUkraineItem].SupplyOrderUkraineID " +
            "JOIN dbo.#SearchResult AS [SearchResult] " +
            "ON [SearchResult].ID = [Product].ID " +
            "LEFT JOIN [ProductSlug] " +
            "ON [ProductSlug].ID = (" +
            "SELECT TOP(1) ID " +
            "FROM [ProductSlug] " +
            // "WHERE [ProductSlug].Deleted = 0 " +
            "WHERE [ProductSlug].[Locale] = @Culture " +
            "AND [ProductSlug].ProductID = [Product].ID" +
            ") " +
            "WHERE [SearchResult].ID IS NOT NULL " +
            "ORDER BY [SearchResult].RowNumber";

        _connection.Query(
            sqlExpression,
            types,
            productsMapper,
            props
        );

        // Sort by SearchRowNumber to preserve the ranking from ProductSearchServiceOptimized
        return productDict.Values.OrderBy(p => p.SearchRowNumber).ToList();
    }

    public List<FromSearchProduct> GetAllFromIdsInTempTable(
        string preDefinedQuery,
        Guid nonVatAgreementNetId,
        Guid? vatAgreementNetId,
        long? organizationId) {
        // Use Dictionary for O(1) lookup instead of O(n) List.Any/First
        Dictionary<long, FromSearchProduct> productDict = new();

        Type[] types = {
            typeof(FromSearchProduct),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(ProductSlug),
            typeof(decimal)
        };

        Func<object[], FromSearchProduct> productsMapper = objects => {
            FromSearchProduct product = (FromSearchProduct)objects[0];
            ProductAvailability productAvailability = (ProductAvailability)objects[1];
            Storage storage = (Storage)objects[2];
            ProductSlug productSlug = (ProductSlug)objects[3];
            decimal localExchangeRate = (decimal)objects[4];

            // O(1) lookup with TryGetValue instead of O(n) Any + First
            if (!productDict.TryGetValue(product.Id, out FromSearchProduct existingProduct)) {
                product.ProductSlug = productSlug;
                product.CurrentLocalPrice = decimal.Round(product.CurrentPrice * localExchangeRate, 2, MidpointRounding.AwayFromZero);

                productDict[product.Id] = product;
            } else if (productAvailability != null) {
                product = existingProduct;
            }

            if (productAvailability == null || !storage.OrganizationId.Equals(organizationId) || storage.ForDefective) return product;

            // Use StringComparison instead of ToLower() allocation
            if (storage.Locale.Equals("pl", StringComparison.OrdinalIgnoreCase)) {
                if (storage.ForVatProducts)
                    product.AvailableQtyPlVAT += productAvailability.Amount;
                else
                    product.AvailableQtyPl += productAvailability.Amount;
            } else {
                if (storage.ForVatProducts)
                    product.AvailableQtyUkVAT += productAvailability.Amount;
                else
                    product.AvailableQtyUk += productAvailability.Amount;
            }

            return product;
        };

        var props = new {
            NonVatAgreementNetId = nonVatAgreementNetId,
            VatAgreementNetId = vatAgreementNetId ?? Guid.Empty,
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
        };

        string sqlExpression =
            preDefinedQuery +
            "SELECT " +
            "[Product].ID ";

        sqlExpression += ProductSqlFragments.ProductCultureColumns;

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
            ", [Product].VendorCode " +
            ", [Product].Volume " +
            ", [Product].[Weight] " +
            ",dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentWithVatPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentLocalWithVatPrice " +
            ",dbo.GetCalculatedProductPriceWithSharesAndVatWith(Product.NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS CurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS CurrentLocalPrice " +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[ProductSlug].* " +
            "FROM [Product] " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "JOIN dbo.#SearchResult AS [SearchResult] " +
            "ON [SearchResult].ID = [Product].ID " +
            "LEFT JOIN [ProductSlug] " +
            "ON [ProductSlug].ID = (" +
            "SELECT TOP(1) ID " +
            "FROM [ProductSlug] " +
            "WHERE [ProductSlug].Deleted = 0 " +
            "AND [ProductSlug].[Locale] = @Culture " +
            "AND [ProductSlug].ProductID = [Product].ID" +
            ") " +
            "WHERE [SearchResult].ID IS NOT NULL " +
            "AND [Storage].Locale = @Culture " +
            "AND [Storage].ForDefective = 0";

        sqlExpression += "ORDER BY [SearchResult].Available DESC, [SearchResult].HundredPercentMatch DESC, [Product].NameUA, [Product].VendorCode";

        _connection.Query(
            sqlExpression,
            types,
            productsMapper,
            props
        );

        return productDict.Values.ToList();
    }

    public List<Product> GetAllProductsByCarBrandNetId(Guid carBrandNetId, Guid nonVatAgreementNetId, Guid? vatAgreementNetId, long limit, long offset) {
        List<Product> products = new();

        _connection.Query<Product, MeasureUnit, ProductSlug, Product>(
            "; WITH [Search_CTE] " +
            "AS ( " +
            "SELECT [Product].ID " +
            ", ROW_NUMBER() OVER( " +
            "ORDER BY CASE WHEN MAX([ProductAvailability].[Amount]) > 0 THEN 0 ELSE 1 END, [Product].NameUA, [Product].VendorCode " +
            ") AS [RowNumber] " +
            "FROM [Product] " +
            "LEFT JOIN  [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = 0 " +
            "AND [Storage].ForDefective = 0 " +
            (
                vatAgreementNetId.HasValue
                    ? "AND [Storage].ForVatProducts = 1 "
                    : "AND [Storage].ForVatProducts = 0 "
            ) +
            "LEFT JOIN [ProductCarBrand] " +
            "ON [ProductCarBrand].ProductID = [Product].ID " +
            "AND [ProductCarBrand].Deleted = 0 " +
            "LEFT JOIN [CarBrand] " +
            "ON [CarBrand].ID = [ProductCarBrand].CarBrandID " +
            "WHERE [Product].Deleted = 0 " +
            "AND [CarBrand].NetUID = @CarBrandNetUID " +
            "AND ( " +
            "[Storage].ID IS NULL " +
            "OR " +
            "[Storage].Locale = @Culture " +
            ") " +
            "GROUP BY [Product].ID, [Product].NameUA, [Product].VendorCode " +
            ") " +
            "SELECT [Product].ID " +
            ProductSqlFragments.ProductCultureColumns +
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
            ", [Product].VendorCode " +
            ", [Product].Volume " +
            ", [Product].[Weight] " +
            ", ISNULL(( " +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'uk' " +
            "AND [Storage].ForVatProducts = 0 " +
            "), 0) AS [AvailableQtyUk] " +
            ", ISNULL(( " +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'uk' " +
            "AND [Storage].ForVatProducts = 1 " +
            "), 0) AS [AvailableQtyUkVAT] " +
            ", ISNULL(( " +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'pl' " +
            "AND [Storage].ForVatProducts = 0 " +
            "), 0) AS [AvailableQtyPl] " +
            ", ISNULL(( " +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'pl' " +
            "AND [Storage].ForVatProducts = 1 " +
            "), 0) AS [AvailableQtyPlVAT] " +
            ",dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, 0, NULL) AS [CurrentPrice] " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, 0, NULL) AS [CurrentLocalPrice] " +
            (
                vatAgreementNetId.HasValue
                    ? ",dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentWithVatPrice " +
                      ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentLocalWithVatPrice "
                    : string.Empty
            ) +
            ",[MeasureUnit].* " +
            ",[ProductSlug].* " +
            "FROM [Product] " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [Search_CTE] " +
            "ON [Search_CTE].ID = [Product].ID " +
            "LEFT JOIN [ProductSlug] " +
            "ON [ProductSlug].ID = (" +
            "SELECT TOP(1) ID " +
            "FROM [ProductSlug] " +
            "WHERE [ProductSlug].Deleted = 0 " +
            "AND [ProductSlug].[Locale] = @Culture " +
            "AND [ProductSlug].ProductID = [Product].ID" +
            ") " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            "ORDER BY [Search_CTE].RowNumber ",
            (product, measureUnit, productSlug) => {
                product.MeasureUnit = measureUnit;
                product.ProductSlug = productSlug;

                products.Add(product);

                return product;
            },
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Offset = offset,
                Limit = limit,
                ClientAgreementNetId = nonVatAgreementNetId,
                NonVatAgreementNetId = vatAgreementNetId ?? Guid.Empty,
                CarBrandNetUID = carBrandNetId
            }
        );

        return products;
    }

    public List<Product> GetAllProductsByCarBrandNetId(string carBrandAlias, Guid nonVatAgreementNetId, Guid? vatAgreementNetId, long limit, long offset) {
        List<Product> products = new();

        _connection.Query<Product, MeasureUnit, ProductSlug, Product>(
            "; WITH [Search_CTE] " +
            "AS ( " +
            "SELECT [Product].ID " +
            ", ROW_NUMBER() OVER( " +
            "ORDER BY CASE WHEN MAX([ProductAvailability].[Amount]) > 0 THEN 0 ELSE 1 END, [Product].NameUA, [Product].VendorCode " +
            ") AS [RowNumber] " +
            "FROM [Product] " +
            "LEFT JOIN  [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = 0 " +
            "AND [Storage].ForDefective = 0 " +
            (
                vatAgreementNetId.HasValue
                    ? "AND [Storage].ForVatProducts = 1 "
                    : "AND [Storage].ForVatProducts = 0 "
            ) +
            "LEFT JOIN [ProductCarBrand] " +
            "ON [ProductCarBrand].ProductID = [Product].ID " +
            "AND [ProductCarBrand].Deleted = 0 " +
            "LEFT JOIN [CarBrand] " +
            "ON [CarBrand].ID = [ProductCarBrand].CarBrandID " +
            "WHERE [Product].Deleted = 0 " +
            "AND [CarBrand].[Alias] = @CarBrandAlias " +
            "AND ( " +
            "[Storage].ID IS NULL " +
            "OR " +
            "[Storage].Locale = @Culture " +
            ") " +
            "GROUP BY [Product].ID, [Product].NameUA, [Product].VendorCode " +
            ") " +
            "SELECT [Product].ID " +
            ProductSqlFragments.ProductCultureColumns +
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
            ", [Product].VendorCode " +
            ", [Product].Volume " +
            ", [Product].[Weight] " +
            ", ISNULL(( " +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'uk' " +
            "AND [Storage].ForVatProducts = 0 " +
            "), 0) AS [AvailableQtyUk] " +
            ", ISNULL(( " +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'uk' " +
            "AND [Storage].ForVatProducts = 1 " +
            "), 0) AS [AvailableQtyUkVAT] " +
            ", ISNULL(( " +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'pl' " +
            "AND [Storage].ForVatProducts = 0 " +
            "), 0) AS [AvailableQtyPl] " +
            ", ISNULL(( " +
            "SELECT SUM([ProductAvailability].Amount) " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Locale = N'pl' " +
            "AND [Storage].ForVatProducts = 1 " +
            "), 0) AS [AvailableQtyPlVAT] " +
            ",dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, 0, NULL) AS [CurrentPrice] " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, 0, NULL) AS [CurrentLocalPrice] " +
            (
                vatAgreementNetId.HasValue
                    ? ",dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentWithVatPrice " +
                      ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentLocalWithVatPrice "
                    : string.Empty
            ) +
            ",[MeasureUnit].* " +
            ",[ProductSlug].* " +
            "FROM [Product] " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [Search_CTE] " +
            "ON [Search_CTE].ID = [Product].ID " +
            "LEFT JOIN [ProductSlug] " +
            "ON [ProductSlug].ID = (" +
            "SELECT TOP(1) ID " +
            "FROM [ProductSlug] " +
            "WHERE [ProductSlug].Deleted = 0 " +
            "AND [ProductSlug].[Locale] = @Culture " +
            "AND [ProductSlug].ProductID = [Product].ID" +
            ") " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            "ORDER BY [Search_CTE].RowNumber ",
            (product, measureUnit, productSlug) => {
                product.MeasureUnit = measureUnit;
                product.ProductSlug = productSlug;

                products.Add(product);

                return product;
            },
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Offset = offset,
                Limit = limit,
                ClientAgreementNetId = nonVatAgreementNetId,
                VatAgreementNetId = vatAgreementNetId ?? Guid.Empty,
                CarBrandAlias = carBrandAlias
            }
        );

        return products;
    }

    public List<Product> GetAllFromIdsInPreDefinedQuery(string preDefinedQuery, Guid nonVatAgreementNetId, Guid? vatAgreementNetId) {
        List<Product> products = new();

        Type[] types = {
            typeof(Product),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(ProductSlug)
        };

        Func<object[], Product> productsMapper = objects => {
            Product product = (Product)objects[0];
            ProductAvailability productAvailability = (ProductAvailability)objects[1];
            Storage storage = (Storage)objects[2];
            ProductSlug productSlug = (ProductSlug)objects[3];

            if (!products.Any(p => p.Id.Equals(product.Id))) {
                product.ProductSlug = productSlug;
                product.CurrentLocalPrice = decimal.Round(product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                if (productAvailability != null)
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
                                    product.AvailableQtyUk += productAvailability.Amount;
                            } else {
                                product.AvailableQtyUk += productAvailability.Amount;
                            }
                        }
                    }

                products.Add(product);
            } else if (productAvailability != null) {
                Product fromList = products.First(p => p.Id.Equals(product.Id));

                if (storage.ForDefective) return product;

                if (storage.Locale.ToLower().Equals("pl")) {
                    if (storage.ForVatProducts)
                        fromList.AvailableQtyPlVAT += productAvailability.Amount;
                    else
                        fromList.AvailableQtyPl += productAvailability.Amount;
                } else {
                    if (storage.ForVatProducts) {
                        fromList.AvailableQtyUkVAT += productAvailability.Amount;

                        if (storage.AvailableForReSale)
                            fromList.AvailableQtyUk += productAvailability.Amount;
                    } else {
                        fromList.AvailableQtyUk += productAvailability.Amount;
                    }
                }
            }

            return product;
        };

        var props = new {
            NonVatAgreementNetId = nonVatAgreementNetId,
            VatAgreementNetId = vatAgreementNetId ?? Guid.Empty,
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
        };

        string sqlExpression =
            "SELECT " +
            "[Product].ID ";

        sqlExpression += ProductSqlFragments.ProductCultureColumns;

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
            ", [Product].VendorCode " +
            ", [Product].Volume " +
            ", [Product].[Weight] ";

        if (vatAgreementNetId.HasValue)
            sqlExpression +=
                ",dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentWithVatPrice " +
                ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentLocalWithVatPrice ";

        sqlExpression +=
            ",dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS CurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS CurrentLocalPrice " +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[ProductSlug].* " +
            "FROM [Product] " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "LEFT JOIN [ProductSlug] " +
            "ON [ProductSlug].ID = (" +
            "SELECT TOP(1) ID " +
            "FROM [ProductSlug] " +
            "WHERE [ProductSlug].Deleted = 0 " +
            "AND [ProductSlug].[Locale] = @Culture " +
            "AND [ProductSlug].ProductID = [Product].ID" +
            ") " +
            "WHERE [Product].ID IN (" +
            preDefinedQuery +
            ") " +
            "AND [Storage].Locale = @Culture " +
            "AND [Storage].ForDefective = 0 ";

        sqlExpression += "ORDER BY [ProductAvailability].[Amount] DESC, [Product].NameUA, [Product].VendorCode";

        _connection.Query(
            sqlExpression,
            types,
            productsMapper,
            props
        );

        if (products.All(p => !p.HasAnalogue && !p.HasComponent)) return products;

        if (products.Any(p => p.HasAnalogue)) {
            Type[] analoguesTypes = {
                typeof(Product),
                typeof(ProductAvailability),
                typeof(Storage),
                typeof(ProductAnalogue),
                typeof(ProductSlug)
            };

            Func<object[], Product> analoguesMapper = objects => {
                Product product = (Product)objects[0];
                ProductAvailability productAvailability = (ProductAvailability)objects[1];
                Storage storage = (Storage)objects[2];
                ProductAnalogue analogue = (ProductAnalogue)objects[3];
                ProductSlug productSlug = (ProductSlug)objects[4];

                if (analogue == null) return product;

                Product parent = products.First(p => p.Id.Equals(analogue.BaseProductId));

                if (!parent.AnalogueProducts.Any(a => a.Id.Equals(analogue.Id))) {
                    product.ProductSlug = productSlug;
                    product.CurrentLocalPrice = decimal.Round(product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                    if (productAvailability != null)
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
                                        product.AvailableQtyUk += productAvailability.Amount;
                                } else {
                                    product.AvailableQtyUk += productAvailability.Amount;
                                }
                            }
                        }

                    analogue.AnalogueProduct = product;

                    parent.AnalogueProducts.Add(analogue);
                } else if (productAvailability != null) {
                    analogue = parent.AnalogueProducts.First(a => a.Id.Equals(analogue.Id));

                    if (storage.ForDefective) return product;

                    if (storage.Locale.ToLower().Equals("pl")) {
                        if (storage.ForVatProducts)
                            analogue.AnalogueProduct.AvailableQtyPlVAT += productAvailability.Amount;
                        else
                            analogue.AnalogueProduct.AvailableQtyPl += productAvailability.Amount;
                    } else {
                        if (storage.ForVatProducts) {
                            analogue.AnalogueProduct.AvailableQtyUkVAT += productAvailability.Amount;

                            if (storage.AvailableForReSale)
                                analogue.AnalogueProduct.AvailableQtyUk += productAvailability.Amount;
                        } else {
                            analogue.AnalogueProduct.AvailableQtyUk += productAvailability.Amount;
                        }
                    }
                }

                return product;
            };

            var analogueProps = new {
                ProductIds = products.Where(p => p.HasAnalogue).Select(p => p.Id),
                NonVatAgreementNetId = nonVatAgreementNetId,
                VatAgreementNetId = vatAgreementNetId ?? Guid.Empty,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            };

            string analoguesSqlExpression =
                "SELECT " +
                "[Product].ID ";

            analoguesSqlExpression += ProductSqlFragments.ProductCultureColumns;

            analoguesSqlExpression +=
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
                ", [Product].VendorCode " +
                ", [Product].Volume " +
                ", [Product].[Weight] ";

            if (vatAgreementNetId.HasValue)
                analoguesSqlExpression +=
                    ",dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentWithVatPrice " +
                    ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentLocalWithVatPrice ";

            analoguesSqlExpression +=
                ",dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS CurrentPrice " +
                ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS CurrentLocalPrice " +
                ",[ProductAvailability].* " +
                ",[Storage].* " +
                ",[ProductAnalogue].* " +
                ",[ProductSlug].* " +
                "FROM [Product] " +
                "LEFT JOIN [ProductAvailability] " +
                "ON [ProductAvailability].ProductID = [Product].ID " +
                "AND [ProductAvailability].Deleted = 0 " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductAvailability].StorageID " +
                "LEFT JOIN [ProductAnalogue] " +
                "ON [ProductAnalogue].AnalogueProductID = [Product].ID " +
                "AND [ProductAnalogue].Deleted = 0 " +
                "LEFT JOIN [ProductSlug] " +
                "ON [ProductSlug].ID = (" +
                "SELECT TOP(1) ID " +
                "FROM [ProductSlug] " +
                "WHERE [ProductSlug].Deleted = 0 " +
                "AND [ProductSlug].[Locale] = @Culture " +
                "AND [ProductSlug].ProductID = [Product].ID" +
                ") " +
                "WHERE [ProductAnalogue].BaseProductID IN @ProductIds " +
                "AND [Storage].Locale = @Culture " +
                "AND [Storage].ForDefective = 0";

            analoguesSqlExpression += "ORDER BY [ProductAvailability].[Amount] DESC, [Product].NameUA, [Product].VendorCode";

            _connection.Query(
                analoguesSqlExpression,
                analoguesTypes,
                analoguesMapper,
                analogueProps
            );
        }

        if (!products.Any(p => p.HasComponent)) return products;

        Type[] componentsTypes = {
            typeof(Product),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(ProductSet),
            typeof(ProductSlug)
        };

        Func<object[], Product> componentsMapper = objects => {
            Product product = (Product)objects[0];
            ProductAvailability productAvailability = (ProductAvailability)objects[1];
            Storage storage = (Storage)objects[2];
            ProductSet productSet = (ProductSet)objects[3];
            ProductSlug productSlug = (ProductSlug)objects[4];

            if (productSet == null) return product;

            Product parent = products.First(p => p.Id.Equals(productSet.BaseProductId));

            if (!parent.AnalogueProducts.Any(a => a.Id.Equals(productSet.Id))) {
                product.ProductSlug = productSlug;
                product.CurrentLocalPrice = decimal.Round(product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                if (productAvailability != null)
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
                                    product.AvailableQtyUk += productAvailability.Amount;
                            } else {
                                product.AvailableQtyUk += productAvailability.Amount;
                            }
                        }
                    }

                productSet.ComponentProduct = product;

                parent.ComponentProducts.Add(productSet);
            } else if (productAvailability != null) {
                productSet = parent.ComponentProducts.First(a => a.Id.Equals(productSet.Id));

                if (storage.ForDefective) return product;

                if (storage.Locale.ToLower().Equals("pl")) {
                    if (storage.ForVatProducts)
                        productSet.ComponentProduct.AvailableQtyPlVAT += productAvailability.Amount;
                    else
                        productSet.ComponentProduct.AvailableQtyPl += productAvailability.Amount;
                } else {
                    if (storage.ForVatProducts) {
                        productSet.ComponentProduct.AvailableQtyUkVAT += productAvailability.Amount;

                        if (storage.AvailableForReSale)
                            productSet.ComponentProduct.AvailableQtyUk += productAvailability.Amount;
                    } else {
                        productSet.ComponentProduct.AvailableQtyUk += productAvailability.Amount;
                    }
                }
            }

            return product;
        };

        var componentsProps = new {
            ProductIds = products.Where(p => p.HasComponent).Select(p => p.Id),
            NonVatAgreementNetId = nonVatAgreementNetId,
            VatAgreementNetId = vatAgreementNetId ?? Guid.Empty,
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
        };

        string componentsSqlExpression =
            "SELECT " +
            "[Product].ID ";

        componentsSqlExpression += ProductSqlFragments.ProductCultureColumns;

        componentsSqlExpression +=
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
            ", [Product].VendorCode " +
            ", [Product].Volume " +
            ", [Product].[Weight] ";

        if (vatAgreementNetId.HasValue)
            componentsSqlExpression +=
                ",dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentWithVatPrice " +
                ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentLocalWithVatPrice ";

        componentsSqlExpression +=
            ",dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS CurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS CurrentLocalPrice " +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[ProductSet].* " +
            ",[ProductSlug].* " +
            "FROM [Product] " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "LEFT JOIN [ProductSet] " +
            "ON [ProductSet].ComponentProductID = [Product].ID " +
            "AND [ProductSet].Deleted = 0 " +
            "LEFT JOIN [ProductSlug] " +
            "ON [ProductSlug].ID = (" +
            "SELECT TOP(1) ID " +
            "FROM [ProductSlug] " +
            "WHERE [ProductSlug].Deleted = 0 " +
            "AND [ProductSlug].[Locale] = @Culture " +
            "AND [ProductSlug].ProductID = [Product].ID" +
            ") " +
            "WHERE [ProductSet].BaseProductID IN @ProductIds " +
            "AND [Storage].Locale = @Culture " +
            "AND [Storage].ForDefective = 0";

        componentsSqlExpression += "ORDER BY [ProductAvailability].[Amount] DESC, [Product].NameUA, [Product].VendorCode";

        _connection.Query(
            componentsSqlExpression,
            componentsTypes,
            componentsMapper,
            componentsProps
        );

        return products;
    }

    public List<FromSearchProduct> GetProductsByOldECommerceIds(
        IEnumerable<long> oldECommerceIds,
        Guid nonVatAgreementNetId,
        Guid? vatAgreementNetId
    ) {
        List<FromSearchProduct> products = new();

        Type[] types = {
            typeof(FromSearchProduct),
            typeof(ProductAvailability),
            typeof(Storage)
        };

        Func<object[], FromSearchProduct> productsMapper = objects => {
            FromSearchProduct product = (FromSearchProduct)objects[0];
            ProductAvailability productAvailability = (ProductAvailability)objects[1];
            Storage storage = (Storage)objects[2];

            if (!products.Any(p => p.Id.Equals(product.Id))) {
                product.CurrentLocalPrice = decimal.Round(product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                if (productAvailability != null)
                    if (!storage.ForDefective) {
                        if (storage.Locale.ToLower().Equals("pl")) {
                            if (storage.ForVatProducts)
                                product.AvailableQtyPlVAT += productAvailability.Amount;
                            else
                                product.AvailableQtyPl += productAvailability.Amount;
                        } else {
                            if (storage.ForVatProducts)
                                product.AvailableQtyUkVAT += productAvailability.Amount;
                            else
                                product.AvailableQtyUk += productAvailability.Amount;
                        }
                    }

                products.Add(product);
            } else if (productAvailability != null) {
                FromSearchProduct fromList = products.First(p => p.Id.Equals(product.Id));

                if (storage.ForDefective) return product;

                if (storage.Locale.ToLower().Equals("pl")) {
                    if (storage.ForVatProducts)
                        fromList.AvailableQtyPlVAT += productAvailability.Amount;
                    else
                        fromList.AvailableQtyPl += productAvailability.Amount;
                } else {
                    if (storage.ForVatProducts)
                        fromList.AvailableQtyUkVAT += productAvailability.Amount;
                    else
                        fromList.AvailableQtyUk += productAvailability.Amount;
                }
            }

            return product;
        };

        var props = new {
            OldEcommerceIds = oldECommerceIds,
            NonVatAgreementNetId = nonVatAgreementNetId,
            VatAgreementNetId = vatAgreementNetId ?? Guid.Empty,
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
        };

        string sqlExpression =
            "SELECT " +
            "[Product].ID ";

        sqlExpression += ProductSqlFragments.ProductCultureColumns;

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
            ", [Product].VendorCode " +
            ", [Product].Volume " +
            ", [Product].[Weight] ";

        if (vatAgreementNetId.HasValue)
            sqlExpression +=
                ",dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentWithVatPrice " +
                ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentLocalWithVatPrice ";

        sqlExpression +=
            ",dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS CurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS CurrentLocalPrice " +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            "FROM [Product] " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Product].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE ([Product].SourceAmgCode IN @OldEcommerceIds " +
            "OR [Product].SourceFenixCode IN @SourceFenixId) " +
            "AND [Storage].Locale = @Culture " +
            "AND [Storage].ForDefective = 0";

        sqlExpression += "ORDER BY (CASE WHEN [ProductAvailability].Amount <> 0 THEN 0 ELSE 1 END), [Product].NameUA, [Product].VendorCode";

        _connection.Query(
            sqlExpression,
            types,
            productsMapper,
            props
        );

        return products;
    }

    public List<Product> GetAllByOldECommerceIds(
        IEnumerable<long> oldECommerceIds,
        Guid nonVatAgreementNetId,
        Guid? vatAgreementNetId
    ) {
        Type[] types = {
            typeof(Product),
            typeof(ProductPricing),
            typeof(ProductAvailability)
        };

        Func<object[], Product> productsMapper = objects => {
            Product product = (Product)objects[0];
            ProductPricing productPricing = (ProductPricing)objects[1];
            ProductAvailability productAvailability = (ProductAvailability)objects[2];

            if (productPricing != null) {
                productPricing.Price = product.CurrentPrice;

                product.ProductPricings.Add(productPricing);
            }

            product.CurrentLocalPrice = decimal.Round(product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

            product.ProductAvailabilities.Add(productAvailability);

            return product;
        };

        var props = new {
            OldEcommerceIds = oldECommerceIds,
            NonVatAgreementNetId = nonVatAgreementNetId,
            VatAgreementNetId = vatAgreementNetId ?? Guid.Empty,
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
        };

        string sqlExpression =
            "SELECT " +
            "[Product].ID " +
            ", [Product].Created " +
            ", [Product].Deleted ";

        sqlExpression += ProductSqlFragments.ProductCultureColumns;

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
            ", [Product].[Weight] ";

        if (vatAgreementNetId.HasValue)
            sqlExpression +=
                ",dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentWithVatPrice " +
                ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentLocalWithVatPrice ";

        sqlExpression +=
            ",dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS CurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS CurrentLocalPrice " +
            ",[ProductPricing].ID " +
            ",[ProductPricing].Price " +
            ",[ProductAvailability].* " +
            "FROM [Product] " +
            "LEFT JOIN [ProductPricing] " +
            "ON [ProductPricing].ProductID = [Product].ID " +
            "AND [ProductPricing].Deleted = 0 " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ID = (" +
            "SELECT TOP(1) [ProductAvailability].ID " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = [Product].ID " +
            "AND [Storage].Locale = @Culture " +
            "AND [Storage].ForDefective = 0 " +
            "AND [ProductAvailability].Deleted = 0" +
            ") " +
            "WHERE ([Product].SourceAmgCode IN @OldEcommerceIds " +
            "OR [Product].SourceFenixCode IN @OldEcommerceIds) ";

        List<Product> products = _connection.Query(
                sqlExpression,
                types,
                productsMapper,
                props
            )
            .ToList();

        if (products.Any(p => p.HasAnalogue)) {
            types = new[] {
                typeof(Product),
                typeof(ProductAnalogue),
                typeof(Product),
                typeof(ProductPricing),
                typeof(ProductAvailability)
            };

            Func<object[], Product> analogueMapper = objects => {
                Product product = (Product)objects[0];
                ProductAnalogue productAnalogue = (ProductAnalogue)objects[1];
                Product analogue = (Product)objects[2];
                ProductPricing productPricing = (ProductPricing)objects[3];
                ProductAvailability productAvailability = (ProductAvailability)objects[4];

                Product fromList = products.First(p => p.Id.Equals(product.Id));

                if (fromList.AnalogueProducts.Any(a => a.Id.Equals(productAnalogue.Id))) return product;

                if (productPricing != null) {
                    productPricing.Price = analogue.CurrentPrice;

                    analogue.ProductPricings.Add(productPricing);
                }

                analogue.CurrentLocalPrice = decimal.Round(analogue.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                analogue.ProductAvailabilities.Add(productAvailability);

                productAnalogue.AnalogueProduct = analogue;

                fromList.AnalogueProducts.Add(productAnalogue);

                return product;
            };

            string analoguesExpression =
                "SELECT " +
                "[Product].ID " +
                ",[ProductAnalogue].* " +
                ", [Analogue].ID " +
                ", [Analogue].Created " +
                ", [Analogue].Deleted ";

            analoguesExpression += ProductSqlFragments.AnalogueCultureColumns;

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
                ", [Analogue].[Weight] ";

            if (vatAgreementNetId.HasValue)
                analoguesExpression +=
                    ",dbo.GetCalculatedProductPriceWithSharesAndVat(Analogue.NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentWithVatPrice " +
                    ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Analogue.NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentLocalWithVatPrice ";

            analoguesExpression +=
                ",dbo.GetCalculatedProductPriceWithSharesAndVat([Analogue].NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS CurrentPrice " +
                ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Analogue].NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS CurrentLocalPrice " +
                ",[ProductPricing].* " +
                ",[ProductAvailability].* " +
                "FROM [Product] " +
                "LEFT JOIN [ProductAnalogue] " +
                "ON [ProductAnalogue].BaseProductID = [Product].ID " +
                "LEFT JOIN [Product] AS [Analogue] " +
                "ON [Analogue].ID = [ProductAnalogue].AnalogueProductID " +
                "LEFT JOIN [ProductPricing] " +
                "ON [ProductPricing].ProductID = [Analogue].ID " +
                "AND [ProductPricing].Deleted = 0 " +
                "LEFT JOIN [ProductAvailability] " +
                "ON [ProductAvailability].ID = (" +
                "SELECT TOP(1) [ProductAvailability].ID " +
                "FROM [ProductAvailability] " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductAvailability].StorageID " +
                "WHERE [ProductAvailability].ProductID = [Analogue].ID " +
                "AND [Storage].Locale = @Culture " +
                "AND [ProductAvailability].Deleted = 0" +
                ") " +
                "WHERE [Product].ID IN @Ids " +
                "AND [Analogue].ID IS NOT NULL " +
                "ORDER BY [Analogue].Name";

            _connection.Query(
                analoguesExpression,
                types,
                analogueMapper,
                new {
                    Ids = products.Where(p => p.HasAnalogue).Select(p => p.Id),
                    NonVatAgreementNetId = nonVatAgreementNetId,
                    VatAgreementNetId = vatAgreementNetId ?? Guid.Empty,
                    Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }

        if (!products.Any(p => p.HasComponent)) return products;

        types = new[] {
            typeof(Product),
            typeof(ProductSet),
            typeof(Product),
            typeof(ProductPricing),
            typeof(ProductAvailability)
        };

        Func<object[], Product> componentMapper = objects => {
            Product product = (Product)objects[0];
            ProductSet productSet = (ProductSet)objects[1];
            Product component = (Product)objects[2];
            ProductPricing productPricing = (ProductPricing)objects[3];
            ProductAvailability productAvailability = (ProductAvailability)objects[4];

            Product fromList = products.First(p => p.Id.Equals(product.Id));

            if (fromList.ComponentProducts.Any(a => a.Id.Equals(productSet.Id))) return product;

            if (productPricing != null) {
                productPricing.Price = component.CurrentPrice;

                component.ProductPricings.Add(productPricing);
            }

            component.CurrentLocalPrice = decimal.Round(component.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

            component.ProductAvailabilities.Add(productAvailability);

            productSet.ComponentProduct = component;

            fromList.ComponentProducts.Add(productSet);

            return product;
        };

        string componentsExpression =
            "SELECT " +
            "[Product].ID " +
            ",[ProductSet].* " +
            ", [Component].ID " +
            ", [Component].Created " +
            ", [Component].Deleted ";

        componentsExpression += ProductSqlFragments.ComponentCultureColumns;

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
            ", [Component].[Weight] ";

        if (vatAgreementNetId.HasValue)
            componentsExpression +=
                ",dbo.GetCalculatedProductPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentWithVatPrice " +
                ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat(Product.NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentLocalWithVatPrice ";

        componentsExpression +=
            ",dbo.GetCalculatedProductPriceWithSharesAndVat([Component].NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS CurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Component].NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS CurrentLocalPrice " +
            ",[ProductPricing].* " +
            ",[ProductAvailability].* " +
            "FROM [Product] " +
            "LEFT JOIN [ProductSet] " +
            "ON [ProductSet].BaseProductID = [Product].ID " +
            "LEFT JOIN [Product] AS [Component] " +
            "ON [Component].ID = [ProductSet].ComponentProductID " +
            "LEFT JOIN [ProductPricing] " +
            "ON [ProductPricing].ProductID = [Component].ID " +
            "AND [ProductPricing].Deleted = 0 " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ID = (" +
            "SELECT TOP(1) [ProductAvailability].ID " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductAvailability].ProductID = [Component].ID " +
            "AND [Storage].Locale = @Culture " +
            "AND [ProductAvailability].Deleted = 0 " +
            ") " +
            "WHERE [Product].ID IN @Ids " +
            "AND [Component].ID IS NOT NULL " +
            "ORDER BY [Component].Name";

        _connection.Query(
            componentsExpression,
            types,
            componentMapper,
            new {
                Ids = products.Where(p => p.HasComponent).Select(p => p.Id),
                NonVatAgreementNetId = nonVatAgreementNetId,
                VatAgreementNetId = vatAgreementNetId ?? Guid.Empty,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        return products;
    }

    public List<FromSearchProduct> GetAllAnaloguesByProductIdWithCalculatedPrices(long productId, Guid nonVatAgreementNetId, Guid? vatAgreementNetId) {
        List<FromSearchProduct> analogues = new();

        Type[] types = {
            typeof(FromSearchProduct),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(ProductSlug)
        };

        Func<object[], FromSearchProduct> mapper = objects => {
            FromSearchProduct analogue = (FromSearchProduct)objects[0];
            ProductAvailability productAvailability = (ProductAvailability)objects[1];
            Storage storage = (Storage)objects[2];
            ProductSlug productSlug = (ProductSlug)objects[3];

            if (!analogues.Any(a => a.Id.Equals(analogue.Id))) {
                analogue.ProductSlug = productSlug;
                analogue.CurrentLocalPrice = decimal.Round(analogue.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                if (!storage.ForDefective) {
                    if (storage.Locale.ToLower().Equals("pl")) {
                        if (storage.ForVatProducts)
                            analogue.AvailableQtyPlVAT += productAvailability.Amount;
                        else
                            analogue.AvailableQtyPl += productAvailability.Amount;
                    } else {
                        if (storage.ForVatProducts)
                            analogue.AvailableQtyUkVAT += productAvailability.Amount;
                        else
                            analogue.AvailableQtyUk += productAvailability.Amount;
                    }
                }

                analogues.Add(analogue);
            } else {
                FromSearchProduct analogueFromList = analogues.First(a => a.Id.Equals(analogue.Id));

                if (storage.ForDefective) return analogue;

                if (storage.Locale.ToLower().Equals("pl")) {
                    if (storage.ForVatProducts)
                        analogueFromList.AvailableQtyPlVAT += productAvailability.Amount;
                    else
                        analogueFromList.AvailableQtyPl += productAvailability.Amount;
                } else {
                    if (storage.ForVatProducts)
                        analogueFromList.AvailableQtyUkVAT += productAvailability.Amount;
                    else
                        analogueFromList.AvailableQtyUk += productAvailability.Amount;
                }
            }

            return analogue;
        };

        string sqlExpression =
            "SELECT " +
            "[Analogue].ID " +
            ", [Analogue].Created " +
            ", [Analogue].Deleted ";

        sqlExpression += ProductSqlFragments.AnalogueCultureColumns;

        sqlExpression +=
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
            ", [Analogue].[Weight] ";

        if (vatAgreementNetId.HasValue)
            sqlExpression +=
                ",dbo.GetCalculatedProductPriceWithSharesAndVat([Analogue].NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentWithVatPrice " +
                ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Analogue].NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentLocalWithVatPrice ";

        sqlExpression +=
            ",dbo.GetCalculatedProductPriceWithSharesAndVat([Analogue].NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS CurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Analogue].NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS CurrentLocalPrice " +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[ProductSlug].* " +
            "FROM [ProductAnalogue] " +
            "LEFT JOIN [Product] AS [Analogue] " +
            "ON [Analogue].ID = [ProductAnalogue].AnalogueProductID " +
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
            "WHERE [ProductAnalogue].BaseProductID = @Id " +
            "AND [ProductAnalogue].Deleted = 0 " +
            "AND [Storage].Locale = @Culture " +
            "AND [Storage].ForDefective = 0 " +
            "ORDER BY [ProductAvailability].Amount DESC, [Analogue].Name, [Analogue].VendorCode";

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new {
                Id = productId,
                NonVatAgreementNetId = nonVatAgreementNetId,
                VatAgreementNetId = vatAgreementNetId ?? Guid.Empty,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        return analogues;
    }

    public List<FromSearchProduct> GetAllAnaloguesByProductIdAndOrganizationIdWithCalculatedPrices(
        long productId,
        Guid clientAgreementNetId,
        long? organizationId,
        long? currencyId,
        bool withVat) {
        List<FromSearchProduct> analogues = new();
        List<ProductAvailability> productAvailabilities = new();

        Type[] types = {
            typeof(FromSearchProduct),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(ProductSlug),
            typeof(ProductImage),
            typeof(string)
        };

        Func<object[], FromSearchProduct> mapper = objects => {
            FromSearchProduct analogue = (FromSearchProduct)objects[0];
            ProductAvailability productAvailability = (ProductAvailability)objects[1];
            Storage storage = (Storage)objects[2];
            ProductSlug productSlug = (ProductSlug)objects[3];
            ProductImage image = (ProductImage)objects[4];
            string currencyCode = (string)objects[5];

            if (!analogues.Any(a => a.Id.Equals(analogue.Id))) {
                analogue.ProductSlug = productSlug;
                analogue.CurrentPrice = decimal.Round(analogue.CurrentPrice, 2, MidpointRounding.AwayFromZero);
                analogue.CurrentLocalPrice = decimal.Round(analogue.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);
                analogue.CurrencyCode = currencyCode;

                if (image != null && !image.Deleted) analogue.Image = image.ImageUrl;

                if (productAvailability == null) return analogue;

                productAvailabilities.Add(productAvailability);

                if (storage.Locale.ToLower().Equals("pl"))
                    analogue.AvailableQtyPl += productAvailability.Amount;
                else
                    analogue.AvailableQtyUk += productAvailability.Amount;

                analogues.Add(analogue);
            } else {
                analogue = analogues.First(a => a.Id.Equals(analogue.Id));

                if (productAvailability == null || storage.ForDefective) return analogue;

                if (productAvailabilities.Any(p => p.Id.Equals(productAvailability.Id) && p.ProductId.Equals(analogue.Id))) return analogue;

                productAvailabilities.Add(productAvailability);
                if (storage.Locale.ToLower().Equals("pl"))
                    analogue.AvailableQtyPl += productAvailability.Amount;
                else
                    analogue.AvailableQtyUk += productAvailability.Amount;
            }

            return analogue;
        };

        string sqlExpression =
            "SELECT " +
            "[Analogue].ID " +
            ", [Analogue].Created " +
            ", [Analogue].Deleted ";

        sqlExpression += ProductSqlFragments.AnalogueCultureColumns;

        sqlExpression +=
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
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[ProductSlug].* " +
            ",[ProductImage].* " +
            ", (SELECT Code FROM Currency " +
            "WHERE ID = @CurrencyId " +
            "AND Deleted = 0) AS CurrencyCode " +
            "FROM [ProductAnalogue] " +
            "LEFT JOIN [Product] AS [Analogue] " +
            "ON [Analogue].ID = [ProductAnalogue].AnalogueProductID " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Analogue].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
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
            "WHERE [ProductAnalogue].BaseProductID = @Id " +
            "AND [ProductAnalogue].Deleted = 0 " +
            "AND [Storage].Locale = @Culture " +
            "AND [Storage].ForDefective = 0 " +
            "AND [Storage].Deleted = 0 ";

        if (withVat)
            sqlExpression +=
                "AND [Storage].OrganizationID = @OrganizationId " +
                "ORDER BY [ProductAvailability].Amount DESC, [Analogue].Name, [Analogue].VendorCode ";
        else
            sqlExpression +=
                "AND [Storage].ID IN " +
                "(SELECT [Storage].ID FROM [Storage] " +
                "WHERE [Storage].Locale = @Culture " +
                "AND [Storage].Deleted = 0" +
                "AND [Storage].ForDefective = 0 " +
                "AND [Storage].AvailableForReSale = 1" +
                "UNION " +
                "SELECT [Storage].ID FROM [Storage] " +
                "WHERE [Storage].OrganizationID = @OrganizationId " +
                "AND [Storage].Deleted = 0 " +
                "AND [Storage].ForDefective = 0 " +
                "AND [Storage].Locale = @Culture) " +
                "ORDER BY [ProductAvailability].Amount DESC, [Analogue].Name, [Analogue].VendorCode ";

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new {
                Id = productId,
                ClientAgreementNetId = clientAgreementNetId,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                CurrencyId = currencyId,
                OrganizationId = organizationId,
                WithVat = withVat
            },
            splitOn: "ID,CurrencyCode"
        );

        return analogues;
    }

    public List<FromSearchProduct> GetAllAnaloguesByProductIdAndOrganizationIdWithCalculatedPricesForRetail(
        long productId,
        Guid clientAgreementNetId,
        long? organizationId,
        long? currencyId,
        bool withVat) {
        List<FromSearchProduct> analogues = new();
        List<ProductAvailability> productAvailabilities = new();

        Type[] types = {
            typeof(FromSearchProduct),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(ProductSlug),
            typeof(ProductImage),
            typeof(string)
        };

        Func<object[], FromSearchProduct> mapper = objects => {
            FromSearchProduct analogue = (FromSearchProduct)objects[0];
            ProductAvailability productAvailability = (ProductAvailability)objects[1];
            Storage storage = (Storage)objects[2];
            ProductSlug productSlug = (ProductSlug)objects[3];
            ProductImage image = (ProductImage)objects[4];
            string currencyCode = (string)objects[5];

            if (!analogues.Any(a => a.Id.Equals(analogue.Id))) {
                analogue.ProductSlug = productSlug;
                analogue.CurrentPrice = decimal.Round(analogue.CurrentPrice, 2, MidpointRounding.AwayFromZero);
                analogue.CurrentLocalPrice = decimal.Round(analogue.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);
                analogue.CurrencyCode = currencyCode;

                if (image != null && !image.Deleted) analogue.Image = image.ImageUrl;

                if (productAvailability == null) return analogue;

                productAvailabilities.Add(productAvailability);

                if (storage.Locale.ToLower().Equals("pl"))
                    analogue.AvailableQtyPl += productAvailability.Amount;
                else
                    analogue.AvailableQtyUk += productAvailability.Amount;

                analogues.Add(analogue);
            } else {
                analogue = analogues.First(a => a.Id.Equals(analogue.Id));

                if (productAvailability == null || storage.ForDefective) return analogue;

                if (productAvailabilities.Any(p => p.Id.Equals(productAvailability.Id) && p.ProductId.Equals(analogue.Id))) return analogue;

                productAvailabilities.Add(productAvailability);
                if (storage.Locale.ToLower().Equals("pl"))
                    analogue.AvailableQtyPl += productAvailability.Amount;
                else
                    analogue.AvailableQtyUk += productAvailability.Amount;
            }

            return analogue;
        };

        string sqlExpression =
            "SELECT " +
            "[Analogue].ID " +
            ", [Analogue].Created " +
            ", [Analogue].Deleted ";

        sqlExpression += ProductSqlFragments.AnalogueCultureColumns;

        sqlExpression +=
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
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[ProductSlug].* " +
            ",[ProductImage].* " +
            ", (SELECT Code FROM Currency " +
            "WHERE ID = @CurrencyId " +
            "AND Deleted = 0) AS CurrencyCode " +
            "FROM [ProductAnalogue] " +
            "LEFT JOIN [Product] AS [Analogue] " +
            "ON [Analogue].ID = [ProductAnalogue].AnalogueProductID " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ProductID = [Analogue].ID " +
            "AND [ProductAvailability].Deleted = 0 " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
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
            "WHERE [ProductAnalogue].BaseProductID = @Id " +
            "AND [ProductAnalogue].Deleted = 0 " +
            "AND [Storage].ForEcommerce = 1 " +
            "AND [Storage].Deleted = 0 " +
            "ORDER BY [ProductAvailability].Amount DESC, [Analogue].Name, [Analogue].VendorCode ";

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new {
                Id = productId,
                ClientAgreementNetId = clientAgreementNetId,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                CurrencyId = currencyId,
                OrganizationId = organizationId,
                WithVat = true
            },
            splitOn: "ID,CurrencyCode"
        );

        return analogues;
    }

    public List<FromSearchProduct> GetAllComponentsByProductIdWithCalculatedPrices(long productId, Guid nonVatAgreementNetId, Guid? vatAgreementNetId) {
        List<FromSearchProduct> components = new();

        Type[] types = {
            typeof(FromSearchProduct),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(ProductSlug)
        };

        Func<object[], FromSearchProduct> mapper = objects => {
            FromSearchProduct component = (FromSearchProduct)objects[0];
            ProductAvailability productAvailability = (ProductAvailability)objects[1];
            Storage storage = (Storage)objects[2];
            ProductSlug productSlug = (ProductSlug)objects[3];

            if (!components.Any(a => a.Id.Equals(component.Id))) {
                component.ProductSlug = productSlug;
                component.CurrentLocalPrice = decimal.Round(component.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                if (!storage.ForDefective) {
                    if (storage.Locale.ToLower().Equals("pl")) {
                        if (storage.ForVatProducts)
                            component.AvailableQtyPlVAT += productAvailability.Amount;
                        else
                            component.AvailableQtyPl += productAvailability.Amount;
                    } else {
                        if (storage.ForVatProducts)
                            component.AvailableQtyUkVAT += productAvailability.Amount;
                        else
                            component.AvailableQtyUk += productAvailability.Amount;
                    }
                }

                components.Add(component);
            } else {
                FromSearchProduct analogueFromList = components.First(a => a.Id.Equals(component.Id));

                if (storage.ForDefective) return component;

                if (storage.Locale.ToLower().Equals("pl")) {
                    if (storage.ForVatProducts)
                        analogueFromList.AvailableQtyPlVAT += productAvailability.Amount;
                    else
                        analogueFromList.AvailableQtyPl += productAvailability.Amount;
                } else {
                    if (storage.ForVatProducts)
                        analogueFromList.AvailableQtyUkVAT += productAvailability.Amount;
                    else
                        analogueFromList.AvailableQtyUk += productAvailability.Amount;
                }
            }

            return component;
        };

        string sqlExpression =
            "SELECT " +
            "[Component].ID " +
            ", [Component].Created " +
            ", [Component].Deleted ";

        sqlExpression += ProductSqlFragments.ComponentCultureColumns;

        sqlExpression +=
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
            ", [Component].[Weight] ";

        if (vatAgreementNetId.HasValue)
            sqlExpression +=
                ",dbo.GetCalculatedProductPriceWithSharesAndVat([Component].NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentWithVatPrice " +
                ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Component].NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentLocalWithVatPrice ";

        sqlExpression +=
            ",dbo.GetCalculatedProductPriceWithSharesAndVat([Component].NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS CurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Component].NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS CurrentLocalPrice " +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[ProductSlug].* " +
            "FROM [ProductSet] " +
            "LEFT JOIN [Product] AS [Component] " +
            "ON [Component].ID = [ProductSet].ComponentProductID " +
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
            "WHERE [ProductSet].BaseProductID = @Id " +
            "AND [ProductSet].Deleted = 0 " +
            "AND [Storage].Locale = @Culture " +
            "AND [Storage].ForDefective = 0 " +
            "ORDER BY [ProductAvailability].Amount DESC, [Component].Name, [Component].VendorCode";

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new {
                Id = productId,
                NonVatAgreementNetId = nonVatAgreementNetId,
                VatAgreementNetId = vatAgreementNetId ?? Guid.Empty,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        return components;
    }

    public List<FromSearchProduct> GetAllComponentsByProductIdWithCalculatedPrices(
        long productId,
        Guid nonVatAgreementNetId,
        Guid? vatAgreementNetId,
        long? organizationId) {
        List<FromSearchProduct> components = new();

        Type[] types = {
            typeof(FromSearchProduct),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(ProductSlug)
        };

        Func<object[], FromSearchProduct> mapper = objects => {
            FromSearchProduct component = (FromSearchProduct)objects[0];
            ProductAvailability productAvailability = (ProductAvailability)objects[1];
            Storage storage = (Storage)objects[2];
            ProductSlug productSlug = (ProductSlug)objects[3];

            if (!components.Any(a => a.Id.Equals(component.Id))) {
                component.ProductSlug = productSlug;
                component.CurrentLocalPrice = decimal.Round(component.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                components.Add(component);
            } else {
                component = components.First(a => a.Id.Equals(component.Id));
            }

            if (productAvailability == null || !storage.OrganizationId.Equals(organizationId) || storage.ForDefective) return component;

            if (storage.Locale.ToLower().Equals("pl")) {
                if (storage.ForVatProducts)
                    component.AvailableQtyPlVAT += productAvailability.Amount;
                else
                    component.AvailableQtyPl += productAvailability.Amount;
            } else {
                if (storage.ForVatProducts)
                    component.AvailableQtyUkVAT += productAvailability.Amount;
                else
                    component.AvailableQtyUk += productAvailability.Amount;
            }

            return component;
        };

        string sqlExpression =
            "SELECT " +
            "[Component].ID " +
            ", [Component].Created " +
            ", [Component].Deleted ";

        sqlExpression += ProductSqlFragments.ComponentCultureColumns;

        sqlExpression +=
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
            ", [Component].[Weight] ";

        if (vatAgreementNetId.HasValue)
            sqlExpression +=
                ",dbo.GetCalculatedProductPriceWithSharesAndVat([Component].NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentWithVatPrice " +
                ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Component].NetUID, @VatAgreementNetId, @Culture, 1, NULL) AS CurrentLocalWithVatPrice ";

        sqlExpression +=
            ",dbo.GetCalculatedProductPriceWithSharesAndVat([Component].NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS CurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Component].NetUID, @NonVatAgreementNetId, @Culture, 0, NULL) AS CurrentLocalPrice " +
            ",[ProductAvailability].* " +
            ",[Storage].* " +
            ",[ProductSlug].* " +
            "FROM [ProductSet] " +
            "LEFT JOIN [Product] AS [Component] " +
            "ON [Component].ID = [ProductSet].ComponentProductID " +
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
            "WHERE [ProductSet].BaseProductID = @Id " +
            "AND [ProductSet].Deleted = 0 " +
            "AND [Storage].Locale = @Culture " +
            "AND [Storage].ForDefective = 0 " +
            "ORDER BY [ProductAvailability].Amount DESC, [Component].Name, [Component].VendorCode";

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new {
                Id = productId,
                NonVatAgreementNetId = nonVatAgreementNetId,
                VatAgreementNetId = vatAgreementNetId ?? Guid.Empty,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        return components;
    }

    public List<Product> GetAll(long limit, long offset) {
        List<Product> products = new();

        var props = new { Offset = offset, Limit = limit, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY [Product].ID DESC) AS RowNumber " +
            ", [Product].ID " +
            "FROM [Product] " +
            "WHERE [Product].Deleted = 0 " +
            ") " +
            "SELECT ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            "ORDER BY RowNumber ";

        IEnumerable<long> productIds =
            _connection.Query<long>(
                sqlExpression,
                props
            );

        string fullSqlExpression =
            "SELECT * FROM Product " +
            "LEFT OUTER JOIN ProductOriginalNumber " +
            "ON Product.ID = ProductOriginalNumber.ProductID AND ProductOriginalNumber.Deleted = 0 " +
            "LEFT OUTER JOIN OriginalNumber " +
            "ON ProductOriginalNumber.OriginalNumberID = OriginalNumber.ID AND OriginalNumber.Deleted = 0 " +
            "LEFT OUTER JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID AND ProductProductGroup.Deleted = 0 " +
            "LEFT OUTER JOIN ProductGroup " +
            "ON ProductProductGroup.ProductGroupID = ProductGroup.ID AND ProductGroup.Deleted = 0 " +
            "LEFT OUTER JOIN MeasureUnit " +
            "ON Product.MeasureUnitId = MeasureUnit.Id AND MeasureUnit.Deleted = 0 " +
            "LEFT OUTER JOIN ProductCategory " +
            "ON ProductCategory.ProductID = Product.Id AND ProductCategory.Deleted = 0 " +
            "LEFT OUTER JOIN Category " +
            "ON ProductCategory.CategoryID = Category.ID AND Category.Deleted = 0 " +
            "LEFT OUTER JOIN ProductAnalogue " +
            "ON ProductAnalogue.BaseProductID = Product.ID " +
            "LEFT JOIN Product analogueProduct " +
            "ON analogueProduct.ID = ProductAnalogue.AnalogueProductID " +
            "LEFT OUTER JOIN Storage " +
            "ON Product.StorageId = Storage.Id AND Storage.Deleted = 0 " +
            "LEFT OUTER JOIN ProductPricing " +
            "ON ProductPricing.ProductId = Product.Id " +
            "LEFT OUTER JOIN Pricing " +
            "ON ProductPricing.PricingId = Pricing.Id AND Pricing.Deleted = 0 " +
            "LEFT OUTER JOIN Currency " +
            "ON Pricing.CurrencyId = Currency.Id AND Currency.Deleted = 0 " +
            "LEFT OUTER JOIN ProductSet " +
            "ON ProductSet.BaseProductId = Product.Id AND ProductSet.Deleted = 0 " +
            "LEFT OUTER JOIN Product AS ProductComponent " +
            "ON ProductComponent.Id = ProductSet.ComponentProductId " +
            "LEFT OUTER JOIN ProductPricing AS ComponentProductPricing " +
            "ON ProductComponent.Id = ComponentProductPricing.ProductId " +
            "LEFT OUTER JOIN ProductAvailability " +
            "ON ProductAvailability.ProductId = Product.Id AND ProductAvailability.Deleted = 0 " +
            "LEFT OUTER JOIN Storage AS ProductAvailabilityStorage " +
            "ON ProductAvailabilityStorage.Id = ProductAvailability.StorageId AND ProductAvailabilityStorage.Deleted = 0 " +
            "WHERE Product.Id IN @Ids";

        Type[] types = {
            typeof(Product),
            typeof(ProductOriginalNumber),
            typeof(OriginalNumber),
            typeof(ProductProductGroup),
            typeof(ProductGroup),
            typeof(MeasureUnit),
            typeof(ProductCategory),
            typeof(Category),
            typeof(ProductAnalogue),
            typeof(Product),
            typeof(ProductPricing),
            typeof(Pricing),
            typeof(Currency),
            typeof(ProductSet),
            typeof(Product),
            typeof(ProductPricing),
            typeof(ProductAvailability),
            typeof(Storage)
        };

        Func<object[], Product> mapper = objects => {
            Product product = (Product)objects[0];
            ProductOriginalNumber productOriginalNumber = (ProductOriginalNumber)objects[1];
            OriginalNumber originalNumber = (OriginalNumber)objects[2];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[3];
            ProductGroup productGroup = (ProductGroup)objects[4];
            MeasureUnit measureUnit = (MeasureUnit)objects[5];
            ProductCategory productCategory = (ProductCategory)objects[6];
            Category category = (Category)objects[7];
            ProductAnalogue productAnalogue = (ProductAnalogue)objects[8];
            Product analogueProduct = (Product)objects[9];
            ProductPricing productPricing = (ProductPricing)objects[10];
            Pricing pricing = (Pricing)objects[11];
            Currency currency = (Currency)objects[12];
            ProductSet productSet = (ProductSet)objects[13];
            Product componentProduct = (Product)objects[14];
            ProductPricing componentProductPricing = (ProductPricing)objects[15];
            ProductAvailability productAvailability = (ProductAvailability)objects[16];
            Storage productAvailabilityStorage = (Storage)objects[17];

            if (productAvailability != null && productAvailabilityStorage != null) {
                productAvailability.Storage = productAvailabilityStorage;
                product.ProductAvailabilities.Add(productAvailability);
            }

            if (measureUnit != null) product.MeasureUnit = measureUnit;

            if (productProductGroup != null && productGroup != null) {
                productProductGroup.ProductGroup = productGroup;
                product.ProductProductGroups.Add(productProductGroup);
            }

            if (productOriginalNumber != null && originalNumber != null) {
                productOriginalNumber.OriginalNumber = originalNumber;
                product.ProductOriginalNumbers.Add(productOriginalNumber);
            }

            if (productCategory != null && category != null) {
                productCategory.Category = category;
                product.ProductCategories.Add(productCategory);
            }

            if (productSet != null && componentProduct != null) {
                if (componentProductPricing != null) componentProduct.ProductPricings.Add(componentProductPricing);

                productSet.ComponentProduct = componentProduct;
                product.ComponentProducts.Add(productSet);
            }

            if (productAnalogue != null && analogueProduct != null) {
                productAnalogue.AnalogueProduct = analogueProduct;
                product.AnalogueProducts.Add(productAnalogue);
            }

            if (productPricing != null) {
                if (pricing != null) {
                    pricing.Currency = currency;
                    productPricing.Pricing = pricing;
                }

                product.ProductPricings.Add(productPricing);
            }

            if (products.Any(p => p.Id.Equals(product.Id))) {
                Product productInList = products.First(p => p.Id.Equals(product.Id));

                if (productOriginalNumber != null && !productInList.ProductOriginalNumbers.Any(o => o.Id.Equals(productOriginalNumber.Id)))
                    productInList.ProductOriginalNumbers.Add(productOriginalNumber);

                if (productProductGroup != null && !productInList.ProductProductGroups.Any(o => o.Id.Equals(productProductGroup.Id)))
                    productInList.ProductProductGroups.Add(productProductGroup);

                if (productCategory != null && !productInList.ProductCategories.Any(c => c.Id.Equals(productCategory.Id))) productInList.ProductCategories.Add(productCategory);

                if (productSet != null && !productInList.ComponentProducts.Any(a => a.Id.Equals(productSet.Id))) productInList.ComponentProducts.Add(productSet);

                if (productAnalogue != null && !productInList.AnalogueProducts.Any(a => a.Id.Equals(productAnalogue.Id))) productInList.AnalogueProducts.Add(productAnalogue);

                if (productPricing != null && !productInList.ProductPricings.Any(a => a.Id.Equals(productPricing.Id))) productInList.ProductPricings.Add(productPricing);
            } else {
                products.Add(product);
            }

            return product;
        };

        var fullProps = new { Offset = offset, Limit = limit, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, Ids = productIds };

        _connection.Query(fullSqlExpression, types, mapper, fullProps);

        return products;
    }

    public List<Product> GetAll(string orderBy, long limit, long offset) {
        List<Product> products = new();

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY [Product].ID DESC) AS RowNumber " +
            ", [Product].ID " +
            "FROM [Product] " +
            "WHERE [Product].Deleted = 0 " +
            ") " +
            "SELECT ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            "ORDER BY RowNumber ";

        IEnumerable<long> productIds = _connection.Query<long>(
            sqlExpression,
            new { Limit = limit, Offset = offset, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        string fullSqlExpression =
            "SELECT * FROM Product " +
            "LEFT OUTER JOIN ProductOriginalNumber " +
            "ON Product.ID = ProductOriginalNumber.ProductID AND ProductOriginalNumber.Deleted = 0 " +
            "LEFT OUTER JOIN OriginalNumber " +
            "ON ProductOriginalNumber.OriginalNumberID = OriginalNumber.ID AND OriginalNumber.Deleted = 0 " +
            "LEFT OUTER JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID AND ProductProductGroup.Deleted = 0 " +
            "LEFT OUTER JOIN ProductGroup " +
            "ON ProductProductGroup.ProductGroupID = ProductGroup.ID AND ProductGroup.Deleted = 0 " +
            "LEFT OUTER JOIN MeasureUnit " +
            "ON Product.MeasureUnitId = MeasureUnit.ID AND MeasureUnit.Deleted = 0 " +
            "LEFT OUTER JOIN ProductCategory " +
            "ON ProductCategory.ProductID = Product.ID AND ProductCategory.Deleted = 0 " +
            "LEFT OUTER JOIN Category " +
            "ON ProductCategory.CategoryID = Category.ID AND Category.Deleted = 0 " +
            "LEFT OUTER JOIN ProductAnalogue " +
            "ON ProductAnalogue.BaseProductID = Product.ID " +
            "LEFT JOIN Product analogueProduct " +
            "ON analogueProduct.ID = ProductAnalogue.AnalogueProductID " +
            "LEFT OUTER JOIN ProductPricing " +
            "ON ProductPricing.ProductId = Product.Id " +
            "LEFT OUTER JOIN Pricing " +
            "ON ProductPricing.PricingId = Pricing.Id AND Pricing.Deleted = 0 " +
            "LEFT OUTER JOIN Currency " +
            "ON Pricing.CurrencyId = Currency.Id AND Currency.Deleted = 0 " +
            "LEFT OUTER JOIN ProductSet " +
            "ON ProductSet.BaseProductId = Product.Id AND ProductSet.Deleted = 0 " +
            "LEFT OUTER JOIN Product AS ProductComponent " +
            "ON ProductComponent.Id = ProductSet.ComponentProductId " +
            "LEFT OUTER JOIN ProductPricing AS ComponentProductPricing " +
            "ON ProductComponent.Id = ComponentProductPricing.ProductId " +
            "LEFT OUTER JOIN ProductAvailability " +
            "ON ProductAvailability.ProductId = Product.Id AND ProductAvailability.Deleted = 0 " +
            "LEFT OUTER JOIN Storage AS ProductAvailabilityStorage " +
            "ON ProductAvailabilityStorage.Id = ProductAvailability.StorageId AND ProductAvailabilityStorage.Deleted = 0 " +
            "LEFT JOIN ProductAvailability AS AnalogueAvailability " +
            "ON analogueProduct.ID = AnalogueAvailability.ProductID " +
            "LEFT JOIN Storage AS AnalogueAvailabilityStorage " +
            "ON AnalogueAvailability.StorageID = AnalogueAvailabilityStorage.ID " +
            "LEFT JOIN ProductPricing AS AnalogueProductPricing " +
            "ON AnalogueProductPricing.ProductID = analogueProduct.ID " +
            "LEFT JOIN Pricing AS AnaloguePricing " +
            "ON AnaloguePricing.ID = AnalogueProductPricing.PricingID " +
            "LEFT JOIN Currency AS AnaloguePricingCurrency " +
            "ON AnaloguePricingCurrency.ID = AnaloguePricing.CurrencyID " +
            "LEFT JOIN ProductProductGroup AS AnalogueProductProductGroup " +
            "ON AnalogueProductProductGroup.ProductID = analogueProduct.ID " +
            "WHERE Product.ID IN @Ids " +
            orderBy;

        Type[] types = {
            typeof(Product),
            typeof(ProductOriginalNumber),
            typeof(OriginalNumber),
            typeof(ProductProductGroup),
            typeof(ProductGroup),
            typeof(MeasureUnit),
            typeof(ProductCategory),
            typeof(Category),
            typeof(ProductAnalogue),
            typeof(Product),
            typeof(ProductPricing),
            typeof(Pricing),
            typeof(Currency),
            typeof(ProductSet),
            typeof(Product),
            typeof(ProductPricing),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(ProductPricing),
            typeof(Pricing),
            typeof(Currency),
            typeof(ProductProductGroup)
        };

        Func<object[], Product> mapper = objects => {
            Product product = (Product)objects[0];
            ProductOriginalNumber productOriginalNumber = (ProductOriginalNumber)objects[1];
            OriginalNumber originalNumber = (OriginalNumber)objects[2];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[3];
            ProductGroup productGroup = (ProductGroup)objects[4];
            MeasureUnit measureUnit = (MeasureUnit)objects[5];
            ProductCategory productCategory = (ProductCategory)objects[6];
            Category category = (Category)objects[7];
            ProductAnalogue productAnalogue = (ProductAnalogue)objects[8];
            Product analogueProduct = (Product)objects[9];
            ProductPricing productPricing = (ProductPricing)objects[10];
            Pricing pricing = (Pricing)objects[11];
            Currency currency = (Currency)objects[12];
            ProductSet productSet = (ProductSet)objects[13];
            Product componentProduct = (Product)objects[14];
            ProductPricing componentProductPricing = (ProductPricing)objects[15];
            ProductAvailability productAvailability = (ProductAvailability)objects[16];
            Storage productAvailabilityStorage = (Storage)objects[17];
            ProductAvailability analogueProductAvailability = (ProductAvailability)objects[18];
            Storage analogueAvailabilityStorage = (Storage)objects[19];
            ProductPricing analogueProductPricing = (ProductPricing)objects[20];
            Pricing analoguePricing = (Pricing)objects[21];
            Currency analoguePricingCurrency = (Currency)objects[22];
            ProductProductGroup analogueProductProductGroup = (ProductProductGroup)objects[23];

            if (productAvailability != null && productAvailabilityStorage != null) {
                productAvailability.Storage = productAvailabilityStorage;
                product.ProductAvailabilities.Add(productAvailability);
            }

            if (measureUnit != null) product.MeasureUnit = measureUnit;

            if (productProductGroup != null && productGroup != null) {
                productProductGroup.ProductGroup = productGroup;
                product.ProductProductGroups.Add(productProductGroup);
            }

            if (productOriginalNumber != null && originalNumber != null) {
                productOriginalNumber.OriginalNumber = originalNumber;
                product.ProductOriginalNumbers.Add(productOriginalNumber);
            }

            if (productCategory != null && category != null) {
                productCategory.Category = category;
                product.ProductCategories.Add(productCategory);
            }

            if (productSet != null && componentProduct != null) {
                if (componentProductPricing != null) componentProduct.ProductPricings.Add(componentProductPricing);

                productSet.ComponentProduct = componentProduct;
                product.ComponentProducts.Add(productSet);
            }

            if (productAnalogue != null && analogueProduct != null) {
                if (analogueProductAvailability != null && analogueAvailabilityStorage != null) {
                    analogueProductAvailability.Storage = analogueAvailabilityStorage;

                    analogueProduct.ProductAvailabilities.Add(analogueProductAvailability);
                }

                if (analogueProductPricing != null && analoguePricing != null) {
                    analoguePricing.Currency = analoguePricingCurrency;
                    analogueProductPricing.Pricing = analoguePricing;

                    analogueProduct.ProductPricings.Add(analogueProductPricing);
                }

                if (analogueProductProductGroup != null) analogueProduct.ProductProductGroups.Add(analogueProductProductGroup);

                productAnalogue.AnalogueProduct = analogueProduct;
                product.AnalogueProducts.Add(productAnalogue);
            }

            if (productPricing != null) {
                if (pricing != null) {
                    pricing.Currency = currency;
                    productPricing.Pricing = pricing;
                }

                product.ProductPricings.Add(productPricing);
            }

            if (products.Any(p => p.Id.Equals(product.Id))) {
                Product productInList = products.First(p => p.Id.Equals(product.Id));

                if (productOriginalNumber != null && !productInList.ProductOriginalNumbers.Any(o => o.Id.Equals(productOriginalNumber.Id)))
                    productInList.ProductOriginalNumbers.Add(productOriginalNumber);

                if (productProductGroup != null && !productInList.ProductProductGroups.Any(o => o.Id.Equals(productProductGroup.Id)))
                    productInList.ProductProductGroups.Add(productProductGroup);

                if (productCategory != null && !productInList.ProductCategories.Any(c => c.Id.Equals(productCategory.Id))) productInList.ProductCategories.Add(productCategory);

                if (productSet != null && !productInList.ComponentProducts.Any(a => a.Id.Equals(productSet.Id))) productInList.ComponentProducts.Add(productSet);

                if (productAnalogue != null && !productInList.AnalogueProducts.Any(a => a.Id.Equals(productAnalogue.Id))) {
                    productInList.AnalogueProducts.Add(productAnalogue);
                } else {
                    if (productAnalogue != null && analogueProduct != null) {
                        if (analogueProductAvailability != null && analogueAvailabilityStorage != null && !productInList.AnalogueProducts
                                .First(a => a.Id.Equals(productAnalogue.Id)).AnalogueProduct.ProductAvailabilities.Any(a => a.Id.Equals(analogueProductAvailability.Id))) {
                            analogueProductAvailability.Storage = analogueAvailabilityStorage;

                            productInList.AnalogueProducts.First(a => a.Id.Equals(productAnalogue.Id)).AnalogueProduct.ProductAvailabilities.Add(analogueProductAvailability);
                        }

                        if (analogueProductPricing != null && analoguePricing != null && !productInList.AnalogueProducts.First(a => a.Id.Equals(productAnalogue.Id))
                                .AnalogueProduct.ProductPricings.Any(p => p.Id.Equals(analogueProductPricing.Id))) {
                            analoguePricing.Currency = analoguePricingCurrency;
                            analogueProductPricing.Pricing = analoguePricing;

                            productInList.AnalogueProducts.First(a => a.Id.Equals(productAnalogue.Id)).AnalogueProduct.ProductPricings.Add(analogueProductPricing);
                        }

                        if (analogueProductProductGroup != null && !productInList.AnalogueProducts.First(a => a.Id.Equals(productAnalogue.Id)).AnalogueProduct
                                .ProductProductGroups.Any(p => p.Id.Equals(analogueProductProductGroup.Id)))
                            productInList.AnalogueProducts.First(a => a.Id.Equals(productAnalogue.Id)).AnalogueProduct.ProductProductGroups.Add(analogueProductProductGroup);
                    }
                }

                if (productPricing != null && !productInList.ProductPricings.Any(a => a.Id.Equals(productPricing.Id))) productInList.ProductPricings.Add(productPricing);
            } else {
                products.Add(product);
            }

            return product;
        };

        var fullProps = new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, Ids = productIds };

        _connection.Query(fullSqlExpression, types, mapper, fullProps);

        return products;
    }

    public List<Product> GetAllAnaloguesByProductNetId(Guid productNetId, Guid clientAgreementNetId, long? organizationId) {
        List<Product> products = new();

        bool withVat = _connection.Query<bool>(
            "SELECT [Agreement].[WithVATAccounting] FROM [ClientAgreement] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "WHERE [ClientAgreement].[NetUID] = @NetId; ",
            new { NetId = clientAgreementNetId }).FirstOrDefault();

        long uahCurrencyId = _connection.Query<long>(
            "SELECT ID FROM Currency " +
            "WHERE Currency.Code = 'UAH'").FirstOrDefault();

        // decimal currentExchangeRateEurToUah = _connection.Query<decimal>(
        //     "SELECT TOP 1 [ExchangeRate].[Amount] FROM [ExchangeRate] " +
        //     "WHERE [ExchangeRate].[Deleted] = 0 " +
        //     "AND [ExchangeRate].[Code] = 'EUR' " +
        //     "AND [ExchangeRate].[CurrencyID] = ( " +
        //     "SELECT TOP 1 [Currency].[ID] FROM [Currency] " +
        //     "WHERE [Currency].[Code] = 'UAH' " +
        //     "AND [Currency].[Deleted] = 0 " +
        //     ") ").FirstOrDefault();

        string analoguesExpression =
            "SELECT [Product].ID " +
            ",[ProductAnalogue].* " +
            ",[Analogue].ID " +
            ",[Analogue].Deleted " +
            ",[Analogue].HasAnalogue " +
            ",[Analogue].HasComponent " +
            ",[Analogue].HasImage " +
            ",[Analogue].Created " +
            ",[Analogue].Image " +
            ",[Analogue].IsForSale " +
            ",[Analogue].IsForWeb " +
            ",[Analogue].IsForZeroSale " +
            ",[Analogue].MainOriginalNumber " +
            ",[Analogue].MeasureUnitID " +
            ",[Analogue].NetUID " +
            ",[Analogue].OrderStandard " +
            ",[Analogue].PackingStandard " +
            ",[Analogue].Standard " +
            ",[Analogue].Size " +
            ",[Analogue].[Top] " +
            ",[Analogue].UCGFEA ";

        analoguesExpression += ProductSqlFragments.AnalogueNameDescriptionColumns;

        analoguesExpression +=
            ",[Analogue].Updated " +
            ",[Analogue].VendorCode " +
            ",[Analogue].[SynonymsUA] " +
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
            ",[Organization].* " +
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
            "LEFT JOIN [Organization] " +
            "ON [Organization].StorageID = [Storage].ID " +
            "WHERE [Product].NetUID = @ProductNetId " +
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
            typeof(Pricing),
            typeof(Organization)
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
            Organization organization = (Organization)objects[14];

            if (analogue == null) return product;

            if (!products.Any(a => a.Id.Equals(analogue.Id))) {
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

                if (productPricing != null) {
                    productPricing.Pricing = pricing;

                    analogue.ProductPricings.Add(productPricing);
                }

                // analogue.CurrentPriceEurToUah = decimal.Round(analogue.CurrentPrice * currentExchangeRateEurToUah, 2, MidpointRounding.AwayFromZero);
                analogue.MeasureUnit = measureUnit;

                products.Add(analogue);
            } else {
                Product analogueFromList = products.First(a => a.Id.Equals(analogue.Id));

                if (productOriginalNumber != null && !analogueFromList.ProductOriginalNumbers.Any(n => n.Id.Equals(productOriginalNumber.Id))) {
                    productOriginalNumber.OriginalNumber = originalNumber;

                    analogueFromList.ProductOriginalNumbers.Add(productOriginalNumber);
                }

                if (productProductGroup != null && !analogueFromList.ProductProductGroups.Any(g => g.Id.Equals(productProductGroup.Id))) {
                    productProductGroup.ProductGroup = productGroup;

                    analogueFromList.ProductProductGroups.Add(productProductGroup);
                }

                if (productCategory != null && !analogueFromList.ProductCategories.Any(c => c.Id.Equals(productCategory.Id))) {
                    productCategory.Category = category;

                    analogueFromList.ProductCategories.Add(productCategory);
                }

                if (productAvailability != null && !analogueFromList.ProductAvailabilities.Any(e => e.Id.Equals(productAvailability.Id))) {
                    productAvailability.Storage = storage;

                    if (!storage.ForDefective) {
                        if (storage.OrganizationId.Equals(organizationId))
                            if (storage.Locale.ToLower().Equals("pl")) {
                                if (storage.ForVatProducts)
                                    analogueFromList.AvailableQtyPlVAT += productAvailability.Amount;
                                else
                                    analogueFromList.AvailableQtyPl += productAvailability.Amount;
                            } else {
                                if (storage.ForVatProducts) {
                                    analogueFromList.AvailableQtyUkVAT += productAvailability.Amount;

                                    if (storage.AvailableForReSale)
                                        analogueFromList.AvailableQtyUk += productAvailability.Amount;
                                } else {
                                    analogueFromList.AvailableQtyUk += productAvailability.Amount;
                                }
                            }
                        else if (storage.AvailableForReSale && !storage.Locale.ToLower().Equals("pl"))
                            analogueFromList.AvailableQtyUk += productAvailability.Amount;
                        else if (organization != null && organization.StorageId.Equals(storage.Id))
                            analogueFromList.AvailableQtyUk += productAvailability.Amount;
                    }

                    analogueFromList.ProductAvailabilities.Add(productAvailability);
                }

                if (productPricing == null || analogueFromList.ProductPricings.Any(p => p.Id.Equals(productPricing.Id))) return product;

                productPricing.Pricing = pricing;

                analogueFromList.ProductPricings.Add(productPricing);
            }

            return product;
        };

        _connection.Query(
            analoguesExpression,
            analoguesTypes,
            analoguesMapper,
            new {
                ProductNetId = productNetId,
                ClientAgreementNetId = clientAgreementNetId,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                WithVat = withVat
            }
        );

        foreach (Product product in products) {
            decimal currentExchangeRateEurToUah = _connection.Query<decimal>(
                "SELECT dbo.GetCurrentEuroExchangeRateFiltered(@ProductNetId, @CurrencyId, @WithVat, NULL)",
                new { ProductNetId = product.NetUid, CurrencyID = uahCurrencyId, WithVat = withVat }).FirstOrDefault();

            product.CurrentPriceEurToUah = decimal.Round(product.CurrentPrice * currentExchangeRateEurToUah, 2, MidpointRounding.AwayFromZero);
            product.CurrentPriceReSaleEurToUah = decimal.Round(product.CurrentPriceReSale * currentExchangeRateEurToUah, 2, MidpointRounding.AwayFromZero);
        }

        return products;
    }

    public List<Product> GetAllComponentsByProductNetId(Guid productNetId, Guid clientAgreementNetId) {
        List<Product> products = new();

        bool withVat = _connection.Query<bool>(
            "SELECT [Agreement].[WithVATAccounting] FROM [ClientAgreement] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "WHERE [ClientAgreement].[NetUID] = @NetId; ",
            new { NetId = clientAgreementNetId }).FirstOrDefault();

        var props = new {
            ProductNetId = productNetId,
            ClientAgreementNetId = clientAgreementNetId,
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
            WithVat = withVat
        };

        string componentsExpression =
            "SELECT [Product].* " +
            ",[ProductSet].* " +
            ",[Component].ID " +
            ",[Component].Deleted " +
            ",[Component].HasAnalogue " +
            ",[Component].HasComponent " +
            ",[Component].HasImage " +
            ",[Component].Created " +
            ",[Component].Image " +
            ",[Component].IsForSale " +
            ",[Component].IsForWeb " +
            ",[Component].IsForZeroSale " +
            ",[Component].MainOriginalNumber " +
            ",[Component].MeasureUnitID " +
            ",[Component].NetUID ";

        componentsExpression += ProductSqlFragments.ComponentCultureColumns;

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
            "WHERE [Product].NetUID = @ProductNetId " +
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

            if (!products.Any(a => a.Id.Equals(component.Id))) {
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

                products.Add(component);
            } else {
                Product componentFromList = products.First(a => a.Id.Equals(component.Id));

                if (productOriginalNumber != null && !componentFromList.ProductOriginalNumbers.Any(n => n.Id.Equals(productOriginalNumber.Id))) {
                    productOriginalNumber.OriginalNumber = originalNumber;

                    componentFromList.ProductOriginalNumbers.Add(productOriginalNumber);
                }

                if (productProductGroup != null && !componentFromList.ProductProductGroups.Any(g => g.Id.Equals(productProductGroup.Id))) {
                    productProductGroup.ProductGroup = productGroup;

                    componentFromList.ProductProductGroups.Add(productProductGroup);
                }

                if (productCategory != null && !componentFromList.ProductCategories.Any(c => c.Id.Equals(productCategory.Id))) {
                    productCategory.Category = category;

                    componentFromList.ProductCategories.Add(productCategory);
                }

                if (productAvailability != null && !componentFromList.ProductAvailabilities.Any(a => a.Id.Equals(productAvailability.Id))) {
                    productAvailability.Storage = storage;

                    componentFromList.ProductAvailabilities.Add(productAvailability);
                }

                if (productPricing == null || componentFromList.ProductPricings.Any(p => p.Id.Equals(productPricing.Id))) return product;

                productPricing.Pricing = pricing;

                componentFromList.ProductPricings.Add(productPricing);
            }

            return product;
        };

        _connection.Query(
            componentsExpression,
            componentsTypes,
            componentsMapper,
            props
        );

        string componentAnaloguesExpression =
            "SELECT [Product].ID " +
            ",[ProductSet].ID " +
            ",[ProductSet].BaseProductID " +
            ",[ProductSet].ComponentProductID " +
            ",[ProductAnalogue].* " +
            ",[Analogue].ID " +
            ",[Analogue].Deleted " +
            ",[Analogue].HasAnalogue " +
            ",[Analogue].HasComponent " +
            ",[Analogue].HasImage " +
            ",[Analogue].Created " +
            ",[Analogue].Image " +
            ",[Analogue].IsForSale " +
            ",[Analogue].IsForWeb " +
            ",[Analogue].IsForZeroSale " +
            ",[Analogue].MainOriginalNumber " +
            ",[Analogue].MeasureUnitID " +
            ",[Analogue].NetUID ";

        componentAnaloguesExpression += ProductSqlFragments.AnalogueNameDescriptionColumns;

        componentAnaloguesExpression +=
            ",[Analogue].OrderStandard " +
            ",[Analogue].PackingStandard " +
            ",[Analogue].Standard " +
            ",[Analogue].[SynonymsUA] " +
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
            "WHERE [Product].NetUID = @ProductNetId " +
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

            Product fromList = products.First(p => p.Id.Equals(productSet.ComponentProductId));

            if (!fromList.AnalogueProducts.Any(a => a.Id.Equals(productAnalogue.Id))) {
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

                fromList.AnalogueProducts.Add(productAnalogue);
            } else {
                ProductAnalogue analogueFromList = fromList.AnalogueProducts.First(a => a.Id.Equals(productAnalogue.Id));

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
            props
        );

        return products;
    }

    public List<Product> GetAllWithDynamicPrices(string sql, string orderBy, GetQuery query, string value, Guid clientAgreementNetId) {
        bool withVat = _connection.Query<bool>(
            "SELECT [Agreement].[WithVATAccounting] FROM [ClientAgreement] " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
            "WHERE [ClientAgreement].[NetUID] = @NetId; ",
            new { NetId = clientAgreementNetId }).FirstOrDefault();

        var fullProps = new {
            Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
            Value = value,
            query.Offset,
            query.Limit,
            ClientAgreementNetId = clientAgreementNetId,
            WithVat = withVat
        };

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY [Product].VendorCode) AS RowNumber " +
            ", [Product].ID " +
            "FROM [Product] " +
            "WHERE [Product].Deleted = 0 " +
            "AND ( " +
            "[Product].MainOriginalNumber like '%' + @Value + '%' " +
            "OR [Product].VendorCode like '%' + @Value + '%' ";

        sqlExpression += "OR [Product].NameUA like '%' + @Value + '%' ";
        sqlExpression += "OR [Product].DescriptionUA like '%' + @Value + '%' ";

        sqlExpression +=
            ") " +
            "SELECT [Product].ID " +
            ",[Product].Deleted " +
            ",[Product].HasAnalogue " +
            ",[Product].HasComponent " +
            ",[Product].HasImage " +
            ",[Product].Created " +
            ",[Product].Image " +
            ",[Product].IsForSale " +
            ",[Product].IsForWeb " +
            ",[Product].IsForZeroSale " +
            ",[Product].MainOriginalNumber " +
            ",[Product].MeasureUnitID ";

        sqlExpression += ProductSqlFragments.ProductCultureColumns;

        sqlExpression +=
            ",[Product].NetUID " +
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
            ",dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentPrice " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, NULL) AS CurrentLocalPrice " +
            ",[MeasureUnit].* " +
            "FROM [Product] " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "WHERE [Product].ID IN (" +
            "SELECT ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            ")";

        List<Product> products = _connection.Query<Product, MeasureUnit, Product>(
            sqlExpression,
            (product, measureUnit) => {
                product.MeasureUnit = measureUnit;

                product.CurrentLocalPrice = decimal.Round(product.CurrentLocalPrice, 2, MidpointRounding.AwayFromZero);

                return product;
            },
            fullProps
        ).ToList();

        if (!products.Any()) return products;

        var joinProps = new { Ids = products.Select(p => p.Id), ClientAgreementNetId = clientAgreementNetId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

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
            "WHERE [Product].ID IN @Ids " +
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
            typeof(Pricing)
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

            Product fromList = products.First(p => p.Id.Equals(product.Id));

            if (productOriginalNumber != null && !fromList.ProductOriginalNumbers.Any(n => n.Id.Equals(productOriginalNumber.Id))) {
                productOriginalNumber.OriginalNumber = originalNumber;

                fromList.ProductOriginalNumbers.Add(productOriginalNumber);
            }

            if (productProductGroup != null && !fromList.ProductProductGroups.Any(g => g.Id.Equals(productProductGroup.Id))) {
                productProductGroup.ProductGroup = productGroup;

                fromList.ProductProductGroups.Add(productProductGroup);
            }

            if (productCategory != null && !fromList.ProductCategories.Any(c => c.Id.Equals(productCategory.Id))) {
                productCategory.Category = category;

                fromList.ProductCategories.Add(productCategory);
            }

            if (productAvailability != null && !fromList.ProductAvailabilities.Any(a => a.Id.Equals(productAvailability.Id))) {
                productAvailability.Storage = storage;

                fromList.ProductAvailabilities.Add(productAvailability);
            }

            if (productPricing != null && !fromList.ProductPricings.Any(p => p.Id.Equals(productPricing.Id))) {
                productPricing.Pricing = pricing;

                fromList.ProductPricings.Add(productPricing);
            }

            return product;
        };

        _connection.Query(
            productsExpression,
            productsTypes,
            productsMapper,
            joinProps
        );

        return products;
    }

    public List<Product> GetAllByIds(IEnumerable<long> ids, long organizationId) {
        List<Product> products = new();

        string sqlExpression =
            "SELECT Product.ID " +
            ",[Product].Deleted " +
            ",[Product].HasAnalogue " +
            ",[Product].HasComponent " +
            ",[Product].HasImage " +
            ",[Product].Created " +
            ",[Product].Image " +
            ",[Product].IsForSale " +
            ",[Product].IsForWeb " +
            ",[Product].IsForZeroSale " +
            ",[Product].MainOriginalNumber " +
            ",[Product].MeasureUnitID ";

        sqlExpression += ProductSqlFragments.ProductCultureColumns;

        sqlExpression +=
            ",[Product].NetUID " +
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
            ",ProductOriginalNumber.* " +
            ",OriginalNumber.* " +
            ",ProductProductGroup.* " +
            ",ProductGroup.* " +
            ",MeasureUnit.* " +
            ",ProductCategory.* " +
            ",Category.* " +
            ",ProductAvailability.* " +
            ",ProductAvailabilityStorage.* " +
            ",ProductAnalogue.* " +
            ",analogueProduct.ID " +
            ",[analogueProduct].Deleted " +
            ",[analogueProduct].HasAnalogue " +
            ",[analogueProduct].HasComponent " +
            ",[analogueProduct].HasImage " +
            ",[analogueProduct].Created " +
            ",[analogueProduct].Image " +
            ",[analogueProduct].IsForSale " +
            ",[analogueProduct].IsForWeb " +
            ",[analogueProduct].IsForZeroSale " +
            ",[analogueProduct].MainOriginalNumber " +
            ",[analogueProduct].MeasureUnitID ";

        sqlExpression += ProductSqlFragments.AnalogueProductAliasColumns;

        sqlExpression +=
            ",[analogueProduct].NetUID " +
            ",[analogueProduct].OrderStandard " +
            ",[analogueProduct].PackingStandard " +
            ",[analogueProduct].Size " +
            ",[analogueProduct].[Top] " +
            ",[analogueProduct].UCGFEA " +
            ",[analogueProduct].Updated " +
            ",[analogueProduct].VendorCode " +
            ",[analogueProduct].Volume " +
            ",[analogueProduct].Weight " +
            ",ProductPricing.* " +
            ",Pricing.* " +
            ",Currency.* " +
            ",ProductSet.* " +
            ",ProductComponent.ID " +
            ",[ProductComponent].Deleted " +
            ",[ProductComponent].HasAnalogue " +
            ",[ProductComponent].HasComponent " +
            ",[ProductComponent].HasImage " +
            ",[ProductComponent].Created " +
            ",[ProductComponent].Image " +
            ",[ProductComponent].IsForSale " +
            ",[ProductComponent].IsForWeb " +
            ",[ProductComponent].IsForZeroSale " +
            ",[ProductComponent].MainOriginalNumber " +
            ",[ProductComponent].MeasureUnitID ";

        sqlExpression += ProductSqlFragments.ProductComponentAliasColumns;

        sqlExpression +=
            ",[ProductComponent].NetUID " +
            ",[ProductComponent].OrderStandard " +
            ",[ProductComponent].PackingStandard " +
            ",[ProductComponent].Size " +
            ",[ProductComponent].[Top] " +
            ",[ProductComponent].UCGFEA " +
            ",[ProductComponent].Updated " +
            ",[ProductComponent].VendorCode " +
            ",[ProductComponent].Volume " +
            ",[ProductComponent].Weight " +
            ",ComponentProductPricing.* " +
            "FROM Product " +
            "LEFT OUTER JOIN ProductOriginalNumber " +
            "ON Product.ID = ProductOriginalNumber.ProductID AND ProductOriginalNumber.Deleted = 0 " +
            "LEFT OUTER JOIN OriginalNumber " +
            "ON ProductOriginalNumber.OriginalNumberID = OriginalNumber.ID AND OriginalNumber.Deleted = 0 " +
            "LEFT OUTER JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID AND ProductProductGroup.Deleted = 0 " +
            "LEFT OUTER JOIN ProductGroup " +
            "ON ProductProductGroup.ProductGroupID = ProductGroup.ID AND ProductGroup.Deleted = 0 " +
            "LEFT OUTER JOIN MeasureUnit " +
            "ON Product.MeasureUnitId = MeasureUnit.ID AND MeasureUnit.Deleted = 0 " +
            "LEFT OUTER JOIN ProductCategory " +
            "ON ProductCategory.ProductID = Product.ID AND ProductCategory.Deleted = 0 " +
            "LEFT OUTER JOIN Category " +
            "ON ProductCategory.CategoryID = Category.ID AND Category.Deleted = 0 " +
            "LEFT JOIN ProductAvailability " +
            "ON ProductAvailability.ProductID = Product.ID " +
            "LEFT OUTER JOIN Storage AS ProductAvailabilityStorage " +
            "ON ProductAvailabilityStorage.Id = ProductAvailability.StorageId " +
            "AND ProductAvailabilityStorage.Deleted = 0" +
            "AND ProductAvailabilityStorage.OrganizationId = @OrganizationId " +
            "LEFT OUTER JOIN ProductAnalogue " +
            "ON ProductAnalogue.BaseProductID = Product.ID " +
            "LEFT JOIN Product AS analogueProduct " +
            "ON analogueProduct.ID = ProductAnalogue.AnalogueProductID " +
            "LEFT OUTER JOIN ProductPricing " +
            "ON ProductPricing.ProductId = Product.Id " +
            "LEFT OUTER JOIN Pricing " +
            "ON ProductPricing.PricingId = Pricing.Id AND Pricing.Deleted = 0 " +
            "LEFT OUTER JOIN Currency " +
            "ON Pricing.CurrencyId = Currency.Id AND Currency.Deleted = 0 " +
            "LEFT OUTER JOIN ProductSet " +
            "ON ProductSet.BaseProductId = Product.Id AND ProductSet.Deleted = 0 " +
            "LEFT OUTER JOIN Product AS ProductComponent " +
            "ON ProductComponent.Id = ProductSet.ComponentProductId " +
            "LEFT OUTER JOIN ProductPricing AS ComponentProductPricing " +
            "ON ProductComponent.Id = ComponentProductPricing.ProductId " +
            "WHERE Product.ID IN @Ids ";

        Type[] types = {
            typeof(Product),
            typeof(ProductOriginalNumber),
            typeof(OriginalNumber),
            typeof(ProductProductGroup),
            typeof(ProductGroup),
            typeof(MeasureUnit),
            typeof(ProductCategory),
            typeof(Category),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(ProductAnalogue),
            typeof(Product),
            typeof(ProductPricing),
            typeof(Pricing),
            typeof(Currency),
            typeof(ProductSet),
            typeof(Product),
            typeof(ProductPricing)
        };

        Func<object[], Product> mapper = objects => {
            Product product = (Product)objects[0];
            ProductOriginalNumber productOriginalNumber = (ProductOriginalNumber)objects[1];
            OriginalNumber originalNumber = (OriginalNumber)objects[2];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[3];
            ProductGroup productGroup = (ProductGroup)objects[4];
            MeasureUnit measureUnit = (MeasureUnit)objects[5];
            ProductCategory productCategory = (ProductCategory)objects[6];
            Category category = (Category)objects[7];
            ProductAvailability productAvailability = (ProductAvailability)objects[8];
            Storage productAvailabilityStorage = (Storage)objects[9];
            ProductAnalogue productAnalogue = (ProductAnalogue)objects[10];
            Product analogueProduct = (Product)objects[11];
            ProductPricing productPricing = (ProductPricing)objects[12];
            Pricing pricing = (Pricing)objects[13];
            Currency currency = (Currency)objects[14];
            ProductSet productSet = (ProductSet)objects[15];
            Product componentProduct = (Product)objects[16];
            ProductPricing componentProductPricing = (ProductPricing)objects[17];

            if (productAvailability != null && productAvailabilityStorage != null) {
                productAvailability.Storage = productAvailabilityStorage;
                product.ProductAvailabilities.Add(productAvailability);
            }

            if (measureUnit != null) product.MeasureUnit = measureUnit;

            if (productProductGroup != null && productGroup != null) {
                productProductGroup.ProductGroup = productGroup;
                product.ProductProductGroups.Add(productProductGroup);
            }

            if (productOriginalNumber != null && originalNumber != null) {
                productOriginalNumber.OriginalNumber = originalNumber;
                product.ProductOriginalNumbers.Add(productOriginalNumber);
            }

            if (productCategory != null && category != null) {
                productCategory.Category = category;
                product.ProductCategories.Add(productCategory);
            }

            if (productSet != null && componentProduct != null) {
                if (componentProductPricing != null) componentProduct.ProductPricings.Add(componentProductPricing);

                productSet.ComponentProduct = componentProduct;
                product.ComponentProducts.Add(productSet);
            }

            if (productAnalogue != null && analogueProduct != null) {
                productAnalogue.AnalogueProduct = analogueProduct;
                product.AnalogueProducts.Add(productAnalogue);
            }

            if (productPricing != null) {
                if (pricing != null) {
                    pricing.Currency = currency;
                    productPricing.Pricing = pricing;
                }

                product.ProductPricings.Add(productPricing);
            }

            if (products.Any(p => p.Id.Equals(product.Id))) {
                Product productInList = products.First(p => p.Id.Equals(product.Id));

                if (productOriginalNumber != null && !productInList.ProductOriginalNumbers.Any(o => o.Id.Equals(productOriginalNumber.Id)))
                    productInList.ProductOriginalNumbers.Add(productOriginalNumber);

                if (productProductGroup != null && !productInList.ProductProductGroups.Any(o => o.Id.Equals(productProductGroup.Id)))
                    productInList.ProductProductGroups.Add(productProductGroup);

                if (productCategory != null && !productInList.ProductCategories.Any(c => c.Id.Equals(productCategory.Id))) productInList.ProductCategories.Add(productCategory);

                if (productSet != null && !productInList.ComponentProducts.Any(a => a.Id.Equals(productSet.Id))) productInList.ComponentProducts.Add(productSet);

                if (productAvailability != null && productAvailabilityStorage != null && !productInList.ProductAvailabilities.Any(p => p.Id.Equals(productAvailability.Id))) {
                    productAvailability.Storage = productAvailabilityStorage;

                    productInList.ProductAvailabilities.Add(productAvailability);
                }

                if (productAnalogue != null && !productInList.AnalogueProducts.Any(a => a.Id.Equals(productAnalogue.Id))) productInList.AnalogueProducts.Add(productAnalogue);

                if (productPricing != null && !productInList.ProductPricings.Any(a => a.Id.Equals(productPricing.Id))) productInList.ProductPricings.Add(productPricing);
            } else {
                products.Add(product);
            }

            return product;
        };

        var fullProps = new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, Ids = ids, OrganizationId = organizationId };

        _connection.Query(sqlExpression, types, mapper, fullProps);

        return products;
    }

    public List<Product> GetAllByGroupNetId(Guid netId, long limit, long offset) {
        List<Product> productsToReturn = new();

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY [Product].ID DESC) AS RowNumber " +
            ", [Product].ID " +
            "FROM [Product] " +
            "LEFT JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID AND ProductProductGroup.Deleted = 0 " +
            "LEFT JOIN ProductGroup " +
            "ON ProductGroup.ID = ProductProductGroup.ProductGroupID " +
            "WHERE [ProductGroup].NetUID = @NetId " +
            ") " +
            "SELECT * " +
            "FROM Product " +
            "LEFT JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID AND ProductProductGroup.Deleted = 0 " +
            "LEFT JOIN ProductGroup " +
            "ON ProductGroup.ID = ProductProductGroup.ProductGroupID " +
            "WHERE Product.ID IN (" +
            "SELECT ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            ") ";

        Type[] types = {
            typeof(Product),
            typeof(ProductProductGroup),
            typeof(ProductGroup)
        };

        Func<object[], Product> mapper = objects => {
            Product product = (Product)objects[0];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[1];
            ProductGroup productGroup = (ProductGroup)objects[2];

            if (productsToReturn.Any(p => p.Id.Equals(product.Id))) {
                Product productInList = productsToReturn.First(p => p.Id.Equals(product.Id));

                if (productProductGroup == null || productInList.ProductProductGroups.Any(g => g.Id.Equals(productProductGroup.Id))) return product;

                productProductGroup.ProductGroup = productGroup;
                productInList.ProductProductGroups.Add(productProductGroup);
            } else {
                if (productProductGroup != null) {
                    productProductGroup.ProductGroup = productGroup;
                    product.ProductProductGroups.Add(productProductGroup);
                }

                productsToReturn.Add(product);
            }

            return product;
        };

        var fullProps = new { NetId = netId, Offset = offset, Limit = limit, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(sqlExpression, types, mapper, fullProps);

        return productsToReturn;
    }

    public List<Product> GetAllByActiveProductSpecificationCode(string code) {
        return _connection.Query<Product, ProductSpecification, Product>(
            "SELECT * " +
            "FROM [Product] " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].ProductID = [Product].ID " +
            "AND [ProductSpecification].IsActive = 1 " +
            "WHERE [Product].Deleted = 0 " +
            "AND [ProductSpecification].SpecificationCode = @Code",
            (product, specification) => {
                if (specification != null) product.ProductSpecifications.Add(specification);

                return product;
            },
            new { Code = code }
        ).ToList();
    }

    public List<Product> SearchForProductsByVendorCode(string value, long limit, long offset) {
        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS ( " +
            "SELECT [Product].ID " +
            ", [Product].[Name] " +
            ", [Product].VendorCode " +
            "FROM [Product] " +
            "WHERE [Product].Deleted = 0 " +
            "AND PATINDEX('%' + @Value + '%', [Product].SearchVendorCode) > 0 " +
            "), " +
            "[Rowed_CTE] " +
            "AS ( " +
            "SELECT [Product].ID " +
            ", ROW_NUMBER() OVER(ORDER BY [Product].VendorCode, [Product].[Name]) AS [RowNumber] " +
            "FROM [Search_CTE] AS [Product] " +
            ") " +
            "SELECT [Product].* ";

        sqlExpression += ProductSqlFragments.ProductCultureColumns;

        sqlExpression +=
            "" +
            "FROM [Product] " +
            "WHERE [Product].ID IN ( " +
            "SELECT [Rowed_CTE].ID " +
            "FROM [Rowed_CTE] " +
            "WHERE [Rowed_CTE].RowNumber > @Offset " +
            "AND [Rowed_CTE].RowNumber <= @Limit + @Offset " +
            ") " +
            "ORDER BY [Product].VendorCode, [Product].[Name]";

        return _connection.Query<Product>(
                sqlExpression,
                new { Value = value, Limit = limit, Offset = offset }
            )
            .ToList();
    }

    public List<ProductHistoryModel> GetAllOrderedProductsHistory(Guid clientNetId) {
        return _connection.Query<ProductHistoryModel>(
                "SELECT " +
                "[Product].ID AS ProductId " +
                ", [Product].NetUID AS ProductNetId " +
                ", [Product].[Name] " +
                ", [Product].VendorCode " +
                ", SUM(OrderItem.Qty - ISNULL(SaleReturnItem.ReturnedQty, 0)) AS Qty " +
                ", MAX([Sale].Updated) AS LastOrderedDate " +
                "FROM [Sale] " +
                "LEFT JOIN [BaseLifeCycleStatus] " +
                "ON [BaseLifeCycleStatus].ID = [Sale].BaseLifeCycleStatusID " +
                "LEFT JOIN [ClientAgreement] " +
                "ON [ClientAgreement].ID = [Sale].ClientAgreementID " +
                "AND [ClientAgreement].Deleted = 0 " +
                "LEFT JOIN [Agreement] " +
                "ON [Agreement].ID = [ClientAgreement].AgreementID " +
                "LEFT JOIN [Client] " +
                "ON [Client].ID = [ClientAgreement].ClientID " +
                "LEFT JOIN [Order] " +
                "ON [Order].ID = [Sale].OrderID " +
                "LEFT JOIN [OrderItem] " +
                "ON [OrderItem].OrderID = [Order].ID " +
                "AND [OrderItem].Deleted = 0 " +
                "LEFT JOIN ( " +
                "SELECT SUM(Qty) AS ReturnedQty, OrderItemID FROM [SaleReturnItem] " +
                "WHERE Deleted = 0 " +
                "GROUP BY OrderItemID " +
                ") [SaleReturnItem] " +
                "ON [SaleReturnItem].OrderItemID = [OrderItem].ID " +
                "LEFT JOIN [Product] " +
                "ON [Product].ID = [OrderItem].ProductID " +
                "WHERE [Client].NetUID = @NetId " +
                "AND [Agreement].ForReSale = 0 " +
                "AND [BaseLifeCycleStatus].SaleLifeCycleType <> 0 " +
                "GROUP BY [Product].ID, [Product].NetUID, [Product].[Name], [Product].VendorCode " +
                "HAVING SUM([OrderItem].Qty - ISNULL([SaleReturnItem].ReturnedQty, 0)) > 0 " +
                "ORDER BY Qty DESC ",
                new { NetId = clientNetId })
            .ToList();
    }

    public IEnumerable<Product> GetByOldECommerceIdsFromSearch(
        IEnumerable<long> oldECommerceIds,
        long limit,
        long offset
    ) {
        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS ( " +
            "SELECT [Product].ID " +
            ", [Product].[Name] " +
            ", [Product].VendorCode " +
            "FROM [Product] " +
            "WHERE [Product].Deleted = 0 " +
            "AND ([Product].SourceAmgCode IN @OldEcommerceIds " +
            "OR [Product].SourceFenixCode IN @OldEcommerceIds) " +
            "), " +
            "[Rowed_CTE] " +
            "AS ( " +
            "SELECT [Product].ID " +
            ", ROW_NUMBER() OVER(ORDER BY [Product].VendorCode, [Product].[Name]) AS [RowNumber] " +
            "FROM [Search_CTE] AS [Product] " +
            ") " +
            "SELECT [Product].* ";

        sqlExpression += ProductSqlFragments.ProductCultureColumns;

        sqlExpression +=
            "" +
            "FROM [Product] " +
            "WHERE [Product].ID IN ( " +
            "SELECT [Rowed_CTE].ID " +
            "FROM [Rowed_CTE] " +
            "WHERE [Rowed_CTE].RowNumber > @Offset " +
            "AND [Rowed_CTE].RowNumber <= @Limit + @Offset " +
            ") " +
            "ORDER BY [Product].VendorCode, [Product].[Name]";

        return _connection.Query<Product>(
            sqlExpression,
            new { OldEcommerceIds = oldECommerceIds, Limit = limit, Offset = offset }
        );
    }

    public IEnumerable<Product> GetAllByVendorCodeWithActiveProductSpecification(string vendorCode) {
        return _connection.Query<Product, ProductSpecification, Product>(
            "SELECT * " +
            "FROM [Product] " +
            "LEFT JOIN [ProductSpecification] " +
            "ON [ProductSpecification].ProductID = [Product].ID " +
            "AND [ProductSpecification].IsActive = 1 " +
            "WHERE [Product].Deleted = 0 " +
            "AND [Product].VendorCode = @VendorCode",
            (product, specification) => {
                if (specification != null) product.ProductSpecifications.Add(specification);

                return product;
            },
            new { VendorCode = vendorCode }
        ).ToList();
    }

    public List<dynamic> GetTopTotalPurchasedByOnlineShop() {
        return _connection.Query<dynamic>(
            "SELECT TOP(10) Product.VendorCode, SUM(OrderItem.Qty) AS TotalPurchased " +
            "FROM Product " +
            "LEFT OUTER JOIN OrderItem " +
            "ON OrderItem.ProductID = Product.ID AND OrderItem.Deleted = 0 " +
            "LEFT OUTER JOIN [Order] " +
            "ON [Order].ID = OrderItem.OrderID AND [Order].Deleted = 0 " +
            "WHERE [Order].OrderSource = 0 " +
            "GROUP BY Product.VendorCode " +
            "ORDER BY TotalPurchased DESC, Product.VendorCode"
        ).ToList();
    }

    public IEnumerable<ProductAvailability> GetAvailabilitiesByProductNetId(Guid productNetId) {
        return _connection.Query<ProductAvailability, Storage, Product, ProductAvailability>(
            "SELECT * " +
            "FROM [ProductAvailability] " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ProductAvailability].ProductID " +
            "WHERE [ProductAvailability].Deleted = 0 " +
            "AND [Product].NetUID = @ProductNetId " +
            "ORDER BY CASE WHEN [Storage].Locale = @Culture THEN 0 ELSE 1 END, [ProductAvailability].Amount DESC",
            (productAvailability, storage, product) => {
                productAvailability.Storage = storage;

                return productAvailability;
            },
            new { ProductNetId = productNetId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );
    }

    public List<SearchResult> GetAllProductIdsFromSql(string sql, dynamic props) {
        return _connection.Query<SearchResult>(
            sql,
            (object)props
        ).ToList();
    }

    public List<Product> GetAllLimited(int limit, int offset) {
        List<Product> products = new();

        var props = new { Offset = offset, Limit = limit, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY [Product].ID DESC) AS RowNumber " +
            ", [Product].ID " +
            ", [Product].Created " +
            "FROM [Product] " +
            "WHERE [Product].Deleted = 0 " +
            ") " +
            "SELECT ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            "ORDER BY RowNumber ";

        IEnumerable<long> productIds = _connection.Query<long>(
            sqlExpression,
            props
        );

        GetProductsByIds(products, productIds);

        return products;
    }

    public List<Product> GetAllByUpdatedDates(DateTime fromDate, DateTime toDate, int limit, int offset) {
        List<Product> products = new();

        var props = new { FromDate = fromDate, ToDate = toDate, Offset = offset, Limit = limit, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY [Product].ID DESC) AS RowNumber " +
            ", [Product].ID " +
            ", [Product].Created " +
            "FROM [Product] " +
            "WHERE [Product].Deleted = 0 " +
            ") " +
            "SELECT ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].Created >= @FromDate " +
            "AND [Search_CTE].Created <= @ToDate " +
            "AND [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            "ORDER BY RowNumber ";

        IEnumerable<long> productIds = _connection.Query<long>(
            sqlExpression,
            props
        );

        GetProductsByIds(products, productIds);

        return products;
    }

    public long GetTotalQty() {
        return _connection.Query<long>("SELECT COUNT(ID) FROM Product").FirstOrDefault();
    }

    private void GetProductsByIds(List<Product> products, IEnumerable<long> productIds) {
        string fullSqlExpression =
            ";WITH LatestProductSpecifications AS (  " +
            "SELECT ps.* " +
            "    FROM ( " +
            "        SELECT ProductID, MAX(Created) AS MaxCreated " +
            "FROM ProductSpecification " +
            "GROUP BY ProductID " +
            ") AS latest " +
            "INNER JOIN ProductSpecification ps " +
            "ON ps.ProductID = latest.ProductID AND ps.Created = latest.MaxCreated " +
            ") " +
            "SELECT * FROM Product " +
            "LEFT OUTER JOIN ProductOriginalNumber " +
            "ON Product.ID = ProductOriginalNumber.ProductID AND ProductOriginalNumber.Deleted = 0 " +
            "LEFT OUTER JOIN OriginalNumber " +
            "ON ProductOriginalNumber.OriginalNumberID = OriginalNumber.ID AND OriginalNumber.Deleted = 0 " +
            "LEFT OUTER JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID AND ProductProductGroup.Deleted = 0 " +
            "LEFT OUTER JOIN ProductGroup " +
            "ON ProductProductGroup.ProductGroupID = ProductGroup.ID AND ProductGroup.Deleted = 0 " +
            "LEFT OUTER JOIN MeasureUnit " +
            "ON Product.MeasureUnitId = MeasureUnit.Id AND MeasureUnit.Deleted = 0 " +
            "LEFT OUTER JOIN ProductCategory " +
            "ON ProductCategory.ProductID = Product.Id AND ProductCategory.Deleted = 0 " +
            "LEFT OUTER JOIN Category " +
            "ON ProductCategory.CategoryID = Category.ID AND Category.Deleted = 0 " +
            "LEFT OUTER JOIN ProductAnalogue " + // TODO Remove ProductAnalogues and Components
            "ON ProductAnalogue.BaseProductID = Product.ID " +
            "LEFT JOIN Product AnalogueProduct " +
            "ON AnalogueProduct.ID = ProductAnalogue.AnalogueProductID " +
            "LEFT OUTER JOIN ProductPricing " +
            "ON ProductPricing.ProductId = Product.Id " +
            "LEFT OUTER JOIN Pricing " +
            "ON ProductPricing.PricingId = Pricing.Id AND Pricing.Deleted = 0 " +
            "LEFT OUTER JOIN Currency " +
            "ON Pricing.CurrencyId = Currency.Id AND Currency.Deleted = 0 " +
            "LEFT OUTER JOIN ProductSet " +
            "ON ProductSet.BaseProductId = Product.Id AND ProductSet.Deleted = 0 " +
            "LEFT OUTER JOIN Product AS ProductComponent " +
            "ON ProductComponent.Id = ProductSet.ComponentProductId " +
            "LEFT OUTER JOIN ProductPricing AS ComponentProductPricing " +
            "ON ProductComponent.Id = ComponentProductPricing.ProductId " +
            "LEFT OUTER JOIN ProductAvailability " +
            "ON ProductAvailability.ProductId = Product.Id AND ProductAvailability.Deleted = 0 " +
            "LEFT OUTER JOIN Storage AS ProductAvailabilityStorage " +
            "ON ProductAvailabilityStorage.Id = ProductAvailability.StorageId AND ProductAvailabilityStorage.Deleted = 0 " +
            "LEFT OUTER JOIN LatestProductSpecifications AS ProductSpecification " +
            "ON ProductSpecification.ProductID = Product.ID " +
            "WHERE Product.Id IN @Ids";

        Type[] types = {
            typeof(Product),
            typeof(ProductOriginalNumber),
            typeof(OriginalNumber),
            typeof(ProductProductGroup),
            typeof(ProductGroup),
            typeof(MeasureUnit),
            typeof(ProductCategory),
            typeof(Category),
            typeof(ProductAnalogue),
            typeof(Product),
            typeof(ProductPricing),
            typeof(Pricing),
            typeof(Currency),
            typeof(ProductSet),
            typeof(Product),
            typeof(ProductPricing),
            typeof(ProductAvailability),
            typeof(Storage),
            typeof(ProductSpecification)
        };

        Func<object[], Product> mapper = objects => {
            Product product = (Product)objects[0];
            ProductOriginalNumber productOriginalNumber = (ProductOriginalNumber)objects[1];
            OriginalNumber originalNumber = (OriginalNumber)objects[2];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[3];
            ProductGroup productGroup = (ProductGroup)objects[4];
            MeasureUnit measureUnit = (MeasureUnit)objects[5];
            ProductCategory productCategory = (ProductCategory)objects[6];
            Category category = (Category)objects[7];
            ProductAnalogue productAnalogue = (ProductAnalogue)objects[8];
            Product analogueProduct = (Product)objects[9];
            ProductPricing productPricing = (ProductPricing)objects[10];
            Pricing pricing = (Pricing)objects[11];
            Currency currency = (Currency)objects[12];
            ProductSet productSet = (ProductSet)objects[13];
            Product componentProduct = (Product)objects[14];
            ProductPricing componentProductPricing = (ProductPricing)objects[15];
            ProductAvailability productAvailability = (ProductAvailability)objects[16];
            Storage productAvailabilityStorage = (Storage)objects[17];
            ProductSpecification productSpecification = (ProductSpecification)objects[18];

            if (productAvailability != null && productAvailabilityStorage != null) {
                productAvailability.Storage = productAvailabilityStorage;
                product.ProductAvailabilities.Add(productAvailability);
            }

            if (productSpecification != null) product.ProductSpecifications.Add(productSpecification);

            if (measureUnit != null) product.MeasureUnit = measureUnit;

            if (productProductGroup != null && productGroup != null) {
                productProductGroup.ProductGroup = productGroup;
                product.ProductProductGroups.Add(productProductGroup);
            }

            if (productOriginalNumber != null && originalNumber != null) {
                productOriginalNumber.OriginalNumber = originalNumber;
                product.ProductOriginalNumbers.Add(productOriginalNumber);
            }

            if (productCategory != null && category != null) {
                productCategory.Category = category;
                product.ProductCategories.Add(productCategory);
            }

            if (productSet != null && componentProduct != null) {
                if (componentProductPricing != null) componentProduct.ProductPricings.Add(componentProductPricing);

                productSet.ComponentProduct = componentProduct;
                product.ComponentProducts.Add(productSet);
            }

            if (productAnalogue != null && analogueProduct != null) {
                productAnalogue.AnalogueProduct = analogueProduct;
                product.AnalogueProducts.Add(productAnalogue);
            }

            if (productPricing != null) {
                if (pricing != null) {
                    pricing.Currency = currency;
                    productPricing.Pricing = pricing;
                }

                product.ProductPricings.Add(productPricing);
            }

            if (products.Any(p => p.Id.Equals(product.Id))) {
                Product productInList = products.First(p => p.Id.Equals(product.Id));

                if (productOriginalNumber != null && !productInList.ProductOriginalNumbers.Any(o => o.Id.Equals(productOriginalNumber.Id)))
                    productInList.ProductOriginalNumbers.Add(productOriginalNumber);

                if (productProductGroup != null && !productInList.ProductProductGroups.Any(o => o.Id.Equals(productProductGroup.Id)))
                    productInList.ProductProductGroups.Add(productProductGroup);

                if (productCategory != null && !productInList.ProductCategories.Any(c => c.Id.Equals(productCategory.Id))) productInList.ProductCategories.Add(productCategory);

                if (productSet != null && !productInList.ComponentProducts.Any(a => a.Id.Equals(productSet.Id))) productInList.ComponentProducts.Add(productSet);

                if (productAnalogue != null && !productInList.AnalogueProducts.Any(a => a.Id.Equals(productAnalogue.Id))) productInList.AnalogueProducts.Add(productAnalogue);

                if (productPricing != null && !productInList.ProductPricings.Any(a => a.Id.Equals(productPricing.Id))) productInList.ProductPricings.Add(productPricing);
            } else {
                products.Add(product);
            }

            return product;
        };

        var fullProps = new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, Ids = productIds };

        _connection.Query(fullSqlExpression, types, mapper, fullProps);
    }
}