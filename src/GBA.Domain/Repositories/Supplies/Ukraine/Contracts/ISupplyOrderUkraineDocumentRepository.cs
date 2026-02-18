using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Documents;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface ISupplyOrderUkraineDocumentRepository {
    void New(IEnumerable<SupplyOrderUkraineDocument> docs);

    void Remove(IEnumerable<SupplyOrderUkraineDocument> docs);
}