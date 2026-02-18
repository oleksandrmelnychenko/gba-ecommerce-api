using GBA.Domain.Entities.Supplies.Documents;

namespace GBA.Domain.Repositories.Supplies.Documents.Contracts;

public interface IActProvidingServiceDocumentRepository {
    long New(ActProvidingServiceDocument document);

    void Update(ActProvidingServiceDocument document);

    void RemoveById(long id);

    ActProvidingServiceDocument GetLastRecord();
}