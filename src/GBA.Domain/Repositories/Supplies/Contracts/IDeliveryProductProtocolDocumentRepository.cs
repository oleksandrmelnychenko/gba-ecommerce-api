using GBA.Domain.Entities.Supplies.Documents;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface IDeliveryProductProtocolDocumentRepository {
    void Add(DeliveryProductProtocolDocument document);
}