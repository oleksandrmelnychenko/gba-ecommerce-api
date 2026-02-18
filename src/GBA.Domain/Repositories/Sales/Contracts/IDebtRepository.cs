using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Repositories.Sales.Contracts;

public interface IDebtRepository {
    long Add(Debt debt);

    long AddWithCreatedDate(Debt debt);

    void Update(Debt debt);

    Debt GetById(long id);

    Debt GetByNetId(Guid netId);

    List<Debt> GetAll();

    List<dynamic> GetTopByAllClients();

    List<dynamic> GetTopByManagers();

    void Remove(Guid netId);
}