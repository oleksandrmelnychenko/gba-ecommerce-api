using System.Collections.Generic;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Entities.Clients;

public sealed class RetailClientPaymentImage : EntityBase {
    public RetailClientPaymentImage() {
        RetailClientPaymentImageItems = new HashSet<RetailClientPaymentImageItem>();
    }

    public long RetailClientId { get; set; }

    public long SaleId { get; set; }

    public long RetailPaymentStatusId { get; set; }

    public RetailClient RetailClient { get; set; }

    public Sale Sale { get; set; }

    public RetailPaymentStatus RetailPaymentStatus { get; set; }

    public ICollection<RetailClientPaymentImageItem> RetailClientPaymentImageItems { get; set; }
}