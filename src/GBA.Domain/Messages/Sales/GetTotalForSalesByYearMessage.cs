using System;

namespace GBA.Domain.Messages.Sales;

public sealed class GetTotalForSalesByYearMessage {
    public GetTotalForSalesByYearMessage(Guid clientNetId) {
        ClientNetId = clientNetId;
    }

    public Guid ClientNetId { get; set; }
}