using System;
using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface IContainerServiceRepository {
    long Add(ContainerService containerService);

    void Update(ContainerService containerService);

    void UpdateDeliveryTerms(long id, string days);

    void SetIsExtraChargeCalculatedByNetId(Guid netId, SupplyExtraChargeType extraChargeType);

    ContainerService GetById(long id);

    ContainerService GetByNetId(Guid netId);

    ContainerService GetBySupplyOrderContainerServiceNetIdWithoutIncludes(Guid netId);

    List<ContainerService> GetAllAvailable();

    List<ContainerService> GetAllFromSearch(string value, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId);

    string GetTermDeliveryInDaysById(long id);

    void Add(IEnumerable<ContainerService> containerServices);

    void Update(IEnumerable<ContainerService> containerServices);

    void UpdateSupplyPaymentTaskId(IEnumerable<ContainerService> containerServices);

    Guid GetSupplyOrderNetIdBySupplyOrderContainerServiceNetId(Guid netId);

    List<ContainerService> GetAllRanged(DateTime from, DateTime to);

    ContainerService GetByIdWithoutIncludes(long containerServiceId);

    bool IsContainerInOrder(Guid supplyOrderNetId, Guid containerServiceNetId);

    long GetIdByNetId(Guid containerServiceNetId);

    void Remove(long containerServiceId);
}