using System;
using GBA.Common.Helpers.SupplyOrders;

namespace GBA.Domain.Messages.Supplies.Ukraine.SupplyOrderUkraineCartItems;

public sealed class UploadSelectCartItemsFileForPreviewValidationMessage {
    public UploadSelectCartItemsFileForPreviewValidationMessage(
        string pathToFile,
        CartItemsParseConfiguration configuration,
        Guid userNetId
    ) {
        PathToFile = pathToFile;

        Configuration = configuration;

        UserNetId = userNetId;
    }

    public string PathToFile { get; }

    public CartItemsParseConfiguration Configuration { get; }

    public Guid UserNetId { get; }
}