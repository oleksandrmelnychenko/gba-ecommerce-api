using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Ecommerce;
using GBA.Domain.Repositories.Ecommerce.Contracts;

namespace GBA.Domain.Repositories.Ecommerce;

public sealed class EcommerceDefaultPricingRepository : IEcommerceDefaultPricingRepository {
    private readonly IDbConnection _connection;

    public EcommerceDefaultPricingRepository(IDbConnection connection) {
        _connection = connection;
    }

    public EcommerceDefaultPricing GetLast() {
        return _connection.Query<EcommerceDefaultPricing>(
                "SELECT TOP(1) * FROM [EcommerceDefaultPricing] " +
                "WHERE [Deleted] = 0")
            .SingleOrDefault();
    }

    public long Add(EcommerceDefaultPricing defaultPricing) {
        return _connection.Query<long>(
                "INSERT INTO [EcommerceDefaultPricing] (PricingId, PromotionalPricingId, Updated) " +
                "VALUES (@PricingId, @PromotionalPricingId, GETUTCDATE()); " +
                "SELECT SCOPE_IDENTITY()",
                defaultPricing)
            .SingleOrDefault();
    }
}