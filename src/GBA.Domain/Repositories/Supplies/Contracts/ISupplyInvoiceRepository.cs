using System;
using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.EntityHelpers.TotalDashboards.SupplyInvoices;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ISupplyInvoiceRepository {
    long Add(SupplyInvoice supplyInvoice);

    void Add(IEnumerable<SupplyInvoice> supplyInvoices);

    void Update(SupplyInvoice supplyInvoice);

    void Update(IEnumerable<SupplyInvoice> supplyInvoices);

    void UpdatePlacementInfo(IEnumerable<SupplyInvoice> supplyInvoices);

    void SetIsShipped(SupplyInvoice supplyInvoice);

    void Remove(Guid netId);

    void Merge(Guid netId, long rootId);

    long GetCountByContainerNetId(Guid containerNetId);

    SupplyInvoice GetById(long id);

    SupplyInvoice GetByIdWithoutIncludes(long id);

    SupplyInvoice GetByNetId(Guid netId);

    SupplyInvoice GetByNetIdAndProductIdWithSupplyOrderIncludes(Guid netId, long productId);

    SupplyInvoice GetByNetIdWithProducts(Guid netId);

    SupplyInvoice GetByNetIdWithoutIncludes(Guid netId);

    SupplyInvoice GetByNetIdWithAllIncludes(Guid netId);

    SupplyInvoice GetByNetIdForDocumentUpload(Guid netId);

    SupplyInvoice GetByNetIdAndCultureWithAllIncludes(Guid netId, string culture);

    SupplyInvoice GetByNetIdWithItemsAndSpecification(Guid netId);

    SupplyInvoice GetByNetIdWithItemsAndSpecificationForExport(Guid netId);

    List<SupplyInvoice> GetAllByContainerNetId(Guid containerNetId);

    List<SupplyInvoice> GetAllBySupplyOrderIdWithPackingLists(long supplyOrderId);

    List<SupplyInvoice> GetAllIncomedInvoicesFiltered(DateTime from, DateTime to);

    List<SupplyInvoice> GetAllIncomeInvoicesFiltered(DateTime from, DateTime to);

    List<SupplyInvoice> GetAllFromSearch(string value, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId);

    List<SupplyInvoice> GetAllIncomedInvoicesFiltered(
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

    IEnumerable<SupplyInvoice> GetAllInvoicesFromApprovedSupplyOrder(SupplyTransportationType transportationType, string culture, long protocol);

    void RemoveAllSupplyInvoiceFromDeliveryProductProtocolById(long id);

    void UnassignAllByDeliveryProductProtocolIdExceptProvided(long protocolId, IEnumerable<long> ids);

    void AssignProvidedToDeliveryProductProtocol(long protocolId, IEnumerable<long> ids);

    List<SupplyInvoice> GetByBillOfLadingServiceId(long id, long protocolId);

    List<SupplyInvoice> GetByMergedServiceId(long id, long protocolId);

    List<SupplyInvoice> GetByIds(IEnumerable<long> ids);

    IEnumerable<long> GetIdBySupplyOrderId(long id);

    IEnumerable<long> GetIdByContainerServiceId(long id);

    IEnumerable<long> GetIdByVehicleServiceId(long id);

    void UpdateIsShippedByDeliveryProductProtocolId(long id);

    TotalInvoicesItem GetTotalQtyNotArrivedInvoices();

    List<OrderedInvoiceModel> GetOrderedInvoicesByIsShipped(TypeIsShippedInvoices type);

    SupplyInvoice GetAllSpendingOnServicesByNetId(Guid netId);

    List<SupplyInvoice> GetWithConsignmentsByIds(IEnumerable<long> ids);

    void UpdateCustomDeclarationData(SupplyInvoice invoice);

    List<SupplyInvoice> GetBySupplyOrderId(long id);

    void RestoreSupplyInvoice(long id);

    SupplyInvoice GetSupplyInvoiceByPackingListNetId(Guid netId);

    IEnumerable<SupplyInvoice> GetAllByDeliveryProductProtocolId(long deliveryProductProtocolId, IEnumerable<Guid> invoiceNetIds);
}