using System;
using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface IServiceDetailItemRepository {
    void Add(IEnumerable<ServiceDetailItem> serviceDetailItems);

    void Update(IEnumerable<ServiceDetailItem> serviceDetailItems);

    List<ServiceDetailItem> GetAllByNetIdAndType(Guid netId, SupplyServiceType type);

    void Remove(Guid netId);

    void RemoveAllByCustomAgencyServiceId(long id);

    void RemoveAllByCustomAgencyServiceIdExceptProvided(long id, IEnumerable<long> ids);

    void RemoveAllByCustomServiceId(long id);

    void RemoveAllByCustomServiceIdExceptProvided(long id, IEnumerable<long> ids);

    void RemoveAllByPlaneDeliveryServiceId(long id);

    void RemoveAllByPlaneDeliveryServiceIdExceptProvided(long id, IEnumerable<long> ids);

    void RemoveAllByPortCustomAgencyServiceId(long id);

    void RemoveAllByPortCustomAgencyServiceIdExceptProvided(long id, IEnumerable<long> ids);

    void RemoveAllByPortWorkServiceId(long id);

    void RemoveAllByPortWorkServiceIdExceptProvided(long id, IEnumerable<long> ids);

    void RemoveAllByTransportationServiceId(long id);

    void RemoveAllByTransportationServiceIdExceptProvided(long id, IEnumerable<long> ids);

    void RemoveAllByVehicleDeliveryServiceId(long id);

    void RemoveAllByVehicleDeliveryServiceIdExceptProvided(long id, IEnumerable<long> ids);

    void RemoveAllByMergedServiceId(long id);

    void RemoveAllByMergedServiceIdExceptProvided(long id, IEnumerable<long> ids);
}