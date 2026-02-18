using System.Collections.Generic;
using GBA.Common.Helpers;

namespace GBA.Domain.Entities.Clients;

public sealed class RetailPaymentStatus : EntityBase {
    public RetailPaymentStatus() {
        RetailClientPaymentImages = new HashSet<RetailClientPaymentImage>();
    }

    public RetailPaymentStatusType RetailPaymentStatusType { get; set; }

    public decimal Amount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal AmountToPay { get; set; }

    public ICollection<RetailClientPaymentImage> RetailClientPaymentImages { get; set; }
}