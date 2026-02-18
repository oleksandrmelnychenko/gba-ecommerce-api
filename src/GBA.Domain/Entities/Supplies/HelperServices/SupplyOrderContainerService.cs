namespace GBA.Domain.Entities.Supplies.HelperServices;

public sealed class SupplyOrderContainerService : EntityBase {
    public long SupplyOrderId { get; set; }

    public long ContainerServiceId { get; set; }

    public SupplyOrder SupplyOrder { get; set; }

    public ContainerService ContainerService { get; set; }
}