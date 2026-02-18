using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Repositories.Supplies.HelperServices.Contracts;

public interface ISupplyInvoiceMergedServiceRepository {
    void RemoveByMergedServiceId(long id);

    List<SupplyInvoiceMergedService> GetByMergedServiceId(long id);

    void UpdateExtraValue(ICollection<SupplyInvoiceMergedService> invoices);

    void UpdateAssign(long serviceId, long id);

    void Add(SupplyInvoiceMergedService supplyInvoiceMergedService);

    void UpdateSupplyInvoiceId(long fromInvoiceId, long toInvoiceId);

    void UnassignAllMergedServiceIdExceptProvided(long serviceId, IEnumerable<long> ids);

    SupplyInvoiceMergedService GetById(long serviceId, long id);

    void ResetExtraValue(IEnumerable<long> ids, long serviceId);

    List<SupplyInvoiceMergedService> GetBySupplyInvoiceId(long id);

    IEnumerable<long> GetSupplyInvoiceIdByMergedServiceId(long id);

    List<SupplyInvoiceMergedService> GetBySupplyInvoiceIds(IEnumerable<long> ids);
}