using System;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Repositories.History.Contracts;

public interface IProductPlacementDataHistoryRepository {
    long Add(ProductPlacementDataHistory productPlacementDataHistory);
    ProductPlacementDataHistory GetId(long Id);
    ProductPlacementDataHistory GetNetId(Guid NetId);
}