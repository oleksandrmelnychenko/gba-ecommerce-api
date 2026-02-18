using System;
using System.Collections.Generic;
using GBA.Domain.EntityHelpers.DataSync;

namespace GBA.Domain.Repositories.DataSync.Contracts;

public interface IDocumentsAfterSyncRepository {
    List<GenericDocument> GetMappedDocumentsFiltered(
        DateTime from,
        DateTime to,
        int limit,
        int offset,
        string name,
        ContractorType contractorType);
}