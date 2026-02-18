using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.AuditEntities;
using GBA.Domain.Repositories.Auditing.Contracts;

namespace GBA.Domain.Repositories.Auditing;

public sealed class AuditPropertiesRepository : IAuditPropertiesRepository {
    private readonly IDbConnection _connection;

    public AuditPropertiesRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(IEnumerable<AuditEntityProperty> properties) {
        _connection.Execute(
            "INSERT INTO AuditEntityProperty (Type, Name, Description, Value, AuditEntityId, Updated) " +
            "VALUES (@Type, @Name, @Description, @Value, @AuditEntityId, getutcdate())",
            properties
        );
    }
}