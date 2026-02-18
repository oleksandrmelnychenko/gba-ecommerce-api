using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Consignments;
using GBA.Domain.Entities.Supplies.PackingLists;

namespace GBA.Domain.Entities.Products.Incomes;

public sealed class ProductIncome : EntityBase {
    public ProductIncome() {
        ProductIncomeItems = new HashSet<ProductIncomeItem>();

        Consignments = new HashSet<Consignment>();
    }

    public ProductIncomeType ProductIncomeType { get; set; }

    public DateTime FromDate { get; set; }

    public string Number { get; set; }

    public string Comment { get; set; }

    public long UserId { get; set; }

    public long StorageId { get; set; }

    public decimal TotalNetPrice { get; set; }

    public decimal AccountingTotalNetPrice { get; set; }

    public double TotalNetWeight { get; set; }

    public double TotalQty { get; set; }

    public double TotalGrossWeight { get; set; }

    public decimal TotalGrossPrice { get; set; }

    public decimal ExchangeRateToUah { get; set; }

    public decimal TotalVatAmount { get; set; }

    public bool IsHide { get; set; }

    public bool IsFromOneC { get; set; }

    public User User { get; set; }

    public Storage Storage { get; set; }

    public Organization Organization { get; set; }

    public ICollection<ProductIncomeItem> ProductIncomeItems { get; set; }

    public ICollection<Consignment> Consignments { get; set; }

    public Currency Currency { get; set; }

    public PackingList PackingList { get; set; }

    public decimal TotalNetWithVat { get; set; }

    public int TotalRowsQty { get; set; }
}