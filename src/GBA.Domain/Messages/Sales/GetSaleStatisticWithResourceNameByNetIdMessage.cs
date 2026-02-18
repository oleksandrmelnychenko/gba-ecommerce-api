using System;

namespace GBA.Domain.Messages.Sales;

public sealed class GetSaleStatisticWithResourceNameByNetIdMessage {
    public GetSaleStatisticWithResourceNameByNetIdMessage(
        Guid saleNetId,
        string saleResourceName,
        bool forceCalculatePrices = false,
        bool pushCreatedNotification = false,
        bool pushUpdatedNotification = false) {
        SaleNetId = saleNetId;

        SaleResourceName = saleResourceName;

        ForceCalculatePrices = forceCalculatePrices;

        PushCreatedNotification = pushCreatedNotification;

        PushUpdatedNotification = pushUpdatedNotification;
    }

    public Guid SaleNetId { get; }

    public string SaleResourceName { get; }

    public bool ForceCalculatePrices { get; }

    public bool PushCreatedNotification { get; }

    public bool PushUpdatedNotification { get; }
}