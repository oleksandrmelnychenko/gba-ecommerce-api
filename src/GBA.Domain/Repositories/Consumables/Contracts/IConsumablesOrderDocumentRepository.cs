using System.Collections.Generic;
using GBA.Domain.Entities.Consumables.Orders;

namespace GBA.Domain.Repositories.Consumables.Contracts;

public interface IConsumablesOrderDocumentRepository {
    void Add(IEnumerable<ConsumablesOrderDocument> consumablesOrderDocuments);

    void Update(IEnumerable<ConsumablesOrderDocument> consumablesOrderDocuments);

    void Remove(IEnumerable<long> ids);
}