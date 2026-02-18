using System;
using GBA.Common.Helpers.DepreciatedOrders;
using GBA.Domain.Entities.Products.Transfers;

namespace GBA.Domain.Messages.Products.Transfers;

public sealed class AddProductTransferFromFileMessage {
    public AddProductTransferFromFileMessage(
        ProductTransfer productTransfer,
        DepreciatedAndTransferParseConfiguration parseConfig,
        string pathToFile,
        Guid userNetId
    ) {
        ProductTransfer = productTransfer;
        ParseConfig = parseConfig;
        PathToFile = pathToFile;
        UserNetId = userNetId;
    }

    public ProductTransfer ProductTransfer { get; }
    public DepreciatedAndTransferParseConfiguration ParseConfig { get; }
    public string PathToFile { get; }
    public Guid UserNetId { get; }
}