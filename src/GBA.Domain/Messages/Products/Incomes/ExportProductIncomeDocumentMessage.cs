using System;

namespace GBA.Domain.Messages.Products.Incomes;

public sealed class ExportProductIncomeDocumentMessage {
    public ExportProductIncomeDocumentMessage(
        string pathToFolder,
        Guid netId) {
        PathToFolder = pathToFolder;
        NetId = netId;
    }

    public string PathToFolder { get; }
    public Guid NetId { get; }
}