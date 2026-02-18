using System;

namespace GBA.Domain.Messages.Sales;

public sealed class SwitchBillSaleUnderClientStructureMessage {
    public SwitchBillSaleUnderClientStructureMessage(
        Guid saleNetId,
        Guid clientAgreementNetId
    ) {
        SaleNetId = saleNetId;

        ClientAgreementNetId = clientAgreementNetId;
    }

    public Guid SaleNetId { get; }

    public Guid ClientAgreementNetId { get; }
}