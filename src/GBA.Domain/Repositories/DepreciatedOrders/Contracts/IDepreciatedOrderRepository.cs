using System;
using System.Collections.Generic;
using GBA.Domain.Entities.DepreciatedOrders;

namespace GBA.Domain.Repositories.DepreciatedOrders.Contracts;

public interface IDepreciatedOrderRepository {
    long Add(DepreciatedOrder depreciatedOrder);

    void Update(DepreciatedOrder depreciatedOrder);

    DepreciatedOrder GetLastRecord(string culture);

    DepreciatedOrder GetById(long id);

    DepreciatedOrder GetByNetId(Guid netId);

    List<DepreciatedOrder> GetAll();

    List<DepreciatedOrder> GetAllFiltered(DateTime from, DateTime to, long limit, long offset);

    List<DepreciatedOrder> GetAllFiltered(DateTime from, DateTime to);

    DepreciatedOrder GetByNetIdForExportDocument(Guid netId);

    DepreciatedOrder GetByIdForConsignment(long id);
}