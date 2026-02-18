using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Products;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductPricingRepository : IProductPricingRepository {
    private readonly IDbConnection _connection;

    public ProductPricingRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(ProductPricing productPricing) {
        _connection.Execute(
            "INSERT INTO ProductPricing (ProductID, PricingID, Price, Updated) " +
            "VALUES(@ProductId, @PricingId, @Price, getutcdate())",
            productPricing
        );
    }

    public void Add(IEnumerable<ProductPricing> productPricings) {
        _connection.Execute(
            "INSERT INTO ProductPricing (ProductID, PricingID, Price, Updated) " +
            "VALUES(@ProductId, @PricingId, @Price, getutcdate())",
            productPricings
        );
    }

    public void Remove(ProductPricing productPricing) {
        _connection.Execute(
            "UPDATE ProductPricing SET Deleted = 1 " +
            "WHERE NetUID = @NetUid",
            productPricing
        );
    }

    public void Remove(IEnumerable<ProductPricing> productPricings) {
        _connection.Execute(
            "UPDATE ProductPricing SET Deleted = 1 " +
            "WHERE NetUID = @NetUid",
            productPricings
        );
    }

    public void Update(ProductPricing productPricing) {
        _connection.Execute(
            "UPDATE ProductPricing " +
            "SET ProductID = ProductId, PricingID = @PricingId, Price = @Price, Updated = getutcdate()" +
            "WHERE NetUID = @NetUid",
            productPricing
        );
    }

    public void Update(IEnumerable<ProductPricing> productPricings) {
        _connection.Execute(
            "UPDATE ProductPricing " +
            "SET ProductID = ProductId, PricingID = @PricingId, Price = @Price, Updated = getutcdate()" +
            "WHERE NetUID = @NetUid",
            productPricings
        );
    }

    public void RemoveAllByIds(IEnumerable<long> ids) {
        _connection.Execute(
            "UPDATE [ProductPricing] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [ProductPricing].ID IN @Ids",
            new { Ids = ids }
        );
    }

    public void RemoveAllByProductId(long productId) {
        _connection.Execute(
            "UPDATE [ProductPricing] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [ProductPricing].ProductID = @ProductId",
            new { ProductId = productId }
        );
    }

    public ProductPricing GetByIdsIfExists(long productId, long pricingId) {
        return _connection.Query<ProductPricing>(
                "SELECT * " +
                "FROM [ProductPricing] " +
                "WHERE [ProductPricing].Deleted = 0 " +
                "AND [ProductPricing].ProductID = @ProductId " +
                "AND [ProductPricing].PricingID = @PricingId",
                new { ProductId = productId, PricingId = pricingId }
            )
            .SingleOrDefault();
    }
}