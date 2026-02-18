using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface ITaxFreePackListRepository {
    long Add(TaxFreePackList taxFreePackList);

    void Update(TaxFreePackList taxFreePackList);

    void Remove(Guid netId);

    TaxFreePackList GetLastRecord();

    TaxFreePackList GetById(long id);

    TaxFreePackList GetByIdForConsignment(long id);

    TaxFreePackList GetByIdForConsignmentMovement(long id);

    TaxFreePackList GetByIdForConsignmentMovementFromSale(long id);

    TaxFreePackList GetByNetId(Guid netId);

    TaxFreePackList GetByNetIdForConsignment(Guid netId);

    IEnumerable<TaxFreePackList> GetAllNotSent();

    IEnumerable<TaxFreePackList> GetAllNotSentFromSales();

    IEnumerable<TaxFreePackList> GetAllSent();

    IEnumerable<TaxFreePackList> GetAllFiltered(DateTime from, DateTime to, long limit, long offset);

    IEnumerable<TaxFreePackList> GetAllFilteredForPrintDocument(DateTime from, DateTime to);
}