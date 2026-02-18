using System.Collections.Generic;
using GBA.Domain.Entities.Sales.OrderPackages;

namespace GBA.Domain.Repositories.Sales.Contracts;

public interface IOrderPackageUserRepository {
    void Add(IEnumerable<OrderPackageUser> orderPackageUsers);

    void RemoveAllByOrderPackageId(long orderPackageId);

    void RemoveAllByOrderPackageIdExceptProvided(long orderPackageId, IEnumerable<long> ids);
}