using System;
using System.Collections.Generic;
using GBA.Domain.Entities;

namespace GBA.Domain.Repositories.UserRoles.Contracts;

public interface IUserRoleRepository {
    long Add(UserRole userRole);

    void Update(UserRole userRole);

    UserRole GetById(long id);

    UserRole GetByNetId(Guid netId);

    UserRole GetByNetIdWithoutTranslation(Guid netId);

    List<UserRole> GetAll();

    void Remove(Guid netId);
}