using System;
using System.Collections.Generic;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Transporters.Contracts;

public interface ITransporterTypeTranslationRepository {
    long Add(TransporterTypeTranslation transporterTypeTranslation);

    void Add(IEnumerable<TransporterTypeTranslation> transporterTypeTranslations);

    void Update(TransporterTypeTranslation transporterTypeTranslation);

    void Update(IEnumerable<TransporterTypeTranslation> transporterTypeTranslations);

    TransporterTypeTranslation GetById(long id);

    TransporterTypeTranslation GetByNetId(Guid netId);

    List<TransporterTypeTranslation> GetAll();

    void Remove(Guid netId);
}