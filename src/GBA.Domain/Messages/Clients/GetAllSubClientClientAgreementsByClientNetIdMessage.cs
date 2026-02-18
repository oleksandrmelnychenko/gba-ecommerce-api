using System;

namespace GBA.Domain.Messages.Clients;

public sealed class GetAllSubClientClientAgreementsByClientNetIdMessage {
    public GetAllSubClientClientAgreementsByClientNetIdMessage(Guid clientNetId) {
        ClientNetId = clientNetId;
    }

    public Guid ClientNetId { get; set; }
}