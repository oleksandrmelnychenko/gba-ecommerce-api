using System;
using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;
using GBA.Domain.EntityHelpers.Supplies.DeliveryProductProtocolModels;

namespace GBA.Domain.Repositories.Supplies.DeliveryProductProtocols.Contracts;

public interface IDeliveryProductProtocolRepository {
    long AddNew(DeliveryProductProtocol deliveryProductProtocol);

    DeliveryProductProtocol GetById(long deliveryProductProtocolId);

    DeliveryProductProtocol GetByNetId(Guid netId);

    long GetIdByNetId(Guid netId);

    GetAllFilteredWithTotalsModel AllFiltered(DateTime from, DateTime to, string organizationName, string supplierName, int limit, int offset);

    List<DeliveryProductProtocol> AllFiltered(DateTime from, DateTime to);

    void RemoveById(long id);

    SupplyTransportationType GetTransportationTypeById(long id);

    void SetFullyAndPartialPlacedPlaced(DeliveryProductProtocol protocol);

    Guid GetNetIdById(long id);

    void UpdateIsShippedByNetId(Guid netId);

    void UpdateIsCompletedByNetId(Guid netId);

    DeliveryProductProtocol GetWithoutIncludesByNetId(Guid netId);

    object GetAllFilteredForPrinting(DateTime from, DateTime to);
}