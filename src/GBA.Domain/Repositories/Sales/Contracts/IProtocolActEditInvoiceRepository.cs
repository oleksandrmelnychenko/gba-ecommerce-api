using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.Shipments;
using GBA.Domain.EntityHelpers.SalesModels.Models;

namespace GBA.Domain.Repositories.Sales.Contracts;

public interface IProtocolActEditInvoiceRepository {
    double GetSalesHistoryQtyModel();
    double GetEditTransporterQtyModel();
    double GetEditActForEditingQtyModel();

    List<SalesHistoryModel> GetSalesHistoryModel(
        DateTime from,
        DateTime to,
        long limit,
        long offset,
        bool isDevelopment);

    List<UpdateDataCarrier> GetEditTransporterModel(
        DateTime from,
        DateTime to,
        long limit,
        long offset,
        bool isDevelopment);

    List<HistoryInvoiceEdit> GetEditActForEditingModel(
        DateTime from,
        DateTime to,
        long limit,
        long offset,
        bool isDevelopment);
}