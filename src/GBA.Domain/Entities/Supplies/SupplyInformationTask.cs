using System;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Entities.Supplies;

public sealed class SupplyInformationTask : EntityBase {
    public string Comment { get; set; }

    public DateTime FromDate { get; set; }

    public long UserId { get; set; }

    public long? UpdatedById { get; set; }

    public long? DeletedById { get; set; }

    public decimal GrossPrice { get; set; }

    public User User { get; set; }

    public User UpdatedBy { get; set; }

    public User DeletedBy { get; set; }

    public ContainerService ContainerService { get; set; }

    public VehicleService VehicleService { get; set; }

    public CustomService CustomService { get; set; }

    public PortWorkService PortWorkService { get; set; }

    public TransportationService TransportationService { get; set; }

    public PortCustomAgencyService PortCustomAgencyService { get; set; }

    public CustomAgencyService CustomAgencyService { get; set; }

    public PlaneDeliveryService PlaneDeliveryService { get; set; }

    public VehicleDeliveryService VehicleDeliveryService { get; set; }

    public MergedService MergedService { get; set; }

    public BillOfLadingService BillOfLadingService { get; set; }
}