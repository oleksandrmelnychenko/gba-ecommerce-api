using System;
using System.Collections.Generic;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.UserRoles.Contracts;

public interface IUserRoleTranslationRepository {
    long Add(UserRoleTranslation userProfileRoleTranslation);

    void Update(UserRoleTranslation userProfileRoleTranslation);

    UserRoleTranslation GetById(long id);

    UserRoleTranslation GetByNetId(Guid netId);

    List<UserRoleTranslation> GetAll();

    void Remove(Guid netId);
}