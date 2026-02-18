using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Entities.Supplies.PackingLists;

public sealed class PackingList : EntityBase {
    public PackingList() {
        PackingListPackages = new HashSet<PackingListPackage>();

        PackingListPallets = new HashSet<PackingListPackage>();

        PackingListBoxes = new HashSet<PackingListPackage>();

        PackingListPackageOrderItems = new HashSet<PackingListPackageOrderItem>();

        InvoiceDocuments = new HashSet<InvoiceDocument>();

        DynamicProductPlacementColumns = new HashSet<DynamicProductPlacementColumn>();

        MergedPackingLists = new HashSet<PackingList>();
    }

    public string MarkNumber { get; set; }

    public string InvNo { get; set; }

    public string PlNo { get; set; }

    public string RefNo { get; set; }

    public string No { get; set; }

    public string Comment { get; set; }

    public DateTime FromDate { get; set; }

    public long SupplyInvoiceId { get; set; }

    public long? ContainerServiceId { get; set; }

    public long? VehicleServiceId { get; set; }

    public long? RootPackingListId { get; set; }

    public int TotalPallets { get; set; }

    public int TotalBoxes { get; set; }

    public double TotalQuantity { get; set; }

    public double TotalGrossWeight { get; set; }

    public double TotalNetWeight { get; set; }

    public double TotalCBM { get; set; }

    public decimal TotalPrice { get; set; }

    public decimal ExtraCharge { get; set; }

    public decimal AccountingExtraCharge { get; set; }

    public decimal TotalNetPrice { get; set; }

    public decimal TotalNetPriceWithVat { get; set; }

    public decimal TotalGrossPrice { get; set; }

    public decimal AccountingTotalGrossPrice { get; set; }

    public decimal TotalNetPriceEur { get; set; }

    public decimal TotalNetPriceWithVatEur { get; set; }

    public decimal TotalGrossPriceEur { get; set; }

    public decimal AccountingTotalGrossPriceEur { get; set; }

    public decimal VatOnePercent { get; set; }

    public decimal VatTwoPercent { get; set; }

    public decimal TotalVatAmount { get; set; }

    public bool IsDocumentsAdded { get; set; }

    public bool IsPlaced { get; set; }

    public bool IsVatOneApplied { get; set; }

    public bool IsVatTwoApplied { get; set; }

    public decimal TotalCustomValue { get; set; }

    public decimal TotalDuty { get; set; }

    public ContainerService ContainerService { get; set; }

    public VehicleService VehicleService { get; set; }

    public SupplyInvoice SupplyInvoice { get; set; }

    public PackingList RootPackingList { get; set; }

    public ICollection<PackingListPackage> PackingListPackages { get; set; }

    public ICollection<PackingListPackage> PackingListPallets { get; set; }

    public ICollection<PackingListPackage> PackingListBoxes { get; set; }

    public ICollection<PackingListPackageOrderItem> PackingListPackageOrderItems { get; set; }

    public ICollection<InvoiceDocument> InvoiceDocuments { get; set; }

    public ICollection<DynamicProductPlacementColumn> DynamicProductPlacementColumns { get; set; }

    public ICollection<PackingList> MergedPackingLists { get; set; }
}