using System;
using GBA.Domain.Entities.Supplies.Documents;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ICreditNoteDocumentRepository {
    long Add(CreditNoteDocument creditNoteDocument);

    void Update(CreditNoteDocument creditNoteDocument);

    CreditNoteDocument GetByNetId(Guid netId);
}