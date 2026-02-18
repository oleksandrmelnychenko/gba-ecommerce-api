using System;
using GBA.Common.Helpers.DepreciatedOrders;
using GBA.Domain.Entities.DepreciatedOrders;

namespace GBA.Domain.Messages.DepreciatedOrders;

public sealed class AddNewDepreciatedOrderFromFileMessage {
    public AddNewDepreciatedOrderFromFileMessage(
        DepreciatedOrder order,
        DepreciatedAndTransferParseConfiguration parseConfig,
        string pathToFile,
        Guid userNetId
    ) {
        Order = order;
        ParseConfig = parseConfig;
        PathToFile = pathToFile;
        UserNetId = userNetId;
    }

    public DepreciatedOrder Order { get; }
    public DepreciatedAndTransferParseConfiguration ParseConfig { get; }
    public string PathToFile { get; }
    public Guid UserNetId { get; }
}