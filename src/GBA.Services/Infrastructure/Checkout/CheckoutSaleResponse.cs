using GBA.Domain.Entities.Sales;

namespace GBA.Services.Infrastructure.Checkout;

public static class CheckoutSaleResponse {
    public static Sale From(Sale sale) {
        if (sale == null) return null;

        return new Sale {
            Id = sale.Id,
            NetUid = sale.NetUid,
            Created = sale.Created,
            Updated = sale.Updated,
            Deleted = sale.Deleted,
            OrderId = sale.OrderId,
            ClientAgreementId = sale.ClientAgreementId,
            BaseLifeCycleStatusId = sale.BaseLifeCycleStatusId,
            BaseSalePaymentStatusId = sale.BaseSalePaymentStatusId,
            DeliveryRecipientId = sale.DeliveryRecipientId,
            DeliveryRecipientAddressId = sale.DeliveryRecipientAddressId,
            TransporterId = sale.TransporterId,
            SaleNumberId = sale.SaleNumberId,
            ShiftStatusId = sale.ShiftStatusId,
            WorkplaceId = sale.WorkplaceId,
            CustomersOwnTtnId = sale.CustomersOwnTtnId,
            Comment = sale.Comment,
            IsInvoice = sale.IsInvoice,
            IsVatSale = sale.IsVatSale,
            IsCashOnDelivery = sale.IsCashOnDelivery,
            HasDocuments = sale.HasDocuments,
            CashOnDeliveryAmount = sale.CashOnDeliveryAmount,
            ShipmentDate = sale.ShipmentDate,
            SaleNumber = sale.SaleNumber == null
                ? null
                : new SaleNumber {
                    Id = sale.SaleNumber.Id,
                    NetUid = sale.SaleNumber.NetUid,
                    Created = sale.SaleNumber.Created,
                    Updated = sale.SaleNumber.Updated,
                    Deleted = sale.SaleNumber.Deleted,
                    Value = sale.SaleNumber.Value,
                    OrganizationId = sale.SaleNumber.OrganizationId
                },
            Order = sale.Order == null
                ? null
                : new Order {
                    Id = sale.Order.Id,
                    NetUid = sale.Order.NetUid,
                    Created = sale.Order.Created,
                    Updated = sale.Order.Updated,
                    Deleted = sale.Order.Deleted,
                    OrderSource = sale.Order.OrderSource,
                    OrderStatus = sale.Order.OrderStatus
                }
        };
    }
}
