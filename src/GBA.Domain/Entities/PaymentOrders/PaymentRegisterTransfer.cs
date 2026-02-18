using System;
using GBA.Domain.Entities.PaymentOrders.PaymentMovements;

namespace GBA.Domain.Entities.PaymentOrders {
    public sealed class PaymentRegisterTransfer : EntityBase {
        public string Number { get; set; }

        public string Comment { get; set; }

        public DateTime FromDate { get; set; }

        public decimal Amount { get; set; }

        public bool IsUpdated { get; set; }

        public bool IsCanceled { get; set; }

        public PaymentRegisterTransferType Type { get; set; }

        public TransferOperationType TypeOfOperation { get; set; }

        public long FromPaymentCurrencyRegisterId { get; set; }

        public long ToPaymentCurrencyRegisterId { get; set; }

        public long UserId { get; set; }

        public PaymentCurrencyRegister FromPaymentCurrencyRegister { get; set; }

        public PaymentCurrencyRegister ToPaymentCurrencyRegister { get; set; }

        public User User { get; set; }

        public PaymentMovementOperation PaymentMovementOperation { get; set; }
    }
}

public enum TransferOperationType {
    FundsTransfer,
    CashBankTransfer,
    PaymentRegisterTransfer
}