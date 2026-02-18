using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface ISadPalletTypeRepository {
    long Add(SadPalletType sadPalletType);

    void Update(SadPalletType sadPalletType);

    void Remove(Guid netId);

    SadPalletType GetById(long id);

    SadPalletType GetByNetId(Guid netId);

    IEnumerable<SadPalletType> GetAll();
}