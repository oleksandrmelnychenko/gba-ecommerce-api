using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Ukraine.Documents;

namespace GBA.Domain.Messages.Supplies.Ukraine.Sads;

public sealed class UploadSadDocumentsBySadNetIdMessage {
    public UploadSadDocumentsBySadNetIdMessage(List<SadDocument> documents, Guid netId) {
        Documents = documents;

        NetId = netId;
    }

    public List<SadDocument> Documents { get; }

    public Guid NetId { get; }
}