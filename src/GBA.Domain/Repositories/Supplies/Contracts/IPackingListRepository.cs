using System;
using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.EntityHelpers;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface IPackingListRepository {
    long Add(PackingList packingList);

    void Update(PackingList packingList);

    void UpdateSupplyInvoiceIdAndRootId(IEnumerable<PackingList> packingLists);

    void UpdateVats(PackingList packingList);

    void UpdateIsPlaced(PackingList packingList);

    void UpdateExtraCharge(IEnumerable<PackingList> packingLists);

    void AssignProvidedToContainerService(long containerServiceId, IEnumerable<long> ids);

    void UnassignAllByContainerServiceId(long containerServiceId);

    void UnassignAllByContainerServiceIdExceptProvided(long containerServiceId, IEnumerable<long> ids);

    void SetIsDocumentsAddedFalse();

    void Remove(Guid netId);

    void RemoveAllByInvoiceId(long invoiceId);

    void RemoveAllByInvoiceIdExceptProvided(long invoiceId, IEnumerable<long> ids);

    void SetPlaced(long id, bool value);

    PackingList GetByNetId(Guid netId);

    PackingList GetByNetIdWithInvoice(Guid netId);

    PackingList GetByNetIdWithContainerOrVehicleInfo(Guid netId);

    PackingList GetByNetIdForPlacement(Guid netId);

    PackingList GetByNetIdWithProductSpecification(
        Guid netId,
        decimal govExhangeRateUahToEur);

    PackingList GetByNetIdWithOrderInfo(Guid netId);

    PackingList GetById(long id);

    PackingList GetByIdForPlacement(long id);

    List<PackingList> GetAllAssignedToContainerByContainerNetId(Guid netId);

    List<PackingListForSpecification> GetByNetIdForSpecification(Guid netId);

    List<GroupedSpecificationByPackingList> GetGroupedSpecificationForDocumentByPackingListNetId(Guid netId);

    List<PackingList> GetAllUnshipped(SupplyTransportationType transportationType, string culture);

    void UnassignAllByVehicleServiceIdExceptProvided(long vehicleServiceId, IEnumerable<long> ids);

    void AssignProvidedToVehicleService(long vehicleServiceId, IEnumerable<long> ids);

    void UnassignAllByVehicleServiceId(long vehicleServiceId);

    List<PackingList> GetAllAssignedToVehicleByVehicleNetId(Guid netId);

    void UnassigningAllByContainerAndSupplyOrderId(long supplyOrderId, long containerServiceId);

    void UnassigningAllByVehicleAndSupplyOrderId(long supplyOrderId, long vehicleServiceId);

    void UnassigninAllByContainerServiceId(long containerServiceId);

    void UnassigninAllByVehicleServiceId(long vehicleServiceId);
}