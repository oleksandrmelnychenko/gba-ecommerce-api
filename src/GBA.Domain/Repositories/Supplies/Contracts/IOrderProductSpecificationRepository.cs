using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface IOrderProductSpecificationRepository {
    void Add(OrderProductSpecification specification);
}