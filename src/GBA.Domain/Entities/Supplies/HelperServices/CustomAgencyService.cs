using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.PackingLists;

namespace GBA.Domain.Entities.Supplies.HelperServices;

public sealed class CustomAgencyService : BaseService {
    public CustomAgencyService() {
        SupplyOrders = new HashSet<SupplyOrder>();

        InvoiceDocuments = new HashSet<InvoiceDocument>();

        ServiceDetailItems = new HashSet<ServiceDetailItem>();

        PackingListPackageOrderItemSupplyServices = new HashSet<PackingListPackageOrderItemSupplyService>();
    }

    public long? CustomAgencyOrganizationId { get; set; }

    public SupplyOrganization CustomAgencyOrganization { get; set; }

    public ICollection<SupplyOrder> SupplyOrders { get; set; }

    public ICollection<InvoiceDocument> InvoiceDocuments { get; set; }

    public ICollection<ServiceDetailItem> ServiceDetailItems { get; set; }

    public ICollection<PackingListPackageOrderItemSupplyService> PackingListPackageOrderItemSupplyServices { get; set; }
}