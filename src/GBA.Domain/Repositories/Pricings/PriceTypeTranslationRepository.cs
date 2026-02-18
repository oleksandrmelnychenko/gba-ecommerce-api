using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Repositories.Pricings.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Pricings;

public sealed class PriceTypeTranslationRepository : IPriceTypeTranslationRepository {
    private readonly IDbConnection _connection;

    public PriceTypeTranslationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(PriceTypeTranslation priceTypeTranslation) {
        return _connection.Query<long>(
                "INSERT INTO PriceTypeTranslation (Name, CultureCode, PriceTypeId, Updated) " +
                "VALUES (@Name, @CultureCode, @PriceTypeId, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                priceTypeTranslation
            )
            .Single();
    }

    public void Update(PriceTypeTranslation priceTypeTranslation) {
        _connection.Execute(
            "UPDATE PriceTypeTranslation SET " +
            "Name = @Name, CultureCode = @CultureCode, PriceTypeId = @PriceTypeId, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            priceTypeTranslation
        );
    }

    public PriceTypeTranslation GetById(long id) {
        return _connection.Query<PriceTypeTranslation>(
                "SELECT * FROM PriceTypeTranslation " +
                "WHERE ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public PriceTypeTranslation GetByNetId(Guid netId) {
        return _connection.Query<PriceTypeTranslation>(
                "SELECT * FROM PriceTypeTranslation " +
                "WHERE NetUID = @NetId",
                new { NetId = netId.ToString() }
            )
            .SingleOrDefault();
    }

    public List<PriceTypeTranslation> GetAll() {
        return _connection.Query<PriceTypeTranslation>(
                "SELECT * FROM PriceTypeTranslation " +
                "WHERE Deleted = 0"
            )
            .ToList();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE PriceTypeTranslation SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        );
    }
}