using System;
using GBA.Domain.EntityHelpers.ReSaleModels;

namespace GBA.Domain.Messages.ReSales;

public sealed class ExportReSaleDocumentMessage {
    public ExportReSaleDocumentMessage(
        string folderPath,
        Guid netId,
        ReSaleDownloadDocumentType type) {
        FolderPath = folderPath;
        NetId = netId;
        Type = type;
    }

    public string FolderPath { get; }
    public Guid NetId { get; }
    public ReSaleDownloadDocumentType Type { get; }
}