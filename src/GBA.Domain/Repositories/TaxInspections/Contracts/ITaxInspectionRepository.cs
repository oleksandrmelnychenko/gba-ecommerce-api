using System;
using System.Collections.Generic;
using GBA.Domain.Entities;

namespace GBA.Domain.Repositories.TaxInspections.Contracts;

public interface ITaxInspectionRepository {
    long Add(TaxInspection taxInspection);

    void Update(TaxInspection taxInspection);

    void Remove(long id);

    void Remove(Guid netId);

    TaxInspection GetById(long id);

    TaxInspection GetByNetId(Guid netId);

    IEnumerable<TaxInspection> GetAll();
}