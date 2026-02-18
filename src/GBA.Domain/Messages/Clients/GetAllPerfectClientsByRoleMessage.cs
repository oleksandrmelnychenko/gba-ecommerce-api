namespace GBA.Domain.Messages.Clients;

public sealed class GetAllPerfectClientsByRoleMessage {
    public GetAllPerfectClientsByRoleMessage(long clientTypeRoleId) {
        ClientTypeRoleId = clientTypeRoleId;
    }

    public long ClientTypeRoleId { get; set; }
}