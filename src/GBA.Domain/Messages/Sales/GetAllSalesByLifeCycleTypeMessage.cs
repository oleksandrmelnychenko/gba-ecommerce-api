using GBA.Common.Helpers;

namespace GBA.Domain.Messages.Sales;

public sealed class GetAllSalesByLifeCycleTypeMessage {
    public GetAllSalesByLifeCycleTypeMessage(SaleLifeCycleType saleLifeCycleTypeId) {
        SaleLifeCycleType = saleLifeCycleTypeId;
    }

    public SaleLifeCycleType SaleLifeCycleType { get; set; }
}