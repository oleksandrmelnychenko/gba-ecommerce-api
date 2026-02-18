using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface IPlaneDeliveryServiceRepository {
    long Add(PlaneDeliveryService planeDeliveryService);

    void Update(PlaneDeliveryService planeDeliveryService);

    void Remove(Guid netId);

    PlaneDeliveryService GetById(long id);

    PlaneDeliveryService GetByNetId(Guid netId);

    List<PlaneDeliveryService> GetAll();

    List<PlaneDeliveryService> GetAllFromSearch(string value, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId);

    PlaneDeliveryService GetByIdWithoutIncludes(long id);
}