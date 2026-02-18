using System.Collections.Generic;
using GBA.Common.Helpers;

namespace GBA.Domain.Entities.Supplies.HelperServices;

public sealed class ServiceDetailItemKey : EntityBase {
    public ServiceDetailItemKey() {
        ServiceDetailItems = new HashSet<ServiceDetailItem>();
    }

    public string Symbol { get; set; }

    public string Name { get; set; }

    public SupplyServiceType Type { get; set; }

    public ICollection<ServiceDetailItem> ServiceDetailItems { get; set; }
}