using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Protocols;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ISupplyOrderPaymentDeliveryProtocolKeyRepository {
    long Add(SupplyOrderPaymentDeliveryProtocolKey key);

    void Add(IEnumerable<SupplyOrderPaymentDeliveryProtocolKey> keys);

    void Update(SupplyOrderPaymentDeliveryProtocolKey key);

    void Update(IEnumerable<SupplyOrderPaymentDeliveryProtocolKey> keys);

    List<SupplyOrderPaymentDeliveryProtocolKey> GetAll();
}