using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Entities.SaleReturns;

public sealed class SaleReturn : EntityBase {
    public SaleReturn() {
        SaleReturnItems = new HashSet<SaleReturnItem>();
    }

    public DateTime FromDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string Number { get; set; }

    public long ClientAgreementId { get; set; }

    public long ClientId { get; set; }

    public long CreatedById { get; set; }

    public long? UpdatedById { get; set; }

    public long? CanceledById { get; set; }

    public bool IsCanceled { get; set; }

    public decimal TotalAmountLocal { get; set; }

    public Client Client { get; set; }

    public ClientAgreement ClientAgreement { get; set; }

    public User CreatedBy { get; set; }

    public User UpdatedBy { get; set; }

    public User CanceledBy { get; set; }

    public ICollection<SaleReturnItem> SaleReturnItems { get; set; }

    public Sale Sale { get; set; }

    public Storage Storage { get; set; }

    public Currency Currency { get; set; }

    public double TotalCount { get; set; }

    // Ignored

    public decimal TotalVatAmountLocal { get; set; }
    public decimal TotalVatAmount { get; set; }

    public decimal ExchangeRate { get; set; }
}