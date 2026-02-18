using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.PackingLists;

namespace GBA.Domain.Entities.Supplies.HelperServices;

public sealed class TransportationService : BaseService {
    public TransportationService() {
        InvoiceDocuments = new HashSet<InvoiceDocument>();

        SupplyOrders = new HashSet<SupplyOrder>();

        ServiceDetailItems = new HashSet<ServiceDetailItem>();

        PackingListPackageOrderItemSupplyServices = new HashSet<PackingListPackageOrderItemSupplyService>();
    }

    public bool IsSealAndSignatureVerified { get; set; }

    public long? TransportationOrganizationId { get; set; }

    public SupplyOrganization TransportationOrganization { get; set; }

    public ICollection<InvoiceDocument> InvoiceDocuments { get; set; }

    public ICollection<SupplyOrder> SupplyOrders { get; set; }

    public ICollection<ServiceDetailItem> ServiceDetailItems { get; set; }

    public ICollection<PackingListPackageOrderItemSupplyService> PackingListPackageOrderItemSupplyServices { get; set; }
}