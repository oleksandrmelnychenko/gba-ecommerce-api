using System;
using System.Collections.Generic;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.EntityHelpers.Supplies.SupplyOrderModels;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ISupplyOrderRepository {
    long Add(SupplyOrder supplyOrder);

    void Update(SupplyOrder supplyOrder);

    void UpdateAdditionalPaymentFields(SupplyOrder supplyOrder);

    void Remove(Guid netId);

    void SetPartiallyPlaced(long id, bool value);

    void SetFullyPlaced(long id, bool value);

    Guid GetNetIdById(long id);

    SupplyOrder GetById(long id);

    SupplyOrder GetByIdWithAllIncludes(long id);

    SupplyOrder GetByIdWithoutIncludes(long id);

    SupplyOrder GetByIdIfExist(long id);

    SupplyOrder GetByPackingListId(long id);

    SupplyOrder GetByNetId(Guid netId);

    Currency GetCurrencyByOrderNetId(Guid netId);

    SupplyOrder GetByNetIdWithOrganization(Guid netId);

    SupplyOrder GetByNetIdIfExist(Guid netId);

    SupplyOrder GetByNetIdForPlacement(Guid netId);

    SupplyOrder GetByNetIdForDocumentUpload(Guid netId);

    List<SupplyOrder> GetAll();

    List<SupplyOrder> GetAll(DateTime from, DateTime to);

    List<SupplyOrder> GetAllForPlacement();

    List<SupplyOrder> GetAllFromSearch(string value, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId);

    List<SupplyOrder> GetAllFromSearchForUkOrganizations(
        string value,
        long limit,
        long offset,
        DateTime from,
        DateTime to,
        string supplierName,
        long? currencyId,
        Guid? clientNetId);

    List<SupplyOrder> GetAllFromSearchByOrderNumber(string value, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId);

    List<SupplyOrder> GetAllFromSearchByProduct(string value, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId);

    dynamic GetTotalsByNetId(Guid netId);

    dynamic GetTotalsOnSupplyOrderItemsBySupplyOrderNetId(Guid netId);

    dynamic GetNearestSupplyArrivalByProductNetId(Guid netId);

    long GetIdByNetId(Guid supplyOrderNetId);

    double GetQtySupplyInvoiceById(long id);

    List<SupplyOrderModel> GetAllForPrint(DateTime from, DateTime to);

    Currency GetCurrencyByInvoiceId(long invoiceId);

    SupplyInvoice GetInvoiceByPackingListPackageOrderItemId(long id);

    Currency GetCurrencyByInvoice(long invoiceId);
}