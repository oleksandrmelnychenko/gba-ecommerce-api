using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface ISadRepository {
    long Add(Sad sad);

    void Update(Sad sad);

    void Delete(long id);

    Sad GetLastRecord();

    Sad GetById(long id);

    Sad GetByIdWithoutIncludes(long id);

    Sad GetByNetId(Guid netId);

    Sad GetByNetIdForConsignment(Guid netId);

    Sad GetByNetIdWithoutIncludes(Guid netId);

    Sad GetByNetIdWithProductSpecification(Guid netId);

    Sad GetForDocumentsExportByNetIdAndCulture(Guid netId, string culture);

    Sad GetForDocumentsExportByNetIdAndCultureWithProductSpecification(Guid netId, string culture);

    Sad GetByNetIdWithProductMovement(Guid netId);

    Sad GetByNetIdAndProductId(Guid netId, long productId);

    Sad GetByNetIdWithItems(Guid netId);

    Sad GetByNetIdWithProductSpecification(Guid netId, string specificationLocale);

    Sad GetByIdForConsignment(long id);

    Sad GetByIdForConsignmentFromSale(long id);

    IEnumerable<Sad> GetAllSent();

    IEnumerable<Sad> GetAllNotSent(SadType type);

    IEnumerable<Sad> GetAllNotSentFromSale(SadType type);

    IEnumerable<Sad> GetAllFiltered(DateTime from, DateTime to, long limit, long offset);
}