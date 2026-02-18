using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Repositories.Consumables.Contracts;

namespace GBA.Domain.Repositories.Consumables;

public sealed class ConsumableProductCategoryRepository : IConsumableProductCategoryRepository {
    private readonly IDbConnection _connection;

    public ConsumableProductCategoryRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ConsumableProductCategory consumableProductCategory) {
        return _connection.Query<long>(
                "INSERT INTO [ConsumableProductCategory] (Name, Description, Updated, [IsSupplyServiceCategory]) " +
                "VALUES (@Name, @Description, getutcdate(), @IsSupplyServiceCategory); " +
                "SELECT SCOPE_IDENTITY()",
                consumableProductCategory
            )
            .Single();
    }

    public void Update(ConsumableProductCategory consumableProductCategory) {
        _connection.Execute(
            "UPDATE [ConsumableProductCategory] " +
            "SET Name = @Name, Description = @Description, Updated = getutcdate()" +
            ", [IsSupplyServiceCategory] = @IsSupplyServiceCategory " +
            "WHERE [ConsumableProductCategory].ID = @Id",
            consumableProductCategory
        );
    }

    public ConsumableProductCategory GetById(long id) {
        return _connection.Query<ConsumableProductCategory>(
                "SELECT [ConsumableProductCategory].ID " +
                ", [ConsumableProductCategory].[Created] " +
                ", [ConsumableProductCategory].[Deleted] " +
                ", [ConsumableProductCategory].[NetUID] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Name IS NOT NULL THEN [ConsumableProductCategoryTranslation].Name ELSE [ConsumableProductCategory].Name END) AS [Name] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Description IS NOT NULL THEN [ConsumableProductCategoryTranslation].Description ELSE [ConsumableProductCategory].Description END) AS [Description] " +
                ", [ConsumableProductCategory].[Updated] " +
                "FROM [ConsumableProductCategory] " +
                "LEFT JOIN [ConsumableProductCategoryTranslation] " +
                "ON [ConsumableProductCategoryTranslation].ConsumableProductCategoryID = [ConsumableProductCategory].ID " +
                "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture " +
                "WHERE [ConsumableProductCategory].ID = @Id",
                new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .Single();
    }

    public ConsumableProductCategory GetByNetId(Guid netId) {
        return _connection.Query<ConsumableProductCategory>(
                "SELECT [ConsumableProductCategory].ID " +
                ", [ConsumableProductCategory].[Created] " +
                ", [ConsumableProductCategory].[Deleted] " +
                ", [ConsumableProductCategory].[NetUID] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Name IS NOT NULL THEN [ConsumableProductCategoryTranslation].Name ELSE [ConsumableProductCategory].Name END) AS [Name] " +
                ", (CASE WHEN [ConsumableProductCategoryTranslation].Description IS NOT NULL THEN [ConsumableProductCategoryTranslation].Description ELSE [ConsumableProductCategory].Description END) AS [Description] " +
                ", [ConsumableProductCategory].[Updated] " +
                "FROM [ConsumableProductCategory] " +
                "LEFT JOIN [ConsumableProductCategoryTranslation] " +
                "ON [ConsumableProductCategoryTranslation].ConsumableProductCategoryID = [ConsumableProductCategory].ID " +
                "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture " +
                "WHERE [ConsumableProductCategory].NetUID = @NetId",
                new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .Single();
    }

    public IEnumerable<ConsumableProductCategory> GetAll() {
        List<ConsumableProductCategory> toReturn = new();

        _connection.Query<ConsumableProductCategory, ConsumableProduct, MeasureUnit, ConsumableProductCategory>(
            "SELECT [ConsumableProductCategory].ID " +
            ", [ConsumableProductCategory].[Created] " +
            ", [ConsumableProductCategory].[Deleted] " +
            ", [ConsumableProductCategory].[NetUID] " +
            ", [ConsumableProductCategory].[IsSupplyServiceCategory] " +
            ", (CASE WHEN [ConsumableProductCategoryTranslation].Name IS NOT NULL THEN [ConsumableProductCategoryTranslation].Name ELSE [ConsumableProductCategory].Name END) AS [Name] " +
            ", (CASE WHEN [ConsumableProductCategoryTranslation].Description IS NOT NULL THEN [ConsumableProductCategoryTranslation].Description ELSE [ConsumableProductCategory].Description END) AS [Description] " +
            ", [ConsumableProductCategory].[Updated] " +
            ", [ConsumableProduct].ID " +
            ", [ConsumableProduct].[ConsumableProductCategoryID] " +
            ", [ConsumableProduct].[Created] " +
            ", [ConsumableProduct].[Deleted] " +
            ", [ConsumableProduct].[VendorCode] " +
            ", (CASE WHEN [ConsumableProductTranslation].Name IS NOT NULL THEN [ConsumableProductTranslation].Name ELSE [ConsumableProduct].Name END) AS [Name] " +
            ", [ConsumableProduct].[NetUID] " +
            ", [ConsumableProduct].[Updated] " +
            ", [MeasureUnit].* " +
            "FROM [ConsumableProductCategory] " +
            "LEFT JOIN [ConsumableProductCategoryTranslation] " +
            "ON [ConsumableProductCategoryTranslation].ConsumableProductCategoryID = [ConsumableProductCategory].ID " +
            "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture " +
            "LEFT JOIN [ConsumableProduct] " +
            "ON [ConsumableProduct].ConsumableProductCategoryID = [ConsumableProductCategory].ID " +
            "AND [ConsumableProduct].Deleted = 0 " +
            "LEFT JOIN [ConsumableProductTranslation] " +
            "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
            "AND [ConsumableProductTranslation].CultureCode = @Culture " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [ConsumableProduct].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "WHERE [ConsumableProductCategory].Deleted = 0 " +
            "ORDER BY [ConsumableProductCategory].Name, [ConsumableProductCategoryTranslation].Name, [ConsumableProduct].Name, [ConsumableProductTranslation].Name",
            (category, product, measureUnit) => {
                if (toReturn.Any(c => c.Id.Equals(category.Id))) {
                    if (product == null) return category;

                    product.MeasureUnit = measureUnit;

                    toReturn.First(c => c.Id.Equals(category.Id)).ConsumableProducts.Add(product);
                } else {
                    if (product != null) {
                        product.MeasureUnit = measureUnit;

                        category.ConsumableProducts.Add(product);
                    }

                    toReturn.Add(category);
                }

                return category;
            },
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }

    public IEnumerable<ConsumableProductCategory> GetAllFromSearch(string value) {
        List<ConsumableProductCategory> toReturn = new();

        _connection.Query<ConsumableProductCategory, ConsumableProduct, MeasureUnit, ConsumableProductCategory>(
            "SELECT [ConsumableProductCategory].ID " +
            ", [ConsumableProductCategory].[Created] " +
            ", [ConsumableProductCategory].[Deleted] " +
            ", [ConsumableProductCategory].[NetUID] " +
            ", [ConsumableProductCategory].[IsSupplyServiceCategory] " +
            ", (CASE WHEN [ConsumableProductCategoryTranslation].Name IS NOT NULL THEN [ConsumableProductCategoryTranslation].Name ELSE [ConsumableProductCategory].Name END) AS [Name] " +
            ", (CASE WHEN [ConsumableProductCategoryTranslation].Description IS NOT NULL THEN [ConsumableProductCategoryTranslation].Description ELSE [ConsumableProductCategory].Description END) AS [Description] " +
            ", [ConsumableProductCategory].[Updated] " +
            ", [ConsumableProduct].ID " +
            ", [ConsumableProduct].[ConsumableProductCategoryID] " +
            ", [ConsumableProduct].[Created] " +
            ", [ConsumableProduct].[VendorCode] " +
            ", [ConsumableProduct].[Deleted] " +
            ", (CASE WHEN [ConsumableProductTranslation].Name IS NOT NULL THEN [ConsumableProductTranslation].Name ELSE [ConsumableProduct].Name END) AS [Name] " +
            ", [ConsumableProduct].[NetUID] " +
            ", [ConsumableProduct].[Updated] " +
            ", [MeasureUnit].* " +
            "FROM [ConsumableProductCategory] " +
            "LEFT JOIN [ConsumableProductCategoryTranslation] " +
            "ON [ConsumableProductCategoryTranslation].ConsumableProductCategoryID = [ConsumableProductCategory].ID " +
            "AND [ConsumableProductCategoryTranslation].CultureCode = @Culture " +
            "LEFT JOIN [ConsumableProduct] " +
            "ON [ConsumableProduct].ConsumableProductCategoryID = [ConsumableProductCategory].ID " +
            "AND [ConsumableProduct].Deleted = 0 " +
            "LEFT JOIN [ConsumableProductTranslation] " +
            "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
            "AND [ConsumableProductTranslation].CultureCode = @Culture " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [ConsumableProduct].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "WHERE [ConsumableProductCategory].Deleted = 0 " +
            "AND (" +
            "[ConsumableProductCategory].Name like '%' + @Value + '%' " +
            "OR " +
            "[ConsumableProductCategoryTranslation].Name like '%' + @Value + '%' " +
            "OR " +
            "[ConsumableProduct].Name like '%' + @Value + '%' " +
            "OR " +
            "[ConsumableProductTranslation].Name like '%' + @Value + '%'" +
            ") " +
            "ORDER BY [ConsumableProductCategory].Name, [ConsumableProductCategoryTranslation].Name, [ConsumableProduct].Name, [ConsumableProductTranslation].Name",
            (category, product, measureUnit) => {
                if (toReturn.Any(c => c.Id.Equals(category.Id))) {
                    if (product != null) {
                        product.MeasureUnit = measureUnit;

                        toReturn.First(c => c.Id.Equals(category.Id)).ConsumableProducts.Add(product);
                    }
                } else {
                    if (product != null) {
                        product.MeasureUnit = measureUnit;

                        category.ConsumableProducts.Add(product);
                    }

                    toReturn.Add(category);
                }

                return category;
            },
            new { Value = value, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [ConsumableProductCategory] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [ConsumableProductCategory].NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public ConsumableProductCategory GetConsumableProductCategoriesSupplyService(
        string value) {
        ConsumableProductCategory toReturn = new();

        Type[] types = {
            typeof(ConsumableProductCategory),
            typeof(ConsumableProduct)
        };

        Func<object[], ConsumableProductCategory> mapper = objects => {
            ConsumableProductCategory category = (ConsumableProductCategory)objects[0];
            ConsumableProduct product = (ConsumableProduct)objects[1];

            if (toReturn.Id.Equals(0))
                toReturn = category;

            if (!toReturn.ConsumableProducts.Any(x => x.Id.Equals(product.Id)))
                toReturn.ConsumableProducts.Add(product);

            return category;
        };

        _connection.Query(
            "SELECT * FROM [ConsumableProductCategory] " +
            "LEFT JOIN [ConsumableProduct] " +
            "ON [ConsumableProduct].[ConsumableProductCategoryID] = [ConsumableProductCategory].[ID] " +
            "AND [ConsumableProduct].[Deleted] = 0 " +
            "WHERE [ConsumableProductCategory].[Deleted] = 0 " +
            "AND [ConsumableProductCategory].[IsSupplyServiceCategory] = 1 " +
            "AND [ConsumableProduct].Name like '%' + @Value + '%' ",
            types, mapper, new { Value = value });

        return toReturn;
    }

    public bool IsCategoryForSupplyService() {
        return _connection.Query<bool>(
            "SELECT IIF(COUNT([ConsumableProductCategory].[ID])>0,1,0) " +
            "FROM [ConsumableProductCategory] " +
            "WHERE [ConsumableProductCategory].[IsSupplyServiceCategory] = 1 ").SingleOrDefault();
    }

    public void UpdateAllCategorySupplyService() {
        _connection.Execute(
            "UPDATE [ConsumableProductCategory] " +
            "SET [IsSupplyServiceCategory] = 0 " +
            "WHERE [ConsumableProductCategory].[IsSupplyServiceCategory] = 1 ");
    }

    public ConsumableProductCategory GetConsumableCategoriesSupplyServiceIfExist() {
        return _connection.Query<ConsumableProductCategory>(
            "SELECT TOP 1 * FROM [ConsumableProductCategory] " +
            "WHERE [ConsumableProductCategory].[Deleted] = 0 " +
            "AND [ConsumableProductCategory].[IsSupplyServiceCategory] = 1" +
            "ORDER BY [ConsumableProductCategory].[Created] DESC ").FirstOrDefault();
    }
}