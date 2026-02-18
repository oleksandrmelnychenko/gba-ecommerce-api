using GBA.Domain.Entities.Sales.SaleShiftStatuses;

namespace GBA.Domain.Repositories.Sales.Contracts;

public interface ISaleBaseShiftStatusRepository {
    long Add(SaleBaseShiftStatus saleBaseShiftStatus);

    void Update(SaleBaseShiftStatus saleBaseShiftStatus);
}