using System;
using System.Collections.Generic;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Measures.Contracts;

public interface IMeasureUnitTranslationRepository {
    long Add(MeasureUnitTranslation measureUnitTranslation);

    void Update(MeasureUnitTranslation measureUnitTranslation);

    MeasureUnitTranslation GetById(long id);

    MeasureUnitTranslation GetByNetId(Guid netId);

    List<MeasureUnitTranslation> GetAll();

    void Remove(Guid netId);
}