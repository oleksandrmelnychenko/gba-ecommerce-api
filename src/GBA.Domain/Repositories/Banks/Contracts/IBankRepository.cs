using System.Collections.Generic;
using GBA.Domain.Entities;

namespace GBA.Domain.Repositories.Banks.Contracts;

public interface IBankRepository {
    long Add(Bank bank);

    IEnumerable<Bank> GetAll();

    void Update(Bank bank);
}