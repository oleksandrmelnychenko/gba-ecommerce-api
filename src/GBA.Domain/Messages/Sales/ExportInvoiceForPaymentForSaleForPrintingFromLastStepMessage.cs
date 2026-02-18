using System;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Messages.Sales;

public sealed class ExportInvoiceForPaymentForSaleForPrintingFromLastStepMessage {
    public ExportInvoiceForPaymentForSaleForPrintingFromLastStepMessage(
        Sale sale,
        Guid userNetId,
        string path
    ) {
        Sale = sale;

        UserNetId = userNetId;

        Path = path;
    }

    public Sale Sale { get; }

    public Guid UserNetId { get; }

    public string Path { get; }
}