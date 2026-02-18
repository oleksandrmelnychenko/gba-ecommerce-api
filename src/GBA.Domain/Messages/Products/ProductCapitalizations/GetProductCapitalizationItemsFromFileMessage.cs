using GBA.Common.Helpers.ProductCapitalizations;

namespace GBA.Domain.Messages.Products.ProductCapitalizations;

public sealed class GetProductCapitalizationItemsFromFileMessage {
    public GetProductCapitalizationItemsFromFileMessage(
        string path,
        ProductCapitalizationParseConfiguration configuration
    ) {
        Path = path;

        Configuration = configuration;
    }

    public string Path { get; }

    public ProductCapitalizationParseConfiguration Configuration { get; }
}