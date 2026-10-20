using System;
using System.Collections.Generic;
using GBA.Domain.EntityHelpers.ProductAvailabilityModels;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IStockReservationReconciliationRepository {
    IEnumerable<StockReservationReconciliationRow> GetRows(IEnumerable<long> orderItemIds);

    IEnumerable<long> GetOrderItemIds(long orderId);

    void RecordMismatch(StockReservationMismatch mismatch, string source, Guid operationNetUid, Guid principalNetUid);
}
