using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ISupplyServiceNumberRepository {
    void Add(string number, bool isPoland = true);

    SupplyServiceNumber GetLastRecord(bool isPoland = true);
}