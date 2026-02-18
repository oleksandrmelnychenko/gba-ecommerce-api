using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Protocols;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface ISupplyOrderUkrainePaymentDeliveryProtocolKeyRepository {
    long Add(SupplyOrderUkrainePaymentDeliveryProtocolKey protocolKey);

    void Add(IEnumerable<SupplyOrderUkrainePaymentDeliveryProtocolKey> protocolKeys);

    void Update(SupplyOrderUkrainePaymentDeliveryProtocolKey protocolKey);

    void Update(IEnumerable<SupplyOrderUkrainePaymentDeliveryProtocolKey> protocolKeys);

    IEnumerable<SupplyOrderUkrainePaymentDeliveryProtocolKey> GetAll();
}