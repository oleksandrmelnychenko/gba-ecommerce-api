using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ISupplyOrderNumberRepository {
    long Add(SupplyOrderNumber supplyOrderNumber);

    void Update(SupplyOrderNumber supplyOrderNumber);

    void Remove(SupplyOrderNumber supplyOrderNumber);

    SupplyOrderNumber GetLastRecord();
}