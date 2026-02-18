using System;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Messages.Clients;

public sealed class UpdateClientMessage {
    public UpdateClientMessage(Client client, Guid updatedByNetId) {
        Client = client;

        UpdatedByNetId = updatedByNetId;
    }

    public Client Client { get; set; }

    public Guid UpdatedByNetId { get; set; }
}