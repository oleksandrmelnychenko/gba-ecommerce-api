using GBA.Domain.Entities.Agreements;

namespace GBA.Domain.Messages.Agreements;

public sealed class UpdateAgreementMessage {
    public UpdateAgreementMessage(Agreement agreement) {
        Agreement = agreement;
    }

    public Agreement Agreement { get; set; }
}