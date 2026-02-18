using System;
using System.Collections.Generic;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IPerfectClientTranslationRepository {
    long Add(PerfectClientTranslation perfectClientTranslation);

    void Add(IEnumerable<PerfectClientTranslation> perfectClientTranslations);

    void Update(PerfectClientTranslation perfectClientTranslation);

    void Update(IEnumerable<PerfectClientTranslation> perfectClientTranslations);

    PerfectClientTranslation GetById(long id);

    PerfectClientTranslation GetByNetId(Guid netId);

    PerfectClientTranslation GetByClientIdAndCultureCode(long id, string code);

    List<PerfectClientTranslation> GetAll();

    void Remove(Guid netId);
}