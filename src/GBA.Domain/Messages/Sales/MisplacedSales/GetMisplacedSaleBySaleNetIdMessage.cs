using System;

namespace GBA.Domain.Messages.Sales.MisplacedSales;

public sealed class GetMisplacedSaleBySaleNetIdMessage {
    public GetMisplacedSaleBySaleNetIdMessage(Guid netId) {
        NetId = netId;
    }

    public Guid NetId { get; }
}