using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Documents;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ISupplyInvoiceDeliveryDocumentRepository {
    SupplyInvoiceDeliveryDocument GetLastRecord();

    void Remove(IEnumerable<SupplyInvoiceDeliveryDocument> documents);

    void Add(SupplyInvoiceDeliveryDocument document);

    void UpdateSupplyInvoiceId(long fromInvoiceId, long toInvoiceId);

    void RemoveBySupplyInvoiceIdExceptProvided(long id, IEnumerable<long> ids);
}