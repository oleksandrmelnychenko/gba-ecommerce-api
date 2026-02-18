using System;
using GBA.Common.Helpers.ProductCapitalizations;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Messages.Products.ProductCapitalizations;

public sealed class AddNewProductCapitalizationFromFileMessage {
    public AddNewProductCapitalizationFromFileMessage(
        string path,
        Guid userNetId,
        ProductCapitalization productCapitalization,
        ProductCapitalizationParseConfiguration configuration
    ) {
        Path = path;

        UserNetId = userNetId;

        ProductCapitalization = productCapitalization;

        Configuration = configuration;
    }

    public string Path { get; }

    public Guid UserNetId { get; }

    public ProductCapitalization ProductCapitalization { get; }

    public ProductCapitalizationParseConfiguration Configuration { get; }
}