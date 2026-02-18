namespace GBA.Domain.Entities.Clients.Documents;

public sealed class ClientContractDocument : EntityBase {
    public string DocumentUrl { get; set; }

    public string FileName { get; set; }

    public string ContentType { get; set; }

    public string GeneratedName { get; set; }

    public long ClientId { get; set; }

    public Client Client { get; set; }
}