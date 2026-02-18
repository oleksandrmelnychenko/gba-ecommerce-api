using GBA.Domain.Entities.Supplies.ActProvidingServices;

namespace GBA.Domain.Messages.Supplies.ActProvidingServices;

public sealed class UpdateActProvidingServiceMessage {
    public UpdateActProvidingServiceMessage(
        ActProvidingService act) {
        Act = act;
    }

    public ActProvidingService Act { get; }
}