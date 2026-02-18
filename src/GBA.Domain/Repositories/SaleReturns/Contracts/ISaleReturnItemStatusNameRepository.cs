using System;
using System.Collections.Generic;
using GBA.Domain.Entities.SaleReturns;

namespace GBA.Domain.Repositories.SaleReturns.Contracts;

public interface ISaleReturnItemStatusNameRepository {
    List<SaleReturnItemStatusName> GetAll();

    Dictionary<SaleReturnItemStatus, double> GetSaleReturnQuantityGroupByReason(DateTime from, DateTime to, bool forMyClient, long userId, Guid? clientNetId);
}