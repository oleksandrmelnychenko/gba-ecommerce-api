using System.Collections.Generic;
using GBA.Domain.Entities.Carriers;

namespace GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

public interface IStathamCarRepository {
    void Add(IEnumerable<StathamCar> cars);

    void Update(IEnumerable<StathamCar> cars);

    void RemoveAllByIds(IEnumerable<long> ids);

    void RemoveAllByStathamIdExceptProvided(long id, IEnumerable<long> ids);
}