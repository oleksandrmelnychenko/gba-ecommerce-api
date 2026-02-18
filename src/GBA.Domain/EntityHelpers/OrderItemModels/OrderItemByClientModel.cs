namespace GBA.Domain.EntityHelpers.OrderItemModels;

public sealed class OrderItemByClientModel {
    public string ProductVendorCode { get; set; }

    public string ProductName { get; set; }

    public double Qty { get; set; }
}