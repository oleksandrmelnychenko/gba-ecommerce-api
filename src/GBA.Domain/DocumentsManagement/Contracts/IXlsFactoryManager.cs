namespace GBA.Domain.DocumentsManagement.Contracts;

public interface IXlsFactoryManager {
    IAccountingXlsManager NewAccountingXlsManager();

    IClientXlsManager NewClientXlsManager();

    IConsignmentXlsManager NewConsignmentXlsManager();

    IInvoiceXlsManager NewInvoiceXlsManager();

    IOrderXlsManager NewOrderXlsManager();

    IParseConfigurationXlsManager NewParseConfigurationXlsManager();

    IProductsXlsManager NewProductsXlsManager();

    IReSaleXlsManager NewReSaleXlsManager();

    ISaleReturnXlsManager NewSaleReturnXlsManager();

    ISalesXlsManager NewSalesXlsManager();

    ISynchronizationXlsManager NewSynchronizationXlsManager();

    ISalesShipmentListManager NewSalesShipmentListManager();

    IProductPlacementStorageManager NewProductPlacementStorageManagerManager();

    ITaxFreeAndSadXlsManager NewTaxFreeAndSadXlsManager();

    IPrintDocumentsManager NewPrintDocumentsManager();

    IConsignmentNoteDocumentManager NewConsignmentNoteDocumentManager();

    IAgreementDocManager NewAgreementXlsManager();
}