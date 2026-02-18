using System;

namespace GBA.Domain.SignalRMessages;

public sealed class PaymentTaskMessage {
    public string OrganisationName { get; set; }

    public string PaymentForm { get; set; }

    public decimal Amount { get; set; }

    public double Discount { get; set; }

    public DateTime? PayToDate { get; set; }

    public string CreatedBy { get; set; }
}