using GBA.Domain.DocumentsManagement.Contracts;

namespace GBA.Domain.DocumentsManagement;

public sealed class XlsFactoryManager : IXlsFactoryManager {
    public IAccountingXlsManager NewAccountingXlsManager() {
        return new AccountingXlsManager();
    }

    public IClientXlsManager NewClientXlsManager() {
        return new ClientXlsManager();
    }

    public IConsignmentXlsManager NewConsignmentXlsManager() {
        return new ConsignmentXlsManager();
    }

    public IInvoiceXlsManager NewInvoiceXlsManager() {
        return new InvoiceXlsManager();
    }

    public IOrderXlsManager NewOrderXlsManager() {
        return new OrderXlsManager();
    }

    public IParseConfigurationXlsManager NewParseConfigurationXlsManager() {
        return new ParseConfigurationXlsManager();
    }

    public IProductsXlsManager NewProductsXlsManager() {
        return new ProductsXlsManager();
    }

    public IReSaleXlsManager NewReSaleXlsManager() {
        return new ReSaleXlsManager();
    }

    public ISaleReturnXlsManager NewSaleReturnXlsManager() {
        return new SaleReturnXlsManager();
    }

    public ISynchronizationXlsManager NewSynchronizationXlsManager() {
        return new SynchronizationXlsManager();
    }

    public ISalesXlsManager NewSalesXlsManager() {
        return new SalesXlsManager();
    }

    public ISalesShipmentListManager NewSalesShipmentListManager() {
        return new SalesShipmentListManager();
    }

    public ITaxFreeAndSadXlsManager NewTaxFreeAndSadXlsManager() {
        return new TaxFreeAndSadXlsManager();
    }

    public IPrintDocumentsManager NewPrintDocumentsManager() {
        return new PrintDocumentsManager();
    }

    public IConsignmentNoteDocumentManager NewConsignmentNoteDocumentManager() {
        return new ConsignmentNoteDocumentManager();
    }

    public IAgreementDocManager NewAgreementXlsManager() {
        return new AgreementDocManager();
    }

    public IProductPlacementStorageManager NewProductPlacementStorageManagerManager() {
        return new ProductPlacementStorageManager();
    }
}