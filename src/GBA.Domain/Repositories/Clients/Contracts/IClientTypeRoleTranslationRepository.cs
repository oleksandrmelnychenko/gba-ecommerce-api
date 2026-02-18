using System;
using System.Collections.Generic;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IClientTypeRoleTranslationRepository {
    long Add(ClientTypeRoleTranslation clientTypeRoleTranslation);

    void Update(ClientTypeRoleTranslation clientTypeRoleTranslation);

    ClientTypeRoleTranslation GetById(long id);

    ClientTypeRoleTranslation GetByNetId(Guid netId);

    List<ClientTypeRoleTranslation> GetAll();

    void Remove(Guid netId);
}