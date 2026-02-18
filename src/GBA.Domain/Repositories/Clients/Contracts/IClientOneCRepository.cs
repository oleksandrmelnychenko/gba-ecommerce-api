using System.Collections.Generic;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IClientOneCRepository {
    IEnumerable<long> GetOldEcommerceIdsFromSearchBySales(string value);
}