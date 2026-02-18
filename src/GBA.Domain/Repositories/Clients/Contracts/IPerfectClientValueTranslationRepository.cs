using System;
using System.Collections.Generic;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IPerfectClientValueTranslationRepository {
    long Add(PerfectClientValueTranslation perfectClientValueTranslation);

    void Add(IEnumerable<PerfectClientValueTranslation> perfectClientValueTranslations);

    void Update(PerfectClientValueTranslation perfectClientValueTranslation);

    void Update(IEnumerable<PerfectClientValueTranslation> perfectClientValueTranslations);

    PerfectClientValueTranslation GetById(long id);

    PerfectClientValueTranslation GetByNetId(Guid netId);

    PerfectClientValueTranslation GetByValueIdAndCultureCode(long id, string code);

    List<PerfectClientValueTranslation> GetAll();

    void Remove(Guid netId);
}