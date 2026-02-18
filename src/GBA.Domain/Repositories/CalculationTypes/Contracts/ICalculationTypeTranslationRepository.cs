using System;
using System.Collections.Generic;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.CalculationTypes.Contracts;

public interface ICalculationTypeTranslationRepository {
    long Add(CalculationTypeTranslation calculationTypeTranslation);

    void Update(CalculationTypeTranslation calculationTypeTranslation);

    CalculationTypeTranslation GetById(long id);

    CalculationTypeTranslation GetByNetId(Guid netId);

    List<CalculationTypeTranslation> GetAll();

    void Remove(Guid netId);
}