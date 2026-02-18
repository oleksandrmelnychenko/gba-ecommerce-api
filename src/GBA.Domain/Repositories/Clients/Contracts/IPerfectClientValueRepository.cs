using System.Collections.Generic;
using GBA.Domain.Entities.Clients.PerfectClients;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IPerfectClientValueRepository {
    long Add(PerfectClientValue value);

    void Add(IEnumerable<PerfectClientValue> values);

    void Update(IEnumerable<PerfectClientValue> values);

    void Update(PerfectClientValue value);
}