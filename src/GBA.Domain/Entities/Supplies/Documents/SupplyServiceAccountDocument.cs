using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Entities.Supplies.Documents;

public sealed class SupplyServiceAccountDocument : BaseDocument {
    public string Number { get; set; }

    public PortWorkService PortWorkService { get; set; }

    public TransportationService TransportationService { get; set; }

    public ContainerService ContainerService { get; set; }

    public VehicleService VehicleService { get; set; }

    public CustomService CustomService { get; set; }

    public VehicleDeliveryService VehicleDeliveryService { get; set; }

    public CustomAgencyService CustomAgencyService { get; set; }

    public PlaneDeliveryService PlaneDeliveryService { get; set; }

    public PortCustomAgencyService PortCustomAgencyService { get; set; }

    public MergedService MergedService { get; set; }

    public BillOfLadingService BillOfLadingService { get; set; }
}