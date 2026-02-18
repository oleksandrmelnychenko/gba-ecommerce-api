using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Returns;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ISupplyReturnRepository {
    long Add(SupplyReturn supplyReturn);

    void Remove(long id);

    SupplyReturn GetLastRecord(string culture);

    SupplyReturn GetLastRecord(long organizationId);

    SupplyReturn GetById(long id);

    SupplyReturn GetByIdForConsignment(long id);

    SupplyReturn GetByNetId(Guid netId);

    List<SupplyReturn> GetAll();

    List<SupplyReturn> GetAllFiltered(DateTime from, DateTime to, long limit, long offset);

    SupplyReturn GetByNetIdForPrintingDocument(Guid netId);
}