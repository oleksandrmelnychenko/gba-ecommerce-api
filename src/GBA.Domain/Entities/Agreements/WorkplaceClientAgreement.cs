using GBA.Domain.Entities.Clients;

namespace GBA.Domain.Entities.Agreements;

public sealed class WorkplaceClientAgreement : EntityBase {
    public long WorkplaceId { get; set; }

    public long ClientAgreementId { get; set; }

    public Workplace Workplace { get; set; }

    public ClientAgreement ClientAgreement { get; set; }

    public bool IsSelected { get; set; }
}