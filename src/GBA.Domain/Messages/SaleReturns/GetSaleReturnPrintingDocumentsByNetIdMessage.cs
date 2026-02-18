using System;

namespace GBA.Domain.Messages.SaleReturns;

public sealed class GetSaleReturnPrintingDocumentsByNetIdMessage {
    public GetSaleReturnPrintingDocumentsByNetIdMessage(Guid netId, string path) {
        NetId = netId;

        Path = path;
    }

    public Guid NetId { get; }

    public string Path { get; }
}