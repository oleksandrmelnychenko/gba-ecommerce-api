using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities.Products;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductImageRepository : IProductImageRepository {
    private readonly IDbConnection _connection;

    public ProductImageRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(IEnumerable<ProductImage> images) {
        _connection.Execute(
            "INSERT INTO [ProductImage] (ImageUrl, ProductId, IsMainImage, Updated) " +
            "VALUES (@ImageUrl, @ProductId, @IsMainImage, GETUTCDATE())",
            images
        );
    }

    public void Update(IEnumerable<ProductImage> images) {
        _connection.Execute(
            "UPDATE [ProductImage] " +
            "SET ImageUrl = @ImageUrl, ProductId = @ProductId, IsMainImage = @IsMainImage, Updated = GETUTCDATE() " +
            "WHERE [ProductImage].ID = @Id",
            images
        );
    }

    public void RemoveAllByIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [ProductImage] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [ProductImage].ID IN @Ids",
            new { Ids = ids }
        );
    }

    public void RemoveAllByProductId(long productId) {
        _connection.Execute(
            "UPDATE [ProductImage] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [ProductImage].ProductID = @ProductId",
            new { ProductId = productId }
        );
    }
}