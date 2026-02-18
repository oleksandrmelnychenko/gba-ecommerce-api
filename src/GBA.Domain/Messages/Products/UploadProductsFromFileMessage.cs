using System;
using GBA.Common.Helpers;

namespace GBA.Domain.Messages.Products;

public sealed class UploadProductsFromFileMessage {
    public UploadProductsFromFileMessage(
        string pathToFile,
        ProductUploadParseConfiguration configuration,
        Guid userNetId
    ) {
        PathToFile = pathToFile;

        Configuration = configuration;

        UserNetId = userNetId;
    }

    public string PathToFile { get; }

    public ProductUploadParseConfiguration Configuration { get; }

    public Guid UserNetId { get; }
}