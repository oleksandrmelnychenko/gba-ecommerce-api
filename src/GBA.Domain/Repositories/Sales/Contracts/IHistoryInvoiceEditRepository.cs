using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Repositories.Sales.Contracts;

public interface IHistoryInvoiceEditRepository {
    long Add(HistoryInvoiceEdit historyInvoiceEdit);

    void UpdateApproveUpdateFalse(Guid netId);
    void UpdateApproveUpdate(Guid netId);

    void UpdateIsDevelopment(Guid netId);

    List<HistoryInvoiceEdit> GetByIdSale(long saleId);

    HistoryInvoiceEdit GetByNetId(Guid netId);
    HistoryInvoiceEdit GetById(long netId);
}