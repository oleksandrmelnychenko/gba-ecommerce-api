using System.Collections.Generic;
using GBA.Domain.Entities.Carriers;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface IStathamPassportRepository {
    long Add(StathamPassport stathamPassport);

    void Add(IEnumerable<StathamPassport> stathamPassports);

    void Update(StathamPassport stathamPassport);

    void Update(IEnumerable<StathamPassport> stathamPassports);

    void RemoveAllByStathamIdExceptProvided(long id, IEnumerable<long> ids);
}