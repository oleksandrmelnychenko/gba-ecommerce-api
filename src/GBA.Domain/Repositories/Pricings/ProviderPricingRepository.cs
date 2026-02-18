using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Repositories.Pricings.Contracts;

namespace GBA.Domain.Repositories.Pricings;

public sealed class ProviderPricingRepository : IProviderPricingRepository {
    private readonly IDbConnection _connection;

    public ProviderPricingRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ProviderPricing providerPricing) {
        return _connection.Query<long>(
                "INSERT INTO ProviderPricing (Name, CurrencyId, BasePricingId, Updated) " +
                "VALUES (@Name, @CurrencyId, @BasePricingId, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                providerPricing
            )
            .Single();
    }

    public void Update(ProviderPricing providerPricing) {
        _connection.Execute(
            "UPDATE ProviderPricing SET " +
            "Name = @Name, CurrencyId = @CurrencyId, BasePricingId = @BasePricingId, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            providerPricing
        );
    }

    public ProviderPricing GetById(long id) {
        return _connection.Query<ProviderPricing>(
                "SELECT * FROM ProviderPricing " +
                "WHERE ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public ProviderPricing GetByNetId(Guid netId) {
        return _connection.Query<ProviderPricing>(
                "SELECT * FROM ProviderPricing " +
                "WHERE NetUID = @NetId",
                new { NetId = netId.ToString() }
            )
            .SingleOrDefault();
    }

    public List<ProviderPricing> GetAll() {
        return _connection.Query<ProviderPricing>(
                "SELECT * FROM ProviderPricing " +
                "WHERE Deleted = 0"
            )
            .ToList();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE ProviderPricing SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        );
    }
}