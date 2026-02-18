using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Documents;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ISupplyOrganizationDocumentRepository {
    void Add(IEnumerable<SupplyOrganizationDocument> documents);

    void Update(IEnumerable<SupplyOrganizationDocument> documents);

    void RemoveAllByIds(IEnumerable<long> ids);

    void RemoveAllBySupplyOrganizationAgreementId(long id);

    void RemoveAllBySupplyOrganizationAgreementIds(IEnumerable<long> ids);
}