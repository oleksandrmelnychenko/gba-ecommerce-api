using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Domain.Entities.Supplies.Protocols;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.Protocols;

public sealed class SupplyOrderPolandPaymentDeliveryProtocolRepository : ISupplyOrderPolandPaymentDeliveryProtocolRepository {
    private readonly IDbConnection _connection;

    public SupplyOrderPolandPaymentDeliveryProtocolRepository(IDbConnection connection) {
        _connection = connection;
    }


    public void Add(IEnumerable<SupplyOrderPolandPaymentDeliveryProtocol> protocols) {
        _connection.Execute(
            "INSERT INTO SupplyOrderPolandPaymentDeliveryProtocol (SupplyOrderID, UserId, SupplyPaymentTaskId, SupplyOrderPaymentDeliveryProtocolKeyId, Name, Number, GrossPrice, " +
            "NetPrice, Vat, VatPercent, FromDate, ServiceNumber, Updated, IsAccounting) " +
            "VALUES(@SupplyOrderID, @UserId, @SupplyPaymentTaskId, @SupplyOrderPaymentDeliveryProtocolKeyId, @Name, @Number, @GrossPrice, @NetPrice, @Vat, @VatPercent, @FromDate, " +
            "@ServiceNumber, getutcdate(), @IsAccounting)",
            protocols
        );
    }

    public long Add(SupplyOrderPolandPaymentDeliveryProtocol protocol) {
        return _connection.Query<long>(
                "INSERT INTO SupplyOrderPolandPaymentDeliveryProtocol (SupplyOrderID, UserId, SupplyPaymentTaskId, SupplyOrderPaymentDeliveryProtocolKeyId, Name, Number, GrossPrice, " +
                "NetPrice, Vat, VatPercent, FromDate, ServiceNumber, Updated, IsAccounting) " +
                "VALUES(@SupplyOrderID, @UserId, @SupplyPaymentTaskId, @SupplyOrderPaymentDeliveryProtocolKeyId, @Name, @Number, @GrossPrice, @NetPrice, @Vat, @VatPercent, @FromDate, " +
                "@ServiceNumber, getutcdate(), @IsAccounting); " +
                "SELECT SCOPE_IDENTITY()",
                protocol
            )
            .Single();
    }

    public long Remove(long id) {
        return _connection.Execute(
            "UPDATE [SupplyOrderPolandPaymentDeliveryProtocol] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE ID = @Id",
            new { Id = id }
        );
    }
}