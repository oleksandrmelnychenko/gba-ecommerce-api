using System;
using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Supplies.DeliveryProductProtocols;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface IMergedServiceRepository {
    long Add(MergedService mergedService);

    void Add(IEnumerable<MergedService> mergedServices);

    void Update(MergedService mergedService);

    void Remove(long id);

    List<MergedService> GetAllFromSearch(string value, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId);

    List<MergedService> GetAllForExport(DateTime from, DateTime to);

    MergedService GetByIdWithoutIncludes(long id);

    DeliveryProductProtocol GetDeliveryProductProtocolByNetId(Guid netId);

    void RemoveById(long id);

    MergedService GetWithoutIncludesByNetId(Guid netId);

    long GetDeliveryProductProtocolIdByNetId(Guid netId);

    void UpdateIsCalculatedValueById(long id, bool isAuto);

    void UpdateSupplyExtraChargeTypeById(long id, SupplyExtraChargeType type);

    void ResetIsCalculatedValueById(long id);
}