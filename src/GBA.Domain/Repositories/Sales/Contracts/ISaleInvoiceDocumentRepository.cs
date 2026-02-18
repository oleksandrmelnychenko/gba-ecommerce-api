using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Repositories.Sales.Contracts;

public interface ISaleInvoiceDocumentRepository {
    long Add(SaleInvoiceDocument saleInvoiceDocument);

    void Update(SaleInvoiceDocument saleInvoiceDocument);

    void UpdateShippingAmount(SaleInvoiceDocument saleInvoiceDocument);

    void UpdateExchangeRateAmount(SaleInvoiceDocument saleInvoiceDocument);

    void UpdateExchangeRateAmountAndAmounts(SaleInvoiceDocument saleInvoiceDocument);
}