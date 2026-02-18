using System;

namespace GBA.Domain.Messages.Products;

public sealed class RemoveProductComponentsMessage {
    public RemoveProductComponentsMessage(Guid baseProductNetId, Guid componentNetId, bool isProductSet) {
        BaseProductNetId = baseProductNetId;
        ComponentNetId = componentNetId;
        IsProductSet = isProductSet;
    }

    public Guid BaseProductNetId { get; }
    public Guid ComponentNetId { get; }

    public bool IsProductSet { get; }
}