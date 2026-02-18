using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Documents;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface IInvoiceDocumentRepository {
    void Add(IEnumerable<InvoiceDocument> invoiceDocuments);

    void Add(InvoiceDocument invoiceDocument);

    void Update(IEnumerable<InvoiceDocument> invoiceDocuments);

    void UpdateSupplyInvoiceId(long fromInvoiceId, long toInvoiceId);

    void RemoveAll(Guid supplyInvoiceNetId);

    void RemoveAllBySupplyInvoiceId(long invoiceId);

    void RemoveAllBySupplyInvoiceIdExceptProvided(long invoiceId, IEnumerable<long> notRemoveIds);

    void RemoveAllByCustomServiceId(long id);

    void RemoveAllByCustomServiceIdExceptProvided(long id, IEnumerable<long> notRemoveIds);

    void RemoveAllByPortWorkServiceId(long id);

    void RemoveAllByPortWorkServiceIdExceptProvided(long id, IEnumerable<long> notRemoveIds);

    void RemoveAllByPortCustomAgencyServiceId(long id);

    void RemoveAllByPortCustomAgencyServiceIdExceptProvided(long id, IEnumerable<long> notRemoveIds);

    void RemoveAllByTransportationServiceId(long id);

    void RemoveAllByTransportationServiceIdExceptProvided(long id, IEnumerable<long> notRemoveIds);

    void RemoveAllByCustomAgencyServiceId(long id);

    void RemoveAllByCustomAgencyServiceIdExceptProvided(long id, IEnumerable<long> notRemoveIds);

    void RemoveAllByContainerServiceId(long id);

    void RemoveAllByContainerServiceIdExceptProvided(long id, IEnumerable<long> notRemoveIds);

    void RemoveAllByPlaneDeliveryServiceId(long id);

    void RemoveAllByPlaneDeliveryServiceIdExceptProvided(long id, IEnumerable<long> notRemoveIds);

    void RemoveAllByVehicleDeliveryServiceId(long id);

    void RemoveAllByVehicleDeliveryServiceIdExceptProvided(long id, IEnumerable<long> notRemoveIds);

    void RemoveAllByPackingListId(long id);

    void RemoveAllByPackingListIdExceptProvided(long id, IEnumerable<long> notRemoveIds);

    void RemoveAllByMergedServiceId(long id);

    void RemoveAllByMergedServiceIdExceptProvided(long id, IEnumerable<long> notRemoveIds);

    void Remove(Guid netId);
    void Remove(IEnumerable<InvoiceDocument> invoiceDocuments);

    void RemoveAllByVehicleServiceIdExceptProvided(long vehicleServiceId, IEnumerable<long> select);

    void RemoveAllByVehicleServiceId(long vehicleServiceId);
}