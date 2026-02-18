using GBA.Domain.Entities.Supplies.Documents;

namespace GBA.Domain.Repositories.Supplies.Documents.Contracts;

public interface ISupplyServiceAccountDocumentRepository {
    long New(SupplyServiceAccountDocument document);

    void Update(SupplyServiceAccountDocument document);

    void RemoveById(long id);

    SupplyServiceAccountDocument GetLastRecord();
}