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

public sealed class ConsumableProductRepository : IConsumableProductRepository {
    private readonly IDbConnection _connection;

    public ConsumableProductRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ConsumableProduct consumableProduct) {
        return _connection.Query<long>(
                "INSERT INTO [ConsumableProduct] (Name, VendorCode, ConsumableProductCategoryId, MeasureUnitId, Updated) " +
                "VALUES (@Name, @VendorCode, @ConsumableProductCategoryId, @MeasureUnitId, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                consumableProduct
            )
            .Single();
    }

    public void Update(ConsumableProduct consumableProduct) {
        _connection.Execute(
            "UPDATE [ConsumableProduct] " +
            "SET Name = @Name, ConsumableProductCategoryId = @ConsumableProductCategoryId, MeasureUnitId = @MeasureUnitId, Updated = getutcdate() " +
            "WHERE [ConsumableProduct].ID = @Id",
            consumableProduct
        );
    }

    public ConsumableProduct GetLastRecord() {
        return _connection.Query<ConsumableProduct>(
                "SELECT TOP(1) * " +
                "FROM [ConsumableProduct] " +
                "WHERE [ConsumableProduct].Deleted = 0 " +
                "ORDER BY [ConsumableProduct].ID DESC"
            )
            .SingleOrDefault();
    }

    public ConsumableProduct GetById(long id) {
        return _connection.Query<ConsumableProduct, MeasureUnit, ConsumableProduct>(
                "SELECT [ConsumableProduct].ID " +
                ", [ConsumableProduct].[ConsumableProductCategoryID] " +
                ", [ConsumableProduct].[Created] " +
                ", [ConsumableProduct].[VendorCode] " +
                ", [ConsumableProduct].[Deleted] " +
                ", (CASE WHEN [ConsumableProductTranslation].Name IS NOT NULL THEN [ConsumableProductTranslation].Name ELSE [ConsumableProduct].Name END) AS [Name] " +
                ", [ConsumableProduct].[NetUID] " +
                ", [ConsumableProduct].[Updated] " +
                ", [MeasureUnit].* " +
                "FROM [ConsumableProduct] " +
                "LEFT JOIN [ConsumableProductTranslation] " +
                "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
                "AND [ConsumableProductTranslation].CultureCode = @Culture " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [ConsumableProduct].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "WHERE [ConsumableProduct].ID = @Id",
                (product, measureUnit) => {
                    product.MeasureUnit = measureUnit;

                    return product;
                },
                new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .SingleOrDefault();
    }

    public ConsumableProduct GetByNetId(Guid netId) {
        return _connection.Query<ConsumableProduct, MeasureUnit, ConsumableProduct>(
                "SELECT [ConsumableProduct].ID " +
                ", [ConsumableProduct].[ConsumableProductCategoryID] " +
                ", [ConsumableProduct].[Created] " +
                ", [ConsumableProduct].[VendorCode] " +
                ", [ConsumableProduct].[Deleted] " +
                ", (CASE WHEN [ConsumableProductTranslation].Name IS NOT NULL THEN [ConsumableProductTranslation].Name ELSE [ConsumableProduct].Name END) AS [Name] " +
                ", [ConsumableProduct].[NetUID] " +
                ", [ConsumableProduct].[Updated] " +
                ", [MeasureUnit].* " +
                "FROM [ConsumableProduct] " +
                "LEFT JOIN [ConsumableProductTranslation] " +
                "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
                "AND [ConsumableProductTranslation].CultureCode = @Culture " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [ConsumableProduct].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "WHERE [ConsumableProduct].NetUID = @NetId",
                (product, measureUnit) => {
                    product.MeasureUnit = measureUnit;

                    return product;
                },
                new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .SingleOrDefault();
    }

    public IEnumerable<ConsumableProduct> GetAll() {
        return _connection.Query<ConsumableProduct, MeasureUnit, ConsumableProduct>(
            "SELECT [ConsumableProduct].ID " +
            ", [ConsumableProduct].[ConsumableProductCategoryID] " +
            ", [ConsumableProduct].[Created] " +
            ", [ConsumableProduct].[VendorCode] " +
            ", [ConsumableProduct].[Deleted] " +
            ", (CASE WHEN [ConsumableProductTranslation].Name IS NOT NULL THEN [ConsumableProductTranslation].Name ELSE [ConsumableProduct].Name END) AS [Name] " +
            ", [ConsumableProduct].[NetUID] " +
            ", [ConsumableProduct].[Updated] " +
            ", [MeasureUnit].* " +
            "FROM [ConsumableProduct] " +
            "LEFT JOIN [ConsumableProductTranslation] " +
            "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
            "AND [ConsumableProductTranslation].CultureCode = @Culture " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [ConsumableProduct].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "WHERE [ConsumableProduct].Deleted = 0",
            (product, measureUnit) => {
                product.MeasureUnit = measureUnit;

                return product;
            },
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );
    }

    public IEnumerable<ConsumableProduct> GetAllFromSearchByVendorCode(string value) {
        return _connection.Query<ConsumableProduct, ConsumableProductCategory, MeasureUnit, ConsumableProduct>(
            "SELECT [ConsumableProduct].ID " +
            ", [ConsumableProduct].[ConsumableProductCategoryID] " +
            ", [ConsumableProduct].[Created] " +
            ", [ConsumableProduct].[VendorCode] " +
            ", [ConsumableProduct].[Deleted] " +
            ", (CASE WHEN [ConsumableProductTranslation].Name IS NOT NULL THEN [ConsumableProductTranslation].Name ELSE [ConsumableProduct].Name END) AS [Name] " +
            ", [ConsumableProduct].[NetUID] " +
            ", [ConsumableProduct].[Updated] " +
            ", [ConsumableProductCategory].* " +
            ", [MeasureUnit].* " +
            "FROM [ConsumableProduct] " +
            "LEFT JOIN [ConsumableProductTranslation] " +
            "ON [ConsumableProductTranslation].ConsumableProductID = [ConsumableProduct].ID " +
            "AND [ConsumableProductTranslation].CultureCode = @Culture " +
            "LEFT JOIN (" +
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
            ") AS [ConsumableProductCategory] " +
            "ON [ConsumableProductCategory].ID = [ConsumableProduct].ConsumableProductCategoryID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [ConsumableProduct].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "WHERE [ConsumableProduct].Deleted = 0 " +
            "AND [ConsumableProduct].VendorCode like '%' + @Value + '%'",
            (product, category, measureUnit) => {
                product.ConsumableProductCategory = category;
                product.MeasureUnit = measureUnit;

                return product;
            },
            new { Value = value, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [ConsumableProduct] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [ConsumableProduct].NetUID = @NetId",
            new { NetId = netId }
        );
    }
}