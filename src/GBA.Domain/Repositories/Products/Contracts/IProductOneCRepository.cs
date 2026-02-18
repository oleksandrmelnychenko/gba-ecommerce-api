using System.Collections.Generic;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductOneCRepository {
    IEnumerable<long> GetMostPurchasedByClientRefId(string clientRefId);

    IEnumerable<long> GetMostPurchasedByRegionName(string regionName);

    IEnumerable<long> GetFromSearchBySales(string searchValue);
}