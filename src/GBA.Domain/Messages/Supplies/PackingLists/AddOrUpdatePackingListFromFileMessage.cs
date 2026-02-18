using System;
using GBA.Common.Helpers.SupplyOrders;
using GBA.Domain.Entities.Supplies.PackingLists;

namespace GBA.Domain.Messages.Supplies.PackingLists;

public sealed class AddOrUpdatePackingListFromFileMessage {
    public AddOrUpdatePackingListFromFileMessage(
        PackingList packingList,
        DocumentParseConfiguration parseConfiguration,
        string pathToFile,
        Guid supplyInvoiceNetId,
        Guid userNetId
    ) {
        PackingList = packingList;

        ParseConfiguration = parseConfiguration;

        PathToFile = pathToFile;

        SupplyInvoiceNetId = supplyInvoiceNetId;

        UserNetId = userNetId;
    }

    public PackingList PackingList { get; private set; }

    public DocumentParseConfiguration ParseConfiguration { get; private set; }

    public string PathToFile { get; private set; }

    public Guid SupplyInvoiceNetId { get; private set; }

    public Guid UserNetId { get; set; }
}