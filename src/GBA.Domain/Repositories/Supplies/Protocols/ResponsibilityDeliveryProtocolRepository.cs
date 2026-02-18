using System.Collections.Generic;
using System.Data;
using Dapper;
using GBA.Domain.Entities.Supplies.Protocols;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.Protocols;

public sealed class ResponsibilityDeliveryProtocolRepository : IResponsibilityDeliveryProtocolRepository {
    private readonly IDbConnection _connection;

    public ResponsibilityDeliveryProtocolRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(IEnumerable<ResponsibilityDeliveryProtocol> responsibilityDeliveryProtocols) {
        _connection.Execute(
            "INSERT INTO ResponsibilityDeliveryProtocol (SupplyOrderID, UserID, SupplyOrderStatus, Updated, Created) " +
            "VALUES(@SupplyOrderID, @UserID, @SupplyOrderStatus, getutcdate(), @Created)",
            responsibilityDeliveryProtocols
        );
    }

    public void Update(IEnumerable<ResponsibilityDeliveryProtocol> responsibilityDeliveryProtocols) {
        _connection.Execute(
            "UPDATE ResponsibilityDeliveryProtocol " +
            "SET SupplyOrderID = @SupplyOrderID, UserID = @UserID, SupplyOrderStatus = @SupplyOrderStatus, Created = @Created, Updated = getutcdate() " +
            "WHERE NetUID = @NetUID",
            responsibilityDeliveryProtocols
        );
    }
}