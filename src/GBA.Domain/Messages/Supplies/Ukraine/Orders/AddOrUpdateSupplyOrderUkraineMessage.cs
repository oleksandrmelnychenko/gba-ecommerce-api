using System;
using GBA.Common.Helpers.SupplyOrders;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Messages.Supplies.Ukraine.Orders;

public sealed class AddOrUpdateSupplyOrderUkraineMessage {
    public AddOrUpdateSupplyOrderUkraineMessage(
        SupplyOrderUkraine supplyOrderUkraine,
        UkraineOrderParseConfiguration parseConfiguration,
        Guid userNetId,
        string pathToFile
    ) {
        SupplyOrderUkraine = supplyOrderUkraine;

        ParseConfiguration = parseConfiguration;

        UserNetId = userNetId;

        PathToFile = pathToFile;
    }

    public SupplyOrderUkraine SupplyOrderUkraine { get; set; }

    public UkraineOrderParseConfiguration ParseConfiguration { get; }

    public Guid UserNetId { get; }

    public string PathToFile { get; }
}