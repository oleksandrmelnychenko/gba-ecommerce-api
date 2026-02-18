using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.PackingLists;

namespace GBA.Domain.Entities.Supplies.HelperServices;

public sealed class CustomService : BaseService {
    public CustomService() {
        InvoiceDocuments = new HashSet<InvoiceDocument>();

        ServiceDetailItems = new HashSet<ServiceDetailItem>();

        PackingListPackageOrderItemSupplyServices = new HashSet<PackingListPackageOrderItemSupplyService>();
    }

    public long? CustomOrganizationId { get; set; }

    public long? ExciseDutyOrganizationId { get; set; }

    public long SupplyOrderId { get; set; }

    public SupplyCustomType SupplyCustomType { get; set; }

    public SupplyOrganization ExciseDutyOrganization { get; set; }

    public SupplyOrder SupplyOrder { get; set; }

    public SupplyOrganization CustomOrganization { get; set; }

    public ICollection<InvoiceDocument> InvoiceDocuments { get; set; }

    public ICollection<ServiceDetailItem> ServiceDetailItems { get; set; }

    public ICollection<PackingListPackageOrderItemSupplyService> PackingListPackageOrderItemSupplyServices { get; set; }
}