using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Repositories.Delivery.Contracts;

namespace GBA.Domain.Repositories.Delivery;

public sealed class TermsOfDeliveryRepository : ITermsOfDeliveryRepository {
    private readonly IDbConnection _connection;

    public TermsOfDeliveryRepository(IDbConnection connection) {
        _connection = connection;
    }

    public List<TermsOfDelivery> GetAll() {
        return _connection.Query<TermsOfDelivery>(
                "SELECT * FROM TermsOfDelivery WHERE Deleted = 0"
            )
            .ToList();
    }
}