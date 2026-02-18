namespace GBA.Domain.Entities.Consignments;

public enum ConsignmentItemMovementType {
    Sale = 0,
    Return = 1,
    Shifting = 2,
    Income = 3,
    UkraineOrder = 4,
    DepreciatedOrder = 5,
    SupplyReturn = 6,
    ProductTransfer = 7,
    Export = 8,
    TaxFree = 9,
    CartItem = 10, //Virtual type
    Capitalization = 11,
    ShiftingStorage = 12,
    ECommerce = 13, //Virtual type
    Offer = 14, //Virtual type
    Order = 15
}