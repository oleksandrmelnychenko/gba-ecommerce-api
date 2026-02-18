using System;
using System.Collections.Generic;
using GBA.Domain.EntityHelpers;

namespace GBA.Domain.Repositories.Clients.Contracts;

public interface IDebtorRepository {
    List<Debtor> GetAllFiltered(string value, bool allDebtors, Guid userNetId, long limit, long offset);
}