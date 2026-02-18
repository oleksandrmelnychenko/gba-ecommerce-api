using System.Collections.Generic;
using GBA.Domain.Entities.Consumables;

namespace GBA.Domain.Repositories.Consumables.Contracts;

public interface ICompanyCarFuelingRepository {
    long Add(CompanyCarFueling companyCarFueling);

    void Add(IEnumerable<CompanyCarFueling> companyCarFuelings);

    void Update(CompanyCarFueling companyCarFueling);

    void Update(IEnumerable<CompanyCarFueling> companyCarFuelings);

    void RemoveAllByIds(IEnumerable<long> ids);

    CompanyCarFueling GetById(long id);
}