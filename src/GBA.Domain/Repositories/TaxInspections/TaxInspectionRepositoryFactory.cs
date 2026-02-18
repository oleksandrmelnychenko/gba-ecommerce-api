using System.Data;
using GBA.Domain.Repositories.TaxInspections.Contracts;

namespace GBA.Domain.Repositories.TaxInspections;

public sealed class TaxInspectionRepositoryFactory : ITaxInspectionRepositoryFactory {
    public ITaxInspectionRepository New(IDbConnection connection) {
        return new TaxInspectionRepository(connection);
    }
}