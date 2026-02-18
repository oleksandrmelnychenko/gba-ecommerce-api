using System;
using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ISupplyDeliveryDocumentRepository {
    long Add(SupplyDeliveryDocument document);

    void Update(SupplyDeliveryDocument document);

    SupplyDeliveryDocument GetByNetId(Guid netId);

    SupplyDeliveryDocument GetById(long id);

    void Remove(Guid netId);

    List<string> GetAllNamesGrouped();

    List<SupplyDeliveryDocument> GetAll();

    List<SupplyDeliveryDocument> GetAllByType(SupplyTransportationType type);

    SupplyDeliveryDocument GetForInvoiceByTransportationType(SupplyTransportationType type);
}