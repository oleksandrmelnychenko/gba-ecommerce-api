using System;

namespace GBA.Domain.EntityHelpers.DebtorModels;

public sealed class DebtAfterDaysModel {
    public DebtAfterDaysModel(decimal total, Guid clientAgreementNetId) {
        Total = total;
        ClientAgreementNetId = clientAgreementNetId;
    }

    public decimal Total { get; set; }
    public Guid ClientAgreementNetId { get; set; }
}