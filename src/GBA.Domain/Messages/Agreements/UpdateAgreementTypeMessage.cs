using GBA.Domain.Entities.Agreements;

namespace GBA.Domain.Messages.Agreements;

public sealed class UpdateAgreementTypeMessage {
    public UpdateAgreementTypeMessage(AgreementType agreementType) {
        AgreementType = agreementType;
    }

    public AgreementType AgreementType { get; set; }
}