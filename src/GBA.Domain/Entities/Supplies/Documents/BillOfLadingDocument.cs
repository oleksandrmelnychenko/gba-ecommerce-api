using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Entities.Supplies.Documents;

public sealed class BillOfLadingDocument : BaseDocument {
    public BillOfLadingDocument() {
        ContainerServices = new HashSet<ContainerService>();

        VehicleServices = new HashSet<VehicleService>();
    }

    public string Number { get; set; }

    public DateTime Date { get; set; }

    public decimal Amount { get; set; }

    public ICollection<ContainerService> ContainerServices { get; set; }

    public ICollection<VehicleService> VehicleServices { get; set; }

    public long? BillOfLadingServiceId { get; set; }

    public BillOfLadingService BillOfLadingService { get; set; }
}