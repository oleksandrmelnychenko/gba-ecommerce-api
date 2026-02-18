using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Entities.Supplies.Protocols;

namespace GBA.Domain.Entities.Supplies.Documents;

public sealed class InvoiceDocument : BaseDocument {
    public long? SupplyInvoiceId { get; set; }

    public long? PortWorkServiceId { get; set; }

    public long? TransportationServiceId { get; set; }

    public long? ContainerServiceId { get; set; }

    public long? VehicleServiceId { get; set; }

    public long? CustomServiceId { get; set; }

    public long? VehicleDeliveryServiceId { get; set; }

    public long? CustomAgencyServiceId { get; set; }

    public long? PlaneDeliveryServiceId { get; set; }

    public long? PortCustomAgencyServiceId { get; set; }

    public long? SupplyOrderPolandPaymentDeliveryProtocolId { get; set; }

    public long? PackingListId { get; set; }

    public long? MergedServiceId { get; set; }

    public TypeInvoiceDocument Type { get; set; }

    public SupplyInvoice SupplyInvoice { get; set; }

    public PortWorkService PortWorkService { get; set; }

    public TransportationService TransportationService { get; set; }

    public ContainerService ContainerService { get; set; }

    public VehicleService VehicleService { get; set; }

    public CustomService CustomService { get; set; }

    public VehicleDeliveryService VehicleDeliveryService { get; set; }

    public CustomAgencyService CustomAgencyService { get; set; }

    public PlaneDeliveryService PlaneDeliveryService { get; set; }

    public PortCustomAgencyService PortCustomAgencyService { get; set; }

    public SupplyOrderPolandPaymentDeliveryProtocol SupplyOrderPolandPaymentDeliveryProtocol { get; set; }

    public PackingList PackingList { get; set; }

    public MergedService MergedService { get; set; }
}