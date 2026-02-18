using System.Data;

namespace GBA.Domain.Repositories.TaxInspections.Contracts;

public interface ITaxInspectionRepositoryFactory {
    ITaxInspectionRepository New(IDbConnection connection);
}