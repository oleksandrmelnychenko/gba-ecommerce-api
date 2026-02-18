using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface ISadPalletRepository {
    long Add(SadPallet sadPallet);

    void Update(SadPallet sadPallet);

    void Remove(long id);

    List<SadPallet> GetAllBySadIdExceptProvided(long sadId, IEnumerable<long> ids);
}