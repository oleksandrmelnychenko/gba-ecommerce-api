using System.Collections.Generic;
using GBA.Domain.Entities.Sales.OrderPackages;

namespace GBA.Domain.Repositories.Sales.Contracts;

public interface IOrderPackageRepository {
    long Add(OrderPackage orderPackage);

    void Add(IEnumerable<OrderPackage> orderPackages);

    void Update(OrderPackage orderPackage);

    void Update(IEnumerable<OrderPackage> orderPackages);

    OrderPackage GetById(long id);

    void RemoveAllByOrderId(long orderId);

    void RemoveAllByOrderIdExceptProvided(long orderId, IEnumerable<long> ids);
}