using GBA.Common.Helpers;

namespace GBA.Domain.Messages.Clients;

public sealed class GetAllClientsTotalAmountMessage {
    public GetAllClientsTotalAmountMessage(ClientTypeType type) {
        Type = type;
    }

    public ClientTypeType Type { get; set; }
}