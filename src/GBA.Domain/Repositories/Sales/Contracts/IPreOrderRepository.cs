using System.Collections.Generic;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Repositories.Sales.Contracts;

public interface IPreOrderRepository {
    long Add(PreOrder preOrder);

    PreOrder GetById(long id);

    IEnumerable<PreOrder> GetAllByCurrentCultureFiltered(long limit, long offset);
}