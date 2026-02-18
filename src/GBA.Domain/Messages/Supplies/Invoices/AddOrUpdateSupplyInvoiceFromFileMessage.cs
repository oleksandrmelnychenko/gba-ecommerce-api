using System;
using GBA.Common.Helpers.SupplyOrders;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.Messages.Supplies;

public sealed class AddOrUpdateSupplyInvoiceFromFileMessage {
    public AddOrUpdateSupplyInvoiceFromFileMessage(
        SupplyInvoice supplyInvoice,
        DocumentParseConfiguration documentParseConfiguration,
        Guid supplyOrderNetId,
        Guid userNetId,
        string pathToFile
    ) {
        SupplyInvoice = supplyInvoice;

        DocumentParseConfiguration = documentParseConfiguration;

        SupplyOrderNetId = supplyOrderNetId;

        UserNetId = userNetId;

        PathToFile = pathToFile;
    }

    public SupplyInvoice SupplyInvoice { get; }

    public DocumentParseConfiguration DocumentParseConfiguration { get; }

    public Guid SupplyOrderNetId { get; }

    public Guid UserNetId { get; }

    public string PathToFile { get; }
}