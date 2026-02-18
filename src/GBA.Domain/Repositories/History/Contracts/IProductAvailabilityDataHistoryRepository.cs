using System;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Repositories.History.Contracts;

public interface IProductAvailabilityDataHistoryRepository {
    long Add(ProductAvailabilityDataHistory productAvailabilityDataHistory);
    ProductAvailabilityDataHistory GetId(long Id);
    ProductAvailabilityDataHistory GetNetId(Guid NetId);
}