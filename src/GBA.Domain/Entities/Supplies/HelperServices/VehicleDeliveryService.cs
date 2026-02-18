using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.PackingLists;

namespace GBA.Domain.Entities.Supplies.HelperServices;

public sealed class VehicleDeliveryService : BaseService {
    public VehicleDeliveryService() {
        SupplyOrders = new HashSet<SupplyOrder>();

        InvoiceDocuments = new HashSet<InvoiceDocument>();

        ServiceDetailItems = new HashSet<ServiceDetailItem>();

        PackingListPackageOrderItemSupplyServices = new HashSet<PackingListPackageOrderItemSupplyService>();
    }

    public bool IsSealAndSignatureVerified { get; set; }

    public long? VehicleDeliveryOrganizationId { get; set; }

    public SupplyOrganization VehicleDeliveryOrganization { get; set; }

    public ICollection<SupplyOrder> SupplyOrders { get; set; }

    public ICollection<InvoiceDocument> InvoiceDocuments { get; set; }

    public ICollection<ServiceDetailItem> ServiceDetailItems { get; set; }

    public ICollection<PackingListPackageOrderItemSupplyService> PackingListPackageOrderItemSupplyServices { get; set; }
}