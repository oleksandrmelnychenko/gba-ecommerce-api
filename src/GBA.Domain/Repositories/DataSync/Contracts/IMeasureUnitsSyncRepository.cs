using System.Collections.Generic;
using GBA.Domain.Entities;
using GBA.Domain.EntityHelpers.DataSync;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.DataSync.Contracts;

public interface IMeasureUnitsSyncRepository {
    IEnumerable<SyncMeasureUnit> GetAllSyncMeasureUnits();

    IEnumerable<SyncMeasureUnit> GetAmgAllSyncMeasureUnits();

    IEnumerable<MeasureUnit> GetAllMeasureUnit();

    long Add(MeasureUnit measureUnit);

    void Add(MeasureUnitTranslation translation);

    void Update(MeasureUnit measureUnit);

    void Update(MeasureUnitTranslation translation);
}