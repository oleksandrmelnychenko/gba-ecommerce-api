using GBA.Common.Helpers;

namespace GBA.Domain.Messages.Products.ProductSpecifications;

public sealed class UploadProductSpecificationFromFileMessage {
    public UploadProductSpecificationFromFileMessage(
        ProductSpecificationParseConfiguration parseConfiguration,
        string pathToFile
    ) {
        ParseConfiguration = parseConfiguration;

        PathToFile = pathToFile;
    }

    public ProductSpecificationParseConfiguration ParseConfiguration { get; }

    public string PathToFile { get; }
}