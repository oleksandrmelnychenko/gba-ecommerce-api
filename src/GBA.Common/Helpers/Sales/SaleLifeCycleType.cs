namespace GBA.Common.Helpers;

public enum SaleLifeCycleType {
    New,
    Packaging,
    Packaged,
    Shipping,
    Received,
    Await,

    // Additional filter-only states
    OrderClosed = 100,
    TransporterChanged = 101,
    InvoiceChanged = 102
}