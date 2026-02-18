using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Protocols;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ISupplyInformationDeliveryProtocolRepository {
    long Add(SupplyInformationDeliveryProtocol protocol);

    void Add(IEnumerable<SupplyInformationDeliveryProtocol> protocols);

    void Update(SupplyInformationDeliveryProtocol protocol);

    void Update(IEnumerable<SupplyInformationDeliveryProtocol> protocols);

    void UpdateSupplyInvoiceId(long fromInvoiceId, long toInvoiceId);

    SupplyInformationDeliveryProtocol GetById(long id);

    SupplyInformationDeliveryProtocol GetByNetId(Guid netId);

    SupplyInformationDeliveryProtocol GetDefaultProtocolBySupplyOrderNetId(Guid netId);

    void Remove(Guid netId);
}