using System;
using System.Collections.Generic;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.EntityHelpers.ReSaleModels;

namespace GBA.Domain.Repositories.ReSales.Contracts;

public interface IReSaleAvailabilityRepository {
    long Add(ReSaleAvailability item);

    void Update(ReSaleAvailability item);

    void Delete(long id);

    ReSaleAvailabilityWithTotalsModel GetAllItemsFiltered(
        decimal extraChargePercent,
        IEnumerable<long> includedProductGroups,
        IEnumerable<long> includedStorages,
        IEnumerable<string> includedSpecificationCodes,
        string search,
        Guid? selectStorageNetId = null);

    ReSaleAvailabilityWithTotalsModel GetAllItemsForExport(DateTime from, DateTime to);

    ReSaleAvailabilityWithTotalsModel GetActualReSaleAvailabilityByProductId(long productId);

    int? GetTotalProductQtyFromReSaleAvailabilitiesByProductId(long productId);

    void UpdateRemainingQty(ReSaleAvailability item);

    IEnumerable<ReSaleAvailability> GetAllForSignal();

    IEnumerable<string> GetAllReSaleAvailabilitySpecificationCodes();

    ReSaleAvailability GetById(long id);

    void RestoreReSaleAvailability(long id);

    ReSaleAvailability GetByProductReservationId(long productReservationId);

    IEnumerable<ReSaleAvailability> GetByProductAndStorageIds(
        long productId,
        long[] storageIds);

    IEnumerable<ReSaleAvailability> GetByProductAndStorageId(
        long productId,
        long storageId);

    IEnumerable<ReSaleAvailability> GetByConsignmentItemIds(long[] consignmentItemIds);

    IEnumerable<ReSaleAvailability> GetExistByProductId(long productId);
}