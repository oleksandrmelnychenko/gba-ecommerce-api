using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;
using GBA.Domain.EntityHelpers.Supplies.SupplyOrderModels;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface ISupplyOrderUkraineRepository {
    long Add(SupplyOrderUkraine supplyOrderUkraine);

    void UpdateAdditionalPaymentFields(SupplyOrderUkraine supplyOrder);

    void UpdateIsPlaced(SupplyOrderUkraine supplyOrderUkraine);

    void UpdateShipmentAmount(SupplyOrderUkraine supplyOrderUkraine);

    void UpdateOrganization(SupplyOrderUkraine supplyOrderUkraine);

    void Remove(long id);

    SupplyOrderUkraine GetLastRecord();

    SupplyOrderUkraine GetById(long id);

    SupplyOrderUkraine GetByNetId(Guid netId);

    List<SupplyOrderUkraine> GetAll();

    List<SupplyOrderUkraine> GetAllFiltered(DateTime from, DateTime to, string supplierName, long? id, long limit, long offset, bool nonPlaced);

    List<SupplyOrderUkraine> GetAllIncomeOrdersFiltered(
        DateTime from,
        DateTime to,
        Guid storageNetId,
        Guid? supplierNetId,
        string value,
        long limit,
        long offset
    );

    decimal GetTotalEuroAmountForPlacedItemsFiltered(
        Guid storageNetId,
        Guid? supplierNetId,
        string value,
        DateTime from,
        DateTime to
    );

    void UpdateVatPercent(long id, decimal vatPercent);

    void UpdateIsPartialPlaced(SupplyOrderUkraine fromDb);

    List<SupplyOrderModel> GetAllForPrintDocument(DateTime from, DateTime to);
}