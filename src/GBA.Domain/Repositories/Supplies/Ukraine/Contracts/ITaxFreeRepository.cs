using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface ITaxFreeRepository {
    long Add(TaxFree taxFree);

    void Update(TaxFree taxFree);

    void Remove(TaxFree taxFree);

    TaxFree GetLastRecord();

    TaxFree GetById(long id);

    TaxFree GetByNetId(Guid netId);

    List<TaxFree> GetByNetIds(IEnumerable<Guid> netIds);

    TaxFree GetByNetIdWithPackList(Guid netId);

    TaxFree GetByNetIdForPrinting(Guid netId);

    TaxFree GetByNetIdFromSaleForPrinting(Guid netId);

    List<TaxFree> GetByNetIdsWithPackList(IEnumerable<Guid> netIds);

    List<TaxFree> GetAllByPackListIdExceptProvided(long packListId, IEnumerable<long> ids);

    IEnumerable<TaxFree> GetAllFiltered(
        DateTime from,
        DateTime to,
        long limit,
        long offset,
        string value,
        TaxFreeStatus? status,
        Guid? stathamNetId
    );
}