namespace GBA.Domain.Entities.SaleReturns;

public enum SaleReturnItemStatus {
    ProductArrivedNotAtTime,
    NotFullDelivery,
    IncorrectAssortment,
    IncorrectCrossCode,
    ProductAbandon,
    IncorrectQuality,
    Defect,
    ClientNotTookProduct,
    SupplierWithdrawal
}