using System;
using GBA.Common.Helpers.SupplyOrders;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Messages.Supplies.Ukraine.Orders;

public sealed class AddOrUpdateSupplyOrderUkraineFromSupplierMessage {
    public AddOrUpdateSupplyOrderUkraineFromSupplierMessage(
        string pathToFile,
        string tempFolderPath,
        SupplyOrderUkraine supplyOrderUkraine,
        UkraineOrderFromSupplierParseConfiguration parseConfiguration,
        Guid userNetId
    ) {
        PathToFile = pathToFile;
        TempFolderPath = tempFolderPath;

        SupplyOrderUkraine = supplyOrderUkraine;

        ParseConfiguration = parseConfiguration;

        UserNetId = userNetId;
    }

    public string PathToFile { get; }
    public string TempFolderPath { get; }

    public SupplyOrderUkraine SupplyOrderUkraine { get; set; }

    public UkraineOrderFromSupplierParseConfiguration ParseConfiguration { get; }

    public Guid UserNetId { get; }
}