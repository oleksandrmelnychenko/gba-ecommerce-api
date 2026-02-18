using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Carriers;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface ICarrierStathamRepository {
    long Add(Statham statham);

    void Update(Statham statham);

    void RemoveByNetId(Guid netId);

    Statham GetById(long id);

    Statham GetByNetId(Guid netId);

    IEnumerable<Statham> GetAll();

    IEnumerable<Statham> GetAllFromSearch(string value);
}