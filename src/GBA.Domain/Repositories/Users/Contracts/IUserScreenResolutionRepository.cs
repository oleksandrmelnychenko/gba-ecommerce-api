using System;
using GBA.Domain.Entities;

namespace GBA.Domain.Repositories.Users.Contracts;

public interface IUserScreenResolutionRepository {
    void Add(UserScreenResolution userScreenResolution);

    void Update(UserScreenResolution userScreenResolution);

    UserScreenResolution GetByUserNetId(Guid userNetId);
}