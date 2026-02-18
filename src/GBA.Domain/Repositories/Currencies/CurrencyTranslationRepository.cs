using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Currencies;

public sealed class CurrencyTranslationRepository : ICurrencyTranslationRepository {
    private readonly IDbConnection _connection;

    public CurrencyTranslationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(CurrencyTranslation currencyTranslation) {
        _connection.Execute(
            "INSERT INTO CurrencyTranslation (Name, CultureCode, CurrencyId, Updated) " +
            "VALUES (@Name, @CultureCode, @CurrencyId, getutcdate())",
            currencyTranslation
        );
    }

    public void Add(IEnumerable<CurrencyTranslation> currencyTranslations) {
        _connection.Execute(
            "INSERT INTO CurrencyTranslation (Name, CultureCode, CurrencyId, Updated) " +
            "VALUES (@Name, @CultureCode, @CurrencyId, getutcdate())",
            currencyTranslations
        );
    }

    public void Update(CurrencyTranslation currencyTranslation) {
        _connection.Execute(
            "UPDATE CurrencyTranslation SET " +
            "Name = @Name, CultureCode = @CultureCode, CurrencyId = @CurrencyId, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            currencyTranslation
        );
    }

    public void Update(IEnumerable<CurrencyTranslation> currencyTranslations) {
        _connection.Execute(
            "UPDATE CurrencyTranslation SET " +
            "Name = @Name, CultureCode = @CultureCode, CurrencyId = @CurrencyId, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            currencyTranslations
        );
    }
}