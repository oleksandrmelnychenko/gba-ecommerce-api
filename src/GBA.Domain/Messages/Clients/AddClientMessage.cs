using System;
using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Messages.Clients;

public sealed class AddClientMessage {
    public AddClientMessage(Client client, Guid parentId, Guid updatedByNetId) {
        Client = client;

        ParentNetId = parentId;

        UpdatedByNetId = updatedByNetId;
    }

    public Client Client { get; set; }

    public Guid ParentNetId { get; set; }

    public Guid UpdatedByNetId { get; set; }
}