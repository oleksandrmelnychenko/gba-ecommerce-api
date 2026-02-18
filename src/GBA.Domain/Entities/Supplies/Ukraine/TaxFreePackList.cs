using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Entities.Supplies.Ukraine;

public sealed class TaxFreePackList : EntityBase {
    public TaxFreePackList() {
        TaxFrees = new HashSet<TaxFree>();

        SupplyOrderUkraineCartItems = new HashSet<SupplyOrderUkraineCartItem>();

        Sales = new HashSet<Sale>();

        TaxFreePackListOrderItems = new HashSet<TaxFreePackListOrderItem>();
    }

    public string Number { get; set; }

    public string Comment { get; set; }

    public double WeightLimit { get; set; }

    public decimal MarginAmount { get; set; }

    public decimal MaxPriceLimit { get; set; }

    public decimal MinPriceLimit { get; set; }

    public decimal TotalUnspecifiedAmount { get; set; }

    public decimal TotalUnspecifiedAmountLocal { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal TotalVatAmountLocal { get; set; }

    public decimal TotalAmountLocal { get; set; }

    public double TotalUnspecifiedWeight { get; set; }

    public double TotalWeight { get; set; }

    public int MaxQtyInTaxFree { get; set; }

    public int MaxPositionsInTaxFree { get; set; }

    public int TaxFreesCount { get; set; }

    public bool IsSent { get; set; }

    public bool IsFromSale { get; set; }

    public DateTime FromDate { get; set; }

    public long ResponsibleId { get; set; }

    public long? OrganizationId { get; set; }

    public long? SupplyOrderUkraineId { get; set; }

    public long? ClientId { get; set; }

    public long? ClientAgreementId { get; set; }

    public string Status { get; set; }

    public User Responsible { get; set; }

    public Organization Organization { get; set; }

    public SupplyOrderUkraine SupplyOrderUkraine { get; set; }

    public Client Client { get; set; }

    public ClientAgreement ClientAgreement { get; set; }

    public ICollection<TaxFree> TaxFrees { get; set; }

    public ICollection<SupplyOrderUkraineCartItem> SupplyOrderUkraineCartItems { get; set; }

    public ICollection<Sale> Sales { get; set; }

    public ICollection<TaxFreePackListOrderItem> TaxFreePackListOrderItems { get; set; }
}