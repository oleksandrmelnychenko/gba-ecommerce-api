using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Documents;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ISupplyPaymentTaskDocumentRepository {
    void Add(SupplyPaymentTaskDocument supplyPaymentTaskDocument);

    void Add(IEnumerable<SupplyPaymentTaskDocument> supplyPaymentTaskDocuments);

    List<SupplyPaymentTaskDocument> GetAllByTaskId(long id);

    void RemoveBySupplyPaymentTaskId(long id);

    void Remove(IEnumerable<SupplyPaymentTaskDocument> documents);
}