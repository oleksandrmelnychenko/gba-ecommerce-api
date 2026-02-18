using System;

namespace GBA.Domain.Messages.PaymentOrders.IncomePaymentOrders;

public sealed class ChangeClientOnIncomePaymentOrderMessage {
    public ChangeClientOnIncomePaymentOrderMessage(Guid incomeNetId, Guid clientNetId, Guid clientAgreementNetId, Guid userNetId) {
        IncomeNetId = incomeNetId;

        ClientNetId = clientNetId;

        ClientAgreementNetId = clientAgreementNetId;

        UserNetId = userNetId;
    }

    public Guid IncomeNetId { get; }

    public Guid ClientNetId { get; }

    public Guid ClientAgreementNetId { get; }

    public Guid UserNetId { get; }
}