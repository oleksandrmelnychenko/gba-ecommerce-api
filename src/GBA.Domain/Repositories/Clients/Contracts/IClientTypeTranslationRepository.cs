using System;
using System.Collections.Generic;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IClientTypeTranslationRepository {
    long Add(ClientTypeTranslation clientTypeTranslation);

    void Update(ClientTypeTranslation clientTypeTranslation);

    ClientTypeTranslation GetById(long id);

    ClientTypeTranslation GetByNetId(Guid netId);

    List<ClientTypeTranslation> GetAll();

    void Remove(Guid netId);
}