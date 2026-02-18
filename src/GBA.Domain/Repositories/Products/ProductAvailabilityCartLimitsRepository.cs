using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Common.Helpers.SupplyOrders;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductAvailabilityCartLimitsRepository : IProductAvailabilityCartLimitsRepository {
    private readonly IDbConnection _connection;

    public ProductAvailabilityCartLimitsRepository(IDbConnection connection) {
        _connection = connection;
    }

    public IEnumerable<CartItemRecommendedProduct> GetAll(long storageIdPl, long storageIdUk) {
        return _connection.Query<CartItemRecommendedProduct>(
            "SELECT " +
            "[CartLimits].ProductId, " +
            "CASE WHEN [ProductAvailabilityPL].Amount - MinAvailabilityPL < MaxAvailabilityUA - [ProductAvailabilityUK].Amount " +
            "THEN [ProductAvailabilityPL].Amount - MinAvailabilityPL " +
            "ELSE MaxAvailabilityUA - [ProductAvailabilityUK].Amount " +
            "END AS Qty " +
            "FROM [ProductAvailabilityCartLimits] AS [CartLimits] " +
            "LEFT JOIN [ProductAvailability] AS [ProductAvailabilityUK] " +
            "ON [CartLimits].ProductId = [ProductAvailabilityUK].ProductID " +
            "LEFT JOIN [ProductAvailability] AS [ProductAvailabilityPL] " +
            "ON [CartLimits].ProductId = [ProductAvailabilityPL].ProductID " +
            "WHERE [ProductAvailabilityUK].Deleted = 0 " +
            "AND [ProductAvailabilityUK].StorageID = @StorageIdUK " +
            "AND [ProductAvailabilityUK].Amount < MinAvailabilityUA " +
            "AND [ProductAvailabilityPL].Deleted = 0 " +
            "AND [ProductAvailabilityPL].StorageID = @StorageIdPL " +
            "AND [ProductAvailabilityPL].Amount > MinAvailabilityPL",
            new { StorageIdPL = storageIdPl, StorageIdUK = storageIdUk }
        );
    }
}