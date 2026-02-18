using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Repositories.Supplies.HelperServices.Contracts;

public interface ISupplyInvoiceBillOfLadingServiceRepository {
    void RemoveByBillOfLadingId(long id);

    List<SupplyInvoiceBillOfLadingService> GetByBillOfLadingServiceId(long id);

    void UpdateExtraValue(ICollection<SupplyInvoiceBillOfLadingService> invoices);

    void UnassignAllBillOfLadingServiceIdExceptProvided(long serviceId, IEnumerable<long> ids);

    void UpdateAssign(long serviceId, long id);

    long Add(SupplyInvoiceBillOfLadingService supplyInvoiceBillOfLadingService);

    void UpdateSupplyInvoiceId(long fromInvoiceId, long toInvoiceId);

    SupplyInvoiceBillOfLadingService GetById(long serviceId, long id);

    void ResetExtraValue(IEnumerable<long> ids, long id);

    List<SupplyInvoiceBillOfLadingService> GetBySupplyInvoiceId(long id);

    IEnumerable<long> GetSupplyInvoiceIdByBillOfLadingServiceId(long id);

    List<SupplyInvoiceBillOfLadingService> GetBySupplyInvoiceIds(IEnumerable<long> ids);
}