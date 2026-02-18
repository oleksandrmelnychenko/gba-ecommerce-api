using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Documents;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface IBillOfLadingDocumentRepository {
    long Add(BillOfLadingDocument billOfLadingDocument);

    void Add(IEnumerable<BillOfLadingDocument> billOfLadingDocuments);

    void Remove(IEnumerable<BillOfLadingDocument> billOfLadingDocuments);

    void Update(BillOfLadingDocument billOfLadingDocument);

    void Remove(Guid netId);
}