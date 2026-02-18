using System;

namespace GBA.Domain.Messages.Sales;

public sealed class GetAllPaymentImagesFilteredMessage {
    public GetAllPaymentImagesFilteredMessage(
        DateTime? saleDateFrom = null,
        DateTime? saleDateTo = null,
        string saleNumber = "",
        string clientName = "",
        string phoneNumber = "") {
        SaleDateFrom = saleDateFrom;
        SaleDateTo = saleDateTo;
        SaleNumber = saleNumber;
        ClientName = clientName;
        PhoneNumber = phoneNumber;
    }

    public DateTime? SaleDateFrom { get; }
    public DateTime? SaleDateTo { get; }
    public string SaleNumber { get; }
    public string ClientName { get; }
    public string PhoneNumber { get; }
}