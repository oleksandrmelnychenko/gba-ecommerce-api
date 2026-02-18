using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Clients.PackingMarkings;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Domain.Repositories.Clients;

public sealed class PackingMarkingPaymentRepository : IPackingMarkingPaymentRepository {
    private readonly IDbConnection _connection;

    public PackingMarkingPaymentRepository(IDbConnection connection) {
        _connection = connection;
    }

    public List<PackingMarkingPayment> GetAll() {
        return _connection.Query<PackingMarkingPayment>(
                "SELECT * FROM PackingMarkingPayment WHERE Deleted = 0"
            )
            .ToList();
    }
}