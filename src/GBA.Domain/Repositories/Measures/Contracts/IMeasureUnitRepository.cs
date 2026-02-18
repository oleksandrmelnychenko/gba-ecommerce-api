using System;
using System.Collections.Generic;
using GBA.Domain.Entities;

namespace GBA.Domain.Repositories.Measures.Contracts;

public interface IMeasureUnitRepository {
    long Add(MeasureUnit measureUnit);

    void Update(MeasureUnit measureUnit);

    MeasureUnit GetById(long id);

    MeasureUnit GetByNetId(Guid netId);

    MeasureUnit GetByName(string name);

    List<MeasureUnit> GetAll();

    IEnumerable<MeasureUnit> GetAllFromSearch(string value);

    void Remove(Guid netId);
}