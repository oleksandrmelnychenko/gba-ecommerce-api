using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Clients.PackingMarkings;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Domain.Repositories.Clients;

public sealed class PackingMarkingRepository : IPackingMarkingRepository {
    private readonly IDbConnection _connection;

    public PackingMarkingRepository(IDbConnection connection) {
        _connection = connection;
    }

    public List<PackingMarking> GetAll() {
        return _connection.Query<PackingMarking>(
                "SELECT * FROM PackingMarking WHERE Deleted = 0"
            )
            .ToList();
    }
}