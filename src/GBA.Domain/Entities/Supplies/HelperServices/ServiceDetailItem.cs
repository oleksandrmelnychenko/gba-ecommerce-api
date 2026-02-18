namespace GBA.Domain.Entities.Supplies.HelperServices;

public sealed class ServiceDetailItem : EntityBase {
    public double Qty { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal NetPrice { get; set; } //calculated  Qty * UnitPrice

    public decimal GrossPrice { get; set; } //calculated

    public decimal Vat { get; set; } //calculated

    public double VatPercent { get; set; }

    public long ServiceDetailItemKeyId { get; set; }

    public long? CustomAgencyServiceId { get; set; }

    public long? CustomServiceId { get; set; }

    public long? PlaneDeliveryServiceId { get; set; }

    public long? PortCustomAgencyServiceId { get; set; }

    public long? PortWorkServiceId { get; set; }

    public long? TransportationServiceId { get; set; }

    public long? VehicleDeliveryServiceId { get; set; }

    public long? MergedServiceId { get; set; }

    public VehicleDeliveryService VehicleDeliveryService { get; set; }

    public TransportationService TransportationService { get; set; }

    public PortWorkService PortWorkService { get; set; }

    public PortCustomAgencyService PortCustomAgencyService { get; set; }

    public PlaneDeliveryService PlaneDeliveryService { get; set; }

    public CustomService CustomService { get; set; }

    public CustomAgencyService CustomAgencyService { get; set; }

    public ServiceDetailItemKey ServiceDetailItemKey { get; set; }

    public MergedService MergedService { get; set; }
}