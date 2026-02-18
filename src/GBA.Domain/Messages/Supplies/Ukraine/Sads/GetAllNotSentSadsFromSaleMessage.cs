using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Messages.Supplies.Ukraine.Sads;

public sealed class GetAllNotSentSadsFromSaleMessage {
    public GetAllNotSentSadsFromSaleMessage(SadType type) {
        Type = type;
    }

    public SadType Type { get; }
}