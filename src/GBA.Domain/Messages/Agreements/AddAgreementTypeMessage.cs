using GBA.Domain.Entities.Agreements;

namespace GBA.Domain.Messages.Agreements;

public sealed class AddAgreementTypeMessage {
    public AddAgreementTypeMessage(AgreementType agreementType) {
        AgreementType = agreementType;
    }

    public AgreementType AgreementType { get; set; }
}