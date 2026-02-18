using System;
using GBA.Domain.EntityHelpers.Agreements;

namespace GBA.Domain.Messages.Agreements;

public sealed class GetAgreementDocumentByAgreementNetIdMessage {
    public GetAgreementDocumentByAgreementNetIdMessage(Guid netId, AgreementDownloadDocumentType documentType, string path) {
        NetId = netId;
        DocumentType = documentType;
        Path = path;
    }

    public Guid NetId { get; }
    public AgreementDownloadDocumentType DocumentType { get; }
    public string Path { get; }
}