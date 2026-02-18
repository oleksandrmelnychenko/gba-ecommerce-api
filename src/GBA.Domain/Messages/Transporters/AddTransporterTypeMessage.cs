using GBA.Domain.Entities.Transporters;

namespace GBA.Domain.Messages.Transporters;

public sealed class AddTransporterTypeMessage {
    public AddTransporterTypeMessage(TransporterType transporterType) {
        TransporterType = transporterType;
    }

    public TransporterType TransporterType { get; set; }
}