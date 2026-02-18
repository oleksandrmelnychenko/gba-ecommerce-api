using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Documents;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface IPackingListDocumentRepository {
    void Add(IEnumerable<PackingListDocument> packingListDocuments);

    IEnumerable<PackingListDocument> GetAllBySupplyOrderNetId(Guid supplyOrderNetId);
}