using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Products;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductCapitalizationRepository : IProductCapitalizationRepository {
    private readonly IDbConnection _connection;

    public ProductCapitalizationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ProductCapitalization productCapitalization) {
        return _connection.Query<long>(
                "INSERT INTO [ProductCapitalization] (Number, Comment, FromDate, OrganizationId, ResponsibleId, StorageId, Updated) " +
                "VALUES (@Number, @Comment, @FromDate, @OrganizationId, @ResponsibleId, @StorageId, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                productCapitalization
            )
            .Single();
    }

    public ProductCapitalization GetLastRecord(string prefix) {
        string sqlExpression =
            "SELECT TOP(1) * " +
            "FROM [ProductCapitalization] ";

        if (string.IsNullOrEmpty(prefix))
            sqlExpression += "WHERE [ProductCapitalization].[Number] LIKE N'0%' ";
        else
            sqlExpression += $"WHERE [ProductCapitalization].[Number] LIKE N'{prefix}%' ";

        sqlExpression +=
            "ORDER BY [ProductCapitalization].ID DESC";

        return _connection.Query<ProductCapitalization>(
                sqlExpression
            )
            .SingleOrDefault();
    }

    public ProductCapitalization GetById(long id) {
        ProductCapitalization toReturn = null;

        Type[] types = {
            typeof(ProductCapitalization),
            typeof(User),
            typeof(Organization),
            typeof(ProductCapitalizationItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(Storage)
        };

        Func<object[], ProductCapitalization> mapper = objects => {
            ProductCapitalization productCapitalization = (ProductCapitalization)objects[0];
            User responsible = (User)objects[1];
            Organization organization = (Organization)objects[2];
            ProductCapitalizationItem productCapitalizationItem = (ProductCapitalizationItem)objects[3];
            Product product = (Product)objects[4];
            MeasureUnit measureUnit = (MeasureUnit)objects[5];
            Storage storage = (Storage)objects[6];

            if (toReturn != null) {
                product.MeasureUnit = measureUnit;

                productCapitalizationItem.Product = product;

                toReturn.ProductCapitalizationItems.Add(productCapitalizationItem);
            } else {
                if (productCapitalizationItem != null) {
                    product.MeasureUnit = measureUnit;

                    productCapitalizationItem.Product = product;

                    productCapitalization.ProductCapitalizationItems.Add(productCapitalizationItem);
                }

                productCapitalization.Storage = storage;
                productCapitalization.Responsible = responsible;
                productCapitalization.Organization = organization;

                toReturn = productCapitalization;
            }

            return productCapitalization;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [ProductCapitalization] " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [ProductCapitalization].ResponsibleID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [ProductCapitalization].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [ProductCapitalizationItem] " +
            "ON [ProductCapitalizationItem].ProductCapitalizationID = [ProductCapitalization].ID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ProductCapitalizationItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductCapitalization].StorageID " +
            "WHERE [ProductCapitalization].ID = @Id",
            types,
            mapper,
            new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }

    public ProductCapitalization GetByNetId(Guid netId) {
        ProductCapitalization toReturn = null;

        Type[] types = {
            typeof(ProductCapitalization),
            typeof(User),
            typeof(Organization),
            typeof(ProductCapitalizationItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(Storage)
        };

        Func<object[], ProductCapitalization> mapper = objects => {
            ProductCapitalization productCapitalization = (ProductCapitalization)objects[0];
            User responsible = (User)objects[1];
            Organization organization = (Organization)objects[2];
            ProductCapitalizationItem productCapitalizationItem = (ProductCapitalizationItem)objects[3];
            Product product = (Product)objects[4];
            MeasureUnit measureUnit = (MeasureUnit)objects[5];
            Storage storage = (Storage)objects[6];

            if (toReturn != null) {
                product.MeasureUnit = measureUnit;

                productCapitalizationItem.Product = product;

                productCapitalizationItem.TotalAmount =
                    decimal.Round(Convert.ToDecimal(productCapitalizationItem.Qty) * productCapitalizationItem.UnitPrice, 2, MidpointRounding.AwayFromZero);

                toReturn.ProductCapitalizationItems.Add(productCapitalizationItem);

                toReturn.TotalAmount =
                    decimal.Round(
                        toReturn.TotalAmount + productCapitalizationItem.TotalAmount,
                        2,
                        MidpointRounding.AwayFromZero
                    );
            } else {
                if (productCapitalizationItem != null) {
                    product.MeasureUnit = measureUnit;

                    productCapitalizationItem.Product = product;

                    productCapitalizationItem.TotalAmount =
                        decimal.Round(Convert.ToDecimal(productCapitalizationItem.Qty) * productCapitalizationItem.UnitPrice, 2, MidpointRounding.AwayFromZero);

                    productCapitalization.ProductCapitalizationItems.Add(productCapitalizationItem);

                    productCapitalization.TotalAmount =
                        decimal.Round(
                            productCapitalization.TotalAmount + productCapitalizationItem.TotalAmount,
                            2,
                            MidpointRounding.AwayFromZero
                        );
                }

                productCapitalization.Storage = storage;
                productCapitalization.Responsible = responsible;
                productCapitalization.Organization = organization;

                toReturn = productCapitalization;
            }

            return productCapitalization;
        };

        _connection.Query(
            "SELECT * " +
            "FROM [ProductCapitalization] " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [ProductCapitalization].ResponsibleID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [ProductCapitalization].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [ProductCapitalizationItem] " +
            "ON [ProductCapitalizationItem].ProductCapitalizationID = [ProductCapitalization].ID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ProductCapitalizationItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductCapitalization].StorageID " +
            "WHERE [ProductCapitalization].NetUID = @NetId",
            types,
            mapper,
            new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }

    public List<ProductCapitalization> GetAllFiltered(DateTime from, DateTime to, long limit, long offset) {
        List<ProductCapitalization> toReturn = new();

        Type[] types = {
            typeof(ProductCapitalization),
            typeof(User),
            typeof(Organization),
            typeof(ProductCapitalizationItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(Storage)
        };

        Func<object[], ProductCapitalization> mapper = objects => {
            ProductCapitalization productCapitalization = (ProductCapitalization)objects[0];
            User responsible = (User)objects[1];
            Organization organization = (Organization)objects[2];
            ProductCapitalizationItem productCapitalizationItem = (ProductCapitalizationItem)objects[3];
            Product product = (Product)objects[4];
            MeasureUnit measureUnit = (MeasureUnit)objects[5];
            Storage storage = (Storage)objects[6];

            if (toReturn.Any(c => c.Id.Equals(productCapitalization.Id))) {
                ProductCapitalization fromList = toReturn.First(c => c.Id.Equals(productCapitalization.Id));

                product.MeasureUnit = measureUnit;

                productCapitalizationItem.Product = product;

                productCapitalizationItem.TotalAmount =
                    decimal.Round(Convert.ToDecimal(productCapitalizationItem.Qty) * productCapitalizationItem.UnitPrice, 2, MidpointRounding.AwayFromZero);

                fromList.ProductCapitalizationItems.Add(productCapitalizationItem);

                fromList.TotalAmount =
                    decimal.Round(
                        fromList.TotalAmount + productCapitalizationItem.TotalAmount,
                        2,
                        MidpointRounding.AwayFromZero
                    );
            } else {
                if (productCapitalizationItem != null) {
                    product.MeasureUnit = measureUnit;

                    productCapitalizationItem.Product = product;

                    productCapitalizationItem.TotalAmount =
                        decimal.Round(Convert.ToDecimal(productCapitalizationItem.Qty) * productCapitalizationItem.UnitPrice, 2, MidpointRounding.AwayFromZero);

                    productCapitalization.ProductCapitalizationItems.Add(productCapitalizationItem);

                    productCapitalization.TotalAmount =
                        decimal.Round(
                            productCapitalization.TotalAmount + productCapitalizationItem.TotalAmount,
                            2,
                            MidpointRounding.AwayFromZero
                        );
                }

                productCapitalization.Storage = storage;
                productCapitalization.Responsible = responsible;
                productCapitalization.Organization = organization;

                toReturn.Add(productCapitalization);
            }

            return productCapitalization;
        };

        _connection.Query(
            ";WITH [Search_CTE] " +
            "AS (" +
            "SELECT [ProductCapitalization].ID " +
            ", ROW_NUMBER() OVER(ORDER BY [ProductCapitalization].FromDate DESC) AS [RowNumber] " +
            "FROM [ProductCapitalization] " +
            "WHERE [ProductCapitalization].FromDate >= @From " +
            "AND [ProductCapitalization].FromDate <= @To" +
            ") " +
            "SELECT * " +
            "FROM [ProductCapitalization] " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [ProductCapitalization].ResponsibleID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [ProductCapitalization].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [ProductCapitalizationItem] " +
            "ON [ProductCapitalizationItem].ProductCapitalizationID = [ProductCapitalization].ID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ProductCapitalizationItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductCapitalization].StorageID " +
            "WHERE [ProductCapitalization].ID IN (" +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            ")",
            types,
            mapper,
            new {
                From = from,
                To = to,
                Limit = limit,
                Offset = offset,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        return toReturn;
    }

    public List<ProductCapitalization> GetAllFiltered(DateTime from, DateTime to) {
        List<ProductCapitalization> toReturn = new();

        Type[] types = {
            typeof(ProductCapitalization),
            typeof(User),
            typeof(Organization),
            typeof(ProductCapitalizationItem),
            typeof(Product),
            typeof(ProductProductGroup),
            typeof(ProductGroup),
            typeof(MeasureUnit),
            typeof(Storage)
        };

        Func<object[], ProductCapitalization> mapper = objects => {
            ProductCapitalization productCapitalization = (ProductCapitalization)objects[0];
            User responsible = (User)objects[1];
            Organization organization = (Organization)objects[2];
            ProductCapitalizationItem productCapitalizationItem = (ProductCapitalizationItem)objects[3];
            Product product = (Product)objects[4];
            ProductProductGroup productProductGroup = (ProductProductGroup)objects[5];
            ProductGroup productGroup = (ProductGroup)objects[6];
            MeasureUnit measureUnit = (MeasureUnit)objects[7];
            Storage storage = (Storage)objects[8];
            if (toReturn.Any(c => c.Id.Equals(productCapitalization.Id))) {
                ProductCapitalization fromList = toReturn.First(c => c.Id.Equals(productCapitalization.Id));

                product.MeasureUnit = measureUnit;

                if (productProductGroup != null && productGroup != null) {
                    productProductGroup.ProductGroup = productGroup;
                    product.ProductProductGroups.Add(productProductGroup);
                }

                productCapitalizationItem.Product = product;

                productCapitalizationItem.TotalAmount =
                    decimal.Round(Convert.ToDecimal(productCapitalizationItem.Qty) * productCapitalizationItem.UnitPrice, 2, MidpointRounding.AwayFromZero);

                fromList.ProductCapitalizationItems.Add(productCapitalizationItem);

                fromList.TotalAmount =
                    decimal.Round(
                        fromList.TotalAmount + productCapitalizationItem.TotalAmount,
                        2,
                        MidpointRounding.AwayFromZero
                    );
            } else {
                if (productCapitalizationItem != null) {
                    product.MeasureUnit = measureUnit;

                    if (productProductGroup != null && productGroup != null) {
                        productProductGroup.ProductGroup = productGroup;
                        product.ProductProductGroups.Add(productProductGroup);
                    }

                    productCapitalizationItem.Product = product;

                    productCapitalizationItem.TotalAmount =
                        decimal.Round(Convert.ToDecimal(productCapitalizationItem.Qty) * productCapitalizationItem.UnitPrice, 2, MidpointRounding.AwayFromZero);

                    productCapitalization.ProductCapitalizationItems.Add(productCapitalizationItem);

                    productCapitalization.TotalAmount =
                        decimal.Round(
                            productCapitalization.TotalAmount + productCapitalizationItem.TotalAmount,
                            2,
                            MidpointRounding.AwayFromZero
                        );
                }

                productCapitalization.Storage = storage;
                productCapitalization.Responsible = responsible;
                productCapitalization.Organization = organization;

                toReturn.Add(productCapitalization);
            }

            return productCapitalization;
        };

        _connection.Query(
            ";WITH [Search_CTE] " +
            "AS (" +
            "SELECT [ProductCapitalization].ID " +
            ", ROW_NUMBER() OVER(ORDER BY [ProductCapitalization].FromDate DESC) AS [RowNumber] " +
            "FROM [ProductCapitalization] " +
            "WHERE [ProductCapitalization].FromDate >= @From " +
            "AND [ProductCapitalization].FromDate <= @To" +
            ") " +
            "SELECT * " +
            "FROM [ProductCapitalization] " +
            "LEFT JOIN [User] AS [Responsible] " +
            "ON [Responsible].ID = [ProductCapitalization].ResponsibleID " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [ProductCapitalization].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [ProductCapitalizationItem] " +
            "ON [ProductCapitalizationItem].ProductCapitalizationID = [ProductCapitalization].ID " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [ProductCapitalizationItem].ProductID " +
            "LEFT JOIN ProductProductGroup " +
            "ON ProductProductGroup.ProductID = Product.ID AND ProductProductGroup.Deleted = 0 " +
            "LEFT JOIN ProductGroup " +
            "ON ProductProductGroup.ProductGroupID = ProductGroup.ID AND ProductGroup.Deleted = 0 " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductCapitalization].StorageID " +
            "WHERE [ProductCapitalization].ID IN (" +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            ")",
            types,
            mapper,
            new {
                From = from,
                To = to,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        return toReturn;
    }

    public void UpdateRemainingQty(ProductCapitalizationItem item) {
        _connection.Execute(
            "UPDATE [ProductCapitalizationItem] " +
            "SET [Updated] = GETUTCDATE() " +
            ", [RemainingQty] = @RemainingQty " +
            "WHERE [ProductCapitalizationItem].[ID] = @Id ",
            item);
    }
}