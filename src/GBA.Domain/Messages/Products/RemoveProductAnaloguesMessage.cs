using System;

namespace GBA.Domain.Messages.Products;

public sealed class RemoveProductAnaloguesMessage {
    public RemoveProductAnaloguesMessage(Guid baseProductNetId, Guid analogueProductNetId, bool removeIndirectAnalogues) {
        BaseProductNetId = baseProductNetId;
        AnalogueProductNetId = analogueProductNetId;
        RemoveIndirectAnalogues = removeIndirectAnalogues;
    }

    public Guid BaseProductNetId { get; }
    public Guid AnalogueProductNetId { get; }
    public bool RemoveIndirectAnalogues { get; }
}