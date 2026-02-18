using System;

namespace GBA.Domain.Messages.Sales.ShipmentLists;

public sealed class GetCreateDocumentShipmentFilteredMessage {
    public GetCreateDocumentShipmentFilteredMessage(
        DateTime from,
        DateTime to,
        Guid netId,
        Guid userNetId,
        string saleInvoicesFolderPath
    ) {
        From = from.Year.Equals(1) ? DateTime.UtcNow.Date : from.Date;

        To = to.Year.Equals(1) ? DateTime.UtcNow.Date.AddHours(23).AddMinutes(59).AddSeconds(59) : to.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

        NetId = netId;

        UserNetId = userNetId;

        SaleInvoicesFolderPath = saleInvoicesFolderPath;
    }

    public string SaleInvoicesFolderPath { get; set; }

    public DateTime From { get; }

    public DateTime To { get; }

    public Guid NetId { get; }

    public Guid UserNetId { get; }
}