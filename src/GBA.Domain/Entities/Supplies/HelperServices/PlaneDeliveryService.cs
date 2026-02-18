using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.PackingLists;

namespace GBA.Domain.Entities.Supplies.HelperServices;

public sealed class PlaneDeliveryService : BaseService {
    public PlaneDeliveryService() {
        SupplyOrders = new HashSet<SupplyOrder>();

        InvoiceDocuments = new HashSet<InvoiceDocument>();

        ServiceDetailItems = new HashSet<ServiceDetailItem>();

        PackingListPackageOrderItemSupplyServices = new HashSet<PackingListPackageOrderItemSupplyService>();
    }

    public long? PlaneDeliveryOrganizationId { get; set; }

    public SupplyOrganization PlaneDeliveryOrganization { get; set; }

    public ICollection<SupplyOrder> SupplyOrders { get; set; }

    public ICollection<InvoiceDocument> InvoiceDocuments { get; set; }

    public ICollection<ServiceDetailItem> ServiceDetailItems { get; set; }

    public ICollection<PackingListPackageOrderItemSupplyService> PackingListPackageOrderItemSupplyServices { get; set; }
}