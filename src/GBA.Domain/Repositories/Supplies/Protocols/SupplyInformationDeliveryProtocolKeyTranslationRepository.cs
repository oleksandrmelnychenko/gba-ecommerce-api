using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Supplies.Protocols;

public sealed class SupplyInformationDeliveryProtocolKeyTranslationRepository : ISupplyInformationDeliveryProtocolKeyTranslationRepository {
    private readonly IDbConnection _connection;

    public SupplyInformationDeliveryProtocolKeyTranslationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(IEnumerable<SupplyInformationDeliveryProtocolKeyTranslation> keyTranslations) {
        _connection.Execute(
            "INSERT INTO SupplyInformationDeliveryProtocolKeyTranslation (CultureCode, Key, Updated) VALUES(@CultureCode, @Key, getutcdate())",
            keyTranslations
        );
    }

    public void Update(IEnumerable<SupplyInformationDeliveryProtocolKeyTranslation> keyTranslations) {
        _connection.Execute(
            "UPDATE SupplyInformationDeliveryProtocolKeyTranslation SET CultureCode = @CultureCode, Key = @Key, Updated = getutcdate() WHERE NetUID = @NetUID",
            keyTranslations
        );
    }
}