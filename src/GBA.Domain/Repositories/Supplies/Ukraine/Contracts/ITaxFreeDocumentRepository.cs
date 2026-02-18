using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine.Documents;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface ITaxFreeDocumentRepository {
    void Add(IEnumerable<TaxFreeDocument> documents);

    void Remove(Guid netId);
}