using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Protocols;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ISupplyOrderPolandPaymentDeliveryProtocolRepository {
    long Add(SupplyOrderPolandPaymentDeliveryProtocol protocol);

    void Add(IEnumerable<SupplyOrderPolandPaymentDeliveryProtocol> protocols);

    long Remove(long id);
}