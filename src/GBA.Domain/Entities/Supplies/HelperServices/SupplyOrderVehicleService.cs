namespace GBA.Domain.Entities.Supplies.HelperServices;

public sealed class SupplyOrderVehicleService : EntityBase {
    public long SupplyOrderId { get; set; }

    public long VehicleServiceId { get; set; }

    public SupplyOrder SupplyOrder { get; set; }

    public VehicleService VehicleService { get; set; }
}