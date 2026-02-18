using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Protocols;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface ISupplyOrderUkrainePaymentDeliveryProtocolRepository {
    long Add(SupplyOrderUkrainePaymentDeliveryProtocol protocol);

    void Add(IEnumerable<SupplyOrderUkrainePaymentDeliveryProtocol> protocols);

    void Update(SupplyOrderUkrainePaymentDeliveryProtocol protocol);

    void Update(IEnumerable<SupplyOrderUkrainePaymentDeliveryProtocol> protocols);

    SupplyOrderUkrainePaymentDeliveryProtocol GetById(long id);

    SupplyOrderUkrainePaymentDeliveryProtocol GetByNetId(Guid netId);

    void Remove(Guid netId);

    void Remove(long id);
}