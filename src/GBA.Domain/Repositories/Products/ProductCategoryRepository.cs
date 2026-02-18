using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities.Products;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductCategoryRepository : IProductCategoryRepository {
    private readonly IDbConnection _connection;

    public ProductCategoryRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(ProductCategory productCategory) {
        _connection.Execute(
            "INSERT INTO ProductCategory (CategoryID, ProductID, Updated) " +
            "VALUES(@CategoryId, @ProductId, getutcdate())",
            productCategory
        );
    }

    public void Add(IEnumerable<ProductCategory> productCategories) {
        _connection.Execute(
            "INSERT INTO ProductCategory (CategoryID, ProductID, Updated) " +
            "VALUES(@CategoryId, @ProductId, getutcdate())",
            productCategories
        );
    }

    public void Remove(ProductCategory productCategory) {
        _connection.Execute(
            "UPDATE ProductCategory SET Deleted = 1 " +
            "WHERE NetUID = @NetUid",
            productCategory
        );
    }

    public void Remove(IEnumerable<ProductCategory> productCategories) {
        _connection.Execute(
            "UPDATE ProductCategory SET Deleted = 1 " +
            "WHERE NetUID = @NetUid",
            productCategories
        );
    }


    public void Update(ProductCategory productCategory) {
        _connection.Execute(
            "UPDATE ProductCategory SET CategoryID = @CategoryId, ProductID = @ProductId, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            productCategory
        );
    }

    public void Update(IEnumerable<ProductCategory> productCategories) {
        _connection.Execute(
            "UPDATE ProductCategory SET CategoryID = @CategoryId, ProductID = @ProductId, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            productCategories
        );
    }

    public void RemoveAllByIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [ProductCategory] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [ProductCategory].ID IN @Ids",
            new { Ids = ids }
        );
    }

    public void RemoveAllByProductId(long productId) {
        _connection.Execute(
            "UPDATE [ProductCategory] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [ProductCategory].ProductID = @ProductId",
            new { ProductId = productId }
        );
    }
}