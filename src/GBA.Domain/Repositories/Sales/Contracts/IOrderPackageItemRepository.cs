using System.Collections.Generic;
using GBA.Domain.Entities.Sales.OrderPackages;

namespace GBA.Domain.Repositories.Sales.Contracts;

public interface IOrderPackageItemRepository {
    void Add(IEnumerable<OrderPackageItem> orderPackageItems);

    void Update(IEnumerable<OrderPackageItem> orderPackageItems);

    void RemoveAllByOrderPackageId(long orderPackageId);

    void RemoveAllByOrderPackageIdExceptProvided(long orderPackageId, IEnumerable<long> ids);
}