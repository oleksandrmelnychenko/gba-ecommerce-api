using GBA.Common.Helpers;

namespace GBA.Domain.Messages.Clients;

public sealed class GetAllPerfectClientsByTypeMessage {
    public GetAllPerfectClientsByTypeMessage(PerfectClientType perfectClientType) {
        PerfectClientType = perfectClientType;
    }

    public PerfectClientType PerfectClientType { get; set; }
}