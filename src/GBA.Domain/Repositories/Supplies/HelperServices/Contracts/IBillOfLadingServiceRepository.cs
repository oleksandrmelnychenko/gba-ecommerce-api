using System;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Repositories.Supplies.HelperServices.Contracts;

public interface IBillOfLadingServiceRepository {
    long Add(BillOfLadingService billOfLadingService);

    void Update(BillOfLadingService billOfLadingService);

    void RemoveById(long id);

    BillOfLadingService GetByIdWithoutIncludes(long id);

    DeliveryProductProtocol GetDeliveryProductProtocolByNetId(Guid netId);

    BillOfLadingService GetWithoutIncludesByNetId(Guid netId);

    long GetDeliveryProductProtocolIdByNetId(Guid netUid);

    void UpdateIsCalculatedValueById(long id, bool isAuto);

    void UpdateSupplyExtraChargeTypeById(long id, SupplyExtraChargeType type);

    void UpdateIsShippedByDeliveryProductProtocolId(long id);

    void ResetIsCalculatedValueById(long id);
}