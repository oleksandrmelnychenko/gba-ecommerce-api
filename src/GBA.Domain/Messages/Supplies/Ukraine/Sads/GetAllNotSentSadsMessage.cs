using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Messages.Supplies.Ukraine.Sads;

public sealed class GetAllNotSentSadsMessage {
    public GetAllNotSentSadsMessage(SadType type) {
        Type = type;
    }

    public SadType Type { get; }
}