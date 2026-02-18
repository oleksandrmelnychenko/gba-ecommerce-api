using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Supplies.Protocols;
using GBA.Domain.Repositories.Supplies.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Supplies.Protocols;

public sealed class SupplyInformationDeliveryProtocolKeyRepository : ISupplyInformationDeliveryProtocolKeyRepository {
    private readonly IDbConnection _connection;

    public SupplyInformationDeliveryProtocolKeyRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(SupplyInformationDeliveryProtocolKey key) {
        return _connection.Query<long>(
                "INSERT INTO SupplyInformationDeliveryProtocolKey ([Key], KeyAssignedTo, IsDefault, TransportationType, Updated) " +
                "VALUES(@Key, @KeyAssignedTo, @IsDefault, @TransportationType, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                key
            )
            .Single();
    }


    public List<SupplyInformationDeliveryProtocolKey> GetAll() {
        return _connection.Query<SupplyInformationDeliveryProtocolKey, SupplyInformationDeliveryProtocolKeyTranslation, SupplyInformationDeliveryProtocolKey>(
                "SELECT * FROM SupplyInformationDeliveryProtocolKey " +
                "LEFT JOIN SupplyInformationDeliveryProtocolKeyTranslation " +
                "ON SupplyInformationDeliveryProtocolKeyTranslation.SupplyInformationDeliveryProtocolKeyID = SupplyInformationDeliveryProtocolKey.ID " +
                "AND SupplyInformationDeliveryProtocolKeyTranslation.CultureCode = @Culture " +
                "WHERE SupplyInformationDeliveryProtocolKey.ID IN " +
                "(SELECT MAX(ID) " +
                "FROM [SupplyInformationDeliveryProtocolKey] " +
                "WHERE Deleted = 0 " +
                "GROUP BY [Key])",
                (informationKey, translation) => {
                    informationKey.Key = translation?.Key ?? informationKey.Key;

                    return informationKey;
                },
                new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
            )
            .ToList();
    }

    public List<SupplyInformationDeliveryProtocolKey> GetAllDefaultByTransportationTypeAndDestination(SupplyTransportationType type, KeyAssignedTo destination) {
        return _connection.Query<SupplyInformationDeliveryProtocolKey>(
                "SELECT * FROM SupplyInformationDeliveryProtocolKey " +
                "WHERE IsDefault = 1 " +
                "AND TransportationType = @Type " +
                "AND KeyAssignedTo = @Destination " +
                "AND Deleted = 0",
                new { Type = type, Destination = destination }
            )
            .ToList();
    }

    public List<SupplyInformationDeliveryProtocolKey> GetAllDefaultByDestination(KeyAssignedTo destination) {
        return _connection.Query<SupplyInformationDeliveryProtocolKey>(
                "SELECT * FROM SupplyInformationDeliveryProtocolKey " +
                "WHERE IsDefault = 1 " +
                "AND KeyAssignedTo = @Destination " +
                "AND Deleted = 0",
                new { Destination = destination }
            )
            .ToList();
    }

    public void Update(SupplyInformationDeliveryProtocolKey key) {
        _connection.Execute(
            "UPDATE SupplyInformationDeliveryProtocolKey SET KeyAssignedTo = @KeyAssignedTo, IsDefault = @IsDefault, TransportationType = @TransportationType, [Key] = @Key, Updated = getutcdate() " +
            "WHERE NetUID = @NetUID",
            key
        );
    }

    public void Update(IEnumerable<SupplyInformationDeliveryProtocolKey> keys) {
        _connection.Execute(
            "UPDATE SupplyInformationDeliveryProtocolKey SET KeyAssignedTo = @KeyAssignedTo, IsDefault = @IsDefault, TransportationType = @TransportationType, [Key] = @Key, Updated = getutcdate() " +
            "WHERE NetUID = @NetUID",
            keys
        );
    }
}