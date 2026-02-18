using GBA.Domain.Entities.Transporters;

namespace GBA.Domain.Messages.Transporters;

public sealed class UpdateTransporterMessage {
    public UpdateTransporterMessage(Transporter transporter) {
        Transporter = transporter;
    }

    public Transporter Transporter { get; set; }
}