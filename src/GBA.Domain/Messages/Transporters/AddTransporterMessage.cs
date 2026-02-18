using GBA.Domain.Entities.Transporters;

namespace GBA.Domain.Messages.Transporters;

public sealed class AddTransporterMessage {
    public AddTransporterMessage(Transporter transporter) {
        Transporter = transporter;
    }

    public Transporter Transporter { get; set; }
}