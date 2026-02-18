using System;

namespace GBA.Domain.Messages.Products.ProductCapitalizations;

public sealed class ExportProductCapitalizationDocumentMessage {
    public ExportProductCapitalizationDocumentMessage(
        string pathToFolder,
        Guid netId) {
        PathToFolder = pathToFolder;
        NetId = netId;
    }

    public string PathToFolder { get; }

    public Guid NetId { get; }
}