using System;
using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.PackingLists;

namespace GBA.Domain.Entities.Supplies.HelperServices;

public sealed class ContainerService : BaseService {
    public ContainerService() {
        InvoiceDocuments = new HashSet<InvoiceDocument>();

        PackingLists = new HashSet<PackingList>();

        SupplyOrderContainerServices = new HashSet<SupplyOrderContainerService>();

        PackingListPackageOrderItemSupplyServices = new HashSet<PackingListPackageOrderItemSupplyService>();
    }

    public DateTime LoadDate { get; set; }

    public double GroosWeight { get; set; }

    public string ContainerNumber { get; set; }

    public string TermDeliveryInDays { get; set; }

    public long? BillOfLadingDocumentId { get; set; }

    public long? ContainerOrganizationId { get; set; }

    public bool IsCalculatedExtraCharge { get; set; }

    public SupplyExtraChargeType SupplyExtraChargeType { get; set; }

    public SupplyOrganization ContainerOrganization { get; set; }

    public BillOfLadingDocument BillOfLadingDocument { get; set; }

    public ICollection<InvoiceDocument> InvoiceDocuments { get; set; }

    public ICollection<PackingList> PackingLists { get; set; }

    public ICollection<SupplyOrderContainerService> SupplyOrderContainerServices { get; set; }

    public ICollection<PackingListPackageOrderItemSupplyService> PackingListPackageOrderItemSupplyServices { get; set; }
}