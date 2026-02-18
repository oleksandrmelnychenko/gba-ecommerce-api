using System;
using GBA.Common.Helpers.SupplyOrders;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Messages.Supplies;

public sealed class AddNewSupplyOrderFromFileMessage {
    public AddNewSupplyOrderFromFileMessage(
        SupplyOrder supplyOrder,
        DocumentParseConfiguration parseConfiguration,
        string pathToFile,
        string tempDataFolder,
        Guid userNetId) {
        SupplyOrder = supplyOrder;

        ParseConfiguration = parseConfiguration;

        PathToFile = pathToFile;

        TempDataFolder = tempDataFolder;

        UserNetId = userNetId;
    }

    public SupplyOrder SupplyOrder { get; }

    public DocumentParseConfiguration ParseConfiguration { get; }

    public string PathToFile { get; }
    public string TempDataFolder { get; }

    public Guid UserNetId { get; }
}