using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Pricings;
using GBA.Domain.Repositories.Pricings.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Pricings;

public sealed class PriceTypeRepository : IPriceTypeRepository {
    private readonly IDbConnection _connection;

    public PriceTypeRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(PriceType priceType) {
        _connection.Execute(
            "INSERT INTO PriceType (Name, Updated) " +
            "VALUES (@Name, getutcdate())",
            priceType
        );
    }

    public void Update(PriceType priceType) {
        _connection.Execute(
            "UPDATE PriceType SET " +
            "Name = @Name, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            priceType
        );
    }

    public List<PriceType> GetAll() {
        return _connection.Query<PriceType, PriceTypeTranslation, PriceType>(
                "SELECT * FROM PriceType " +
                "LEFT JOIN PriceTypeTranslation " +
                "ON PriceType.ID = PriceTypeTranslation.PriceTypeID " +
                "AND PriceTypeTranslation.CultureCode = @Culture " +
                "WHERE PriceType.Deleted = 0",
                (type, translation) => {
                    type.Name = translation?.Name;

                    return type;
                },
                new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .ToList();
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE PriceType SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        );
    }
}