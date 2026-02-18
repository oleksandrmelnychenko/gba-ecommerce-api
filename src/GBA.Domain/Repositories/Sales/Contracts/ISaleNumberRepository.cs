using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Repositories.Sales.Contracts;

public interface ISaleNumberRepository {
    long Add(SaleNumber saleNumber);

    void Update(SaleNumber saleNumber);

    SaleNumber GetById(long id);

    SaleNumber GetByNetId(Guid netId);

    List<SaleNumber> GetAll();

    SaleNumber GetLastRecordByOrganizationNetId(Guid organizationNetId);

    void Remove(Guid netId);
}