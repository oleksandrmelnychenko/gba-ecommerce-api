using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Clients.Documents;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IClientContractDocumentRepository {
    void Add(IEnumerable<ClientContractDocument> documents);

    void Update(IEnumerable<ClientContractDocument> documents);

    void Remove(Guid netId);

    void Remove(IEnumerable<long> ids);
}