using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Repositories.Pricings.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Pricings;

public sealed class PricingTranslationRepository : IPricingTranslationRepository {
    private readonly IDbConnection _connection;

    public PricingTranslationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(PricingTranslation pricingTranslation) {
        return _connection.Query<long>(
                "INSERT INTO PricingTranslation (Name, PricingId, CultureCode, Updated) " +
                "VALUES (@Name, @PricingId, @CultureCode, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                pricingTranslation
            )
            .Single();
    }

    public long Add(IEnumerable<PricingTranslation> pricingTranslations) {
        return _connection.Execute(
            "INSERT INTO PricingTranslation (Name, PricingId, CultureCode, Updated) " +
            "VALUES (@Name, @PricingId, @CultureCode, getutcdate())",
            pricingTranslations
        );
    }

    public void Update(PricingTranslation pricingTranslation) {
        _connection.Execute(
            "UPDATE PricingTranslation SET " +
            "Name = @Name, PricingId = @PricingId, CultureCode = @CultureCode, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            pricingTranslation
        );
    }

    public void Update(IEnumerable<PricingTranslation> pricingTranslations) {
        _connection.Execute(
            "UPDATE PricingTranslation SET " +
            "Name = @Name, PricingId = @PricingId, CultureCode = @CultureCode, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            pricingTranslations
        );
    }

    public PricingTranslation GetById(long id) {
        return _connection.Query<PricingTranslation>(
                "SELECT * FROM PricingTranslation " +
                "WHERE ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public PricingTranslation GetByNetId(Guid netId) {
        return _connection.Query<PricingTranslation>(
                "SELECT * FROM PricingTranslation " +
                "WHERE NetUID = @NetId",
                new { NetId = netId.ToString() }
            )
            .SingleOrDefault();
    }

    public List<PricingTranslation> GetAll() {
        return _connection.Query<PricingTranslation>(
                "SELECT * FROM PricingTranslation " +
                "WHERE Deleted = 0"
            )
            .ToList();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE PricingTranslation SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        );
    }
}