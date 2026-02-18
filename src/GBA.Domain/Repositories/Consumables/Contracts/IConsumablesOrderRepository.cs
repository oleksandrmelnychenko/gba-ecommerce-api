using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Consumables;

namespace GBA.Domain.Repositories.Consumables.Contracts;

public interface IConsumablesOrderRepository {
    long Add(ConsumablesOrder consumablesOrder);

    void Update(ConsumablesOrder consumablesOrder);

    void Remove(Guid netId);

    decimal GetUnpaidAmountByOrderId(long id);

    decimal GetPaidAmountByOrderId(long id);

    ConsumablesOrder GetLastRecord();

    ConsumablesOrder GetById(long id);

    ConsumablesOrder GetByNetId(Guid netId);

    List<ConsumablesOrder> GetAll(DateTime from, DateTime to);

    List<ConsumablesOrder> GetAllServices(DateTime from, DateTime to, string value, Guid? organizationNetId);

    List<ConsumablesOrder> GetAllUnpaidByConsumableOrganizationNetId(Guid organizationNetId);

    List<ConsumablesOrder> GetAllFromSearch(string value);
}