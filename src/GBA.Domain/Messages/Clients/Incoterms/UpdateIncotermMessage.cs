using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Messages.Clients.Incoterms;

public sealed class UpdateIncotermMessage {
    public UpdateIncotermMessage(Incoterm incoterm) {
        Incoterm = incoterm;
    }

    public Incoterm Incoterm { get; }
}