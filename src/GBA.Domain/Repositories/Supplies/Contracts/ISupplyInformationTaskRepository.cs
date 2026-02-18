using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ISupplyInformationTaskRepository {
    long? Add(SupplyInformationTask supplyInformationTask);

    void Update(SupplyInformationTask supplyInformationTask);

    void Remove(SupplyInformationTask supplyInformationTask);
}