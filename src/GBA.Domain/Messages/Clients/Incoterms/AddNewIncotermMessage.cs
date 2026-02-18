using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Messages.Clients.Incoterms;

public sealed class AddNewIncotermMessage {
    public AddNewIncotermMessage(Incoterm incoterm) {
        Incoterm = incoterm;
    }

    public Incoterm Incoterm { get; }
}