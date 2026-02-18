using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Products;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductSetRepository : IProductSetRepository {
    private readonly IDbConnection _connection;

    public ProductSetRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ProductSet productSet) {
        return _connection.Query<long>(
            "INSERT INTO ProductSet (BaseProductID, ComponentProductID, SetComponentsQty, Updated) " +
            "VALUES(@BaseProductId, @ComponentProductId, @SetComponentsQty, getutcdate());" +
            "SELECT SCOPE_IDENTITY() ",
            productSet
        ).FirstOrDefault();
    }

    public void Add(IEnumerable<ProductSet> productSets) {
        _connection.Execute(
            "INSERT INTO ProductSet (BaseProductID, ComponentProductID, SetComponentsQty, Updated) " +
            "VALUES(@BaseProductId, @ComponentProductId, @SetComponentsQty, getutcdate())",
            productSets
        );
    }

    public void Remove(ProductSet productSet) {
        _connection.Execute(
            "UPDATE ProductSet " +
            "SET Deleted = 1 " +
            "WHERE NetUID = @NetUid",
            productSet
        );
    }

    public void Remove(IEnumerable<ProductSet> productSets) {
        _connection.Execute(
            "UPDATE ProductSet " +
            "SET Deleted = 1 " +
            "WHERE NetUID = @NetUid",
            productSets
        );
    }

    public void Update(ProductSet productSet) {
        _connection.Execute(
            "UPDATE ProductSet " +
            "SET BaseProductID = @BaseProductId, ComponentProductID = @ComponentProductId, SetComponentsQty = @SetComponentsQty, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            productSet
        );
    }

    public void Update(IEnumerable<ProductSet> productSets) {
        _connection.Execute(
            "UPDATE ProductSet " +
            "SET BaseProductID = @BaseProductId, ComponentProductID = @ComponentProductId, SetComponentsQty = @SetComponentsQty, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            productSets
        );
    }

    public void RemoveAllByIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [ProductSet] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [ProductSet].ID IN @Ids",
            new { Ids = ids }
        );
    }

    public void RemoveAllByProductId(long productId) {
        _connection.Execute(
            "UPDATE [ProductSet] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [ProductSet].BaseProductID = @ProductId",
            new { ProductId = productId }
        );
    }

    public void DeleteByBaseProductNetIdAndComponentNetId(Guid baseProductNetId, Guid componentNetId) {
        _connection.Execute(
            "DELETE FROM [ProductSet] " +
            "WHERE ProductSet.ID = ( " +
            "SELECT [ProductSet].ID FROM [ProductSet] " +
            "LEFT JOIN [Product] [BaseProduct] " +
            "ON [BaseProduct].ID = [ProductSet].BaseProductID " +
            "LEFT JOIN [Product] [Component] " +
            "ON [Component].ID = [ProductSet].ComponentProductID " +
            "WHERE [BaseProduct].NetUID = @BaseProductNetId " +
            "AND [Component].NetUID = @ComponentNetId )",
            new { BaseProductNetId = baseProductNetId, ComponentNetId = componentNetId });
    }

    public void DeleteAllByIds(IEnumerable<long> ids) {
        _connection.Execute("DELETE FROM [ProductSet] WHERE ID IN @Ids",
            new { Ids = ids });
    }

    public bool CheckIsProductSetExistsByBaseProductAndComponentIds(long baseProductId, long componentId) {
        return _connection.Query<bool>(
            "SELECT " +
            "CASE WHEN EXISTS ( " +
            "SELECT * FROM ProductSet " +
            "WHERE BaseProductID = @BaseProductId " +
            "AND ComponentProductID = @ComponentProductId " +
            "AND Deleted = 0 " +
            ") THEN 1 " +
            "ELSE 0 " +
            "END AS Result ",
            new { BaseProductId = baseProductId, ComponentProductId = componentId }
        ).FirstOrDefault();
    }

    public bool CheckIfProductSetExistsByBaseProductAndComponentNetIds(Guid baseProductNetId, Guid componentNetId) {
        return _connection.Query<bool>(
            "SELECT " +
            "CASE WHEN EXISTS ( " +
            "SELECT * FROM ProductSet " +
            "LEFT JOIN [Product] [BaseProduct] " +
            "ON [BaseProduct].ID = [ProductSet].BaseProductID " +
            "LEFT JOIN [Product] [Component]" +
            "ON [Component].ID = [ProductSet].[ComponentProductID] " +
            "WHERE [BaseProduct].NetUID = @BaseProductNetId " +
            "AND [Component].NetUID = @ComponentNetId " +
            "AND [ProductSet].Deleted = 0 " +
            ") THEN 1 " +
            "ELSE 0 " +
            "END AS Result ",
            new { BaseProductNetId = baseProductNetId, ComponentNetId = componentNetId }
        ).FirstOrDefault();
    }

    public ProductSet GetByProductAndComponentIds(long baseProductId, long componentId) {
        return _connection.Query<ProductSet>(
            "SELECT * FROM ProductSet " +
            "WHERE BaseProductID = @BaseProductId " +
            "AND ComponentProductID = @ComponentProductId ",
            new { BaseProductId = baseProductId, ComponentProductId = componentId }
        ).FirstOrDefault();
    }
}