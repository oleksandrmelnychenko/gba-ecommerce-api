using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Agreements;

namespace GBA.Domain.Repositories.CalculationTypes.Contracts;

public interface ICalculationTypeRepository {
    long Add(CalculationType calculationType);

    void Update(CalculationType calculationType);

    CalculationType GetById(long id);

    CalculationType GetByNetId(Guid netId);

    List<CalculationType> GetAll();

    void Remove(Guid netId);
}