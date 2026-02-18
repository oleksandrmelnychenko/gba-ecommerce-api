using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Protocols;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ISupplyOrderPaymentDeliveryProtocolRepository {
    long Add(SupplyOrderPaymentDeliveryProtocol protocol);

    void Add(IEnumerable<SupplyOrderPaymentDeliveryProtocol> protocols);

    void Update(IEnumerable<SupplyOrderPaymentDeliveryProtocol> protocols);

    void Update(SupplyOrderPaymentDeliveryProtocol protocol);

    void UpdateSupplyInvoiceId(long fromInvoiceId, long toInvoiceId);

    SupplyOrderPaymentDeliveryProtocol GetById(long id);

    SupplyOrderPaymentDeliveryProtocol GetByNetId(Guid netId);

    List<SupplyOrderPaymentDeliveryProtocol> GetAllByTaskIds(IEnumerable<long> ids);

    void Remove(Guid netId);
}