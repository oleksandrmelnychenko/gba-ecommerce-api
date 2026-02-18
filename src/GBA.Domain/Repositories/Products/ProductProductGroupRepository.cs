using System;
using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Products;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductProductGroupRepository : IProductProductGroupRepository {
    private readonly IDbConnection _connection;

    public ProductProductGroupRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(ProductProductGroup productProductGroup) {
        _connection.Execute(
            "INSERT INTO ProductProductGroup (ProductGroupID, ProductID, Updated) " +
            "VALUES(@ProductGroupId, @ProductId, getutcdate())",
            productProductGroup
        );
    }

    public void Add(IEnumerable<ProductProductGroup> productProductGroups) {
        _connection.Execute(
            "INSERT INTO ProductProductGroup (ProductGroupID, ProductID, Updated) " +
            "VALUES(@ProductGroupId, @ProductId, getutcdate())",
            productProductGroups
        );
    }

    public void Remove(ProductProductGroup productProductGroup) {
        _connection.Execute(
            "UPDATE ProductProductGroup " +
            "SET Deleted = 1 " +
            "WHERE NetUID = @NetUid",
            productProductGroup
        );
    }

    public void Remove(IEnumerable<ProductProductGroup> productProductGroups) {
        _connection.Execute(
            "UPDATE ProductProductGroup " +
            "SET Deleted = 1 " +
            "WHERE NetUID = @NetUid",
            productProductGroups
        );
    }

    public void Update(ProductProductGroup productProductGroup) {
        _connection.Execute(
            "UPDATE ProductProductGroup " +
            "SET ProductGroupID = @ProductGroupId, ProductID = @ProductId, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            productProductGroup
        );
    }

    public void Update(IEnumerable<ProductProductGroup> productProductGroups) {
        _connection.Execute(
            "UPDATE ProductProductGroup " +
            "SET ProductGroupID = @ProductGroupId, ProductID = @ProductId, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            productProductGroups
        );
    }

    public void RemoveAllByIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [ProductProductGroup] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [ProductProductGroup].ID IN @Ids",
            new { Ids = ids }
        );
    }

    public void RemoveAllByProductId(long productId) {
        _connection.Execute(
            "UPDATE [ProductProductGroup] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [ProductProductGroup].ProductID = @ProductId",
            new { ProductId = productId }
        );
    }

    public List<ProductProductGroup> GetFilteredByProductGroupNetId(
        Guid netId,
        int limit,
        int offset,
        string value) {
        return _connection.Query<ProductProductGroup, Product, MeasureUnit, ProductProductGroup>(
            ";WITH [TOTAL_SEARCH] AS ( " +
            "SELECT " +
            "ROW_NUMBER() OVER(ORDER BY [ProductProductGroup].ID DESC) AS [RowNumber] " +
            ", [ProductProductGroup].[ID] " +
            "FROM [ProductProductGroup] " +
            "LEFT JOIN [ProductGroup] " +
            "ON [ProductGroup].[ID] = [ProductProductGroup].[ProductGroupID] " +
            "LEFT JOIN [Product] " +
            "ON [Product].[ID] = [ProductProductGroup].[ProductID] " +
            "WHERE [ProductProductGroup].[Deleted] = 0 " +
            "AND [ProductGroup].[NetUID] = @NetId " +
            "AND ( " +
            "[Product].[Name] LIKE '%' + @Value + '%' " +
            "OR [Product].[Description] LIKE '%' + @Value + '%' " +
            ") " +
            "GROUP BY [ProductProductGroup].[ID] " +
            ") " +
            "SELECT " +
            "* " +
            "FROM [ProductProductGroup] " +
            "LEFT JOIN [Product] " +
            "ON [Product].[ID] = [ProductProductGroup].[ProductID] " +
            "WHERE [ProductProductGroup].[ID] IN ( " +
            "SELECT [TOTAL_SEARCH].[ID] " +
            "FROM [TOTAL_SEARCH] " +
            "WHERE [TOTAL_SEARCH].[RowNumber] > @Offset " +
            "AND [TOTAL_SEARCH].[RowNumber] <= @Offset + @Limit " +
            "); ",
            (productProductGroup, product, measureUnit) => {
                product.MeasureUnit = measureUnit;
                productProductGroup.Product = product;
                return productProductGroup;
            }, new {
                NetId = netId,
                Limit = limit,
                Offset = offset,
                Value = value
            }).AsList();
    }
}