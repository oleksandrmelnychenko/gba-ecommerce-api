using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine.Documents;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface ISadDocumentRepository {
    void Add(IEnumerable<SadDocument> documents);

    void Remove(Guid netId);
}