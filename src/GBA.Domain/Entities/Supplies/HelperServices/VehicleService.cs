using System;
using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.PackingLists;

namespace GBA.Domain.Entities.Supplies.HelperServices;

public sealed class VehicleService : BaseService {
    public VehicleService() {
        InvoiceDocuments = new HashSet<InvoiceDocument>();

        PackingLists = new HashSet<PackingList>();

        SupplyOrderVehicleServices = new HashSet<SupplyOrderVehicleService>();

        PackingListPackageOrderItemSupplyServices = new HashSet<PackingListPackageOrderItemSupplyService>();
    }

    public DateTime LoadDate { get; set; }

    public double GrossWeight { get; set; }

    public string VehicleNumber { get; set; }

    public string TermDeliveryInDays { get; set; }

    public long? BillOfLadingDocumentId { get; set; }

    public long? VehicleOrganizationId { get; set; }

    public bool IsCalculatedExtraCharge { get; set; }

    public SupplyExtraChargeType SupplyExtraChargeType { get; set; }

    public SupplyOrganization VehicleOrganization { get; set; }

    public BillOfLadingDocument BillOfLadingDocument { get; set; }

    public ICollection<InvoiceDocument> InvoiceDocuments { get; set; }

    public ICollection<PackingList> PackingLists { get; set; }

    public ICollection<SupplyOrderVehicleService> SupplyOrderVehicleServices { get; set; }

    public ICollection<PackingListPackageOrderItemSupplyService> PackingListPackageOrderItemSupplyServices { get; set; }
}