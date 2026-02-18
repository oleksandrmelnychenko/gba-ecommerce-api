using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.HelperServices;

public sealed class ServiceDetailItemKeyRepository : IServiceDetailItemKeyRepository {
    private readonly IDbConnection _connection;

    public ServiceDetailItemKeyRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ServiceDetailItemKey key) {
        return _connection.Query<long>(
                "INSERT INTO ServiceDetailItemKey ([Name], [Symbol], [Type], Updated) VALUES(@Name, @Symbol, @Type, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                key
            )
            .Single();
    }

    public void Update(ServiceDetailItemKey key) {
        _connection.Execute(
            "UPDATE ServiceDetailItemKey SET [Name] = @Name, [Symbol] = @Symbol, [Type] = @Type, Updated = getutcdate() WHERE NetUID = @NetUID",
            key
        );
    }

    public void Update(IEnumerable<ServiceDetailItemKey> keys) {
        _connection.Execute(
            "UPDATE ServiceDetailItemKey SET [Name] = @Name, [Symbol] = @Symbol, [Type] = @Type, Updated = getutcdate() WHERE NetUID = @NetUID",
            keys
        );
    }

    public ServiceDetailItemKey GetByFieldsIfExists(string name, string symbol, SupplyServiceType type) {
        return _connection.Query<ServiceDetailItemKey>(
                "SELECT TOP(1) * " +
                "FROM [ServiceDetailItemKey] " +
                "WHERE [ServiceDetailItemKey].Deleted = 0 " +
                "AND [ServiceDetailItemKey].[Name] = @Name " +
                "AND [ServiceDetailItemKey].[Symbol] = @Symbol " +
                "AND [ServiceDetailItemKey].[Type] = @Type",
                new { Name = name, Symbol = symbol, Type = type }
            )
            .SingleOrDefault();
    }

    public List<ServiceDetailItemKey> GetAllByType(SupplyServiceType type) {
        return _connection.Query<ServiceDetailItemKey>(
                "SELECT * FROM ServiceDetailItemKey WHERE [Type] = @Type AND Deleted = 0",
                new { Type = type }
            )
            .ToList();
    }
}