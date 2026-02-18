using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Documents;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface IProFormDocumentRepository {
    void Add(IEnumerable<ProFormDocument> proFormDocuments);

    void Update(IEnumerable<ProFormDocument> proFormDocuments);

    void RemoveAll(Guid supplyProFormNetId);

    void Remove(Guid netId);

    void RemoveAllByProFormId(long id);

    void RemoveAllByProFormIdExceptProvided(long id, IEnumerable<long> notRemoveIds);
}