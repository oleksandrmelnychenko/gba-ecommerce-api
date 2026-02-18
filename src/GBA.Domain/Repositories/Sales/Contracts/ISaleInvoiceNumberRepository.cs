using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Repositories.Sales.Contracts;

public interface ISaleInvoiceNumberRepository {
    long Add(SaleInvoiceNumber number);

    SaleInvoiceNumber GetLastRecord();
}