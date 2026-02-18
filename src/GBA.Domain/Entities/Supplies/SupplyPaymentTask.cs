using System;
using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.Protocols;

namespace GBA.Domain.Entities.Supplies;

public sealed class SupplyPaymentTask : EntityBase {
    public SupplyPaymentTask() {
        PaymentDeliveryProtocols = new HashSet<SupplyOrderPaymentDeliveryProtocol>();

        SupplyOrderPolandPaymentDeliveryProtocols = new HashSet<SupplyOrderPolandPaymentDeliveryProtocol>();

        ContainerServices = new HashSet<ContainerService>();

        BrokerServices = new HashSet<CustomService>();

        PortWorkServices = new HashSet<PortWorkService>();

        TransportationServices = new HashSet<TransportationService>();

        PortCustomAgencyServices = new HashSet<PortCustomAgencyService>();

        CustomAgencyServices = new HashSet<CustomAgencyService>();

        PlaneDeliveryServices = new HashSet<PlaneDeliveryService>();

        VehicleDeliveryServices = new HashSet<VehicleDeliveryService>();

        SupplyPaymentTaskDocuments = new HashSet<SupplyPaymentTaskDocument>();

        OutcomePaymentOrderSupplyPaymentTasks = new HashSet<OutcomePaymentOrderSupplyPaymentTask>();

        MergedServices = new HashSet<MergedService>();

        SupplyOrderUkrainePaymentDeliveryProtocols = new HashSet<SupplyOrderUkrainePaymentDeliveryProtocol>();

        VehicleServices = new HashSet<VehicleService>();

        AccountingContainerServices = new HashSet<ContainerService>();

        AccountingBrokerServices = new HashSet<CustomService>();

        AccountingPortWorkServices = new HashSet<PortWorkService>();

        AccountingTransportationServices = new HashSet<TransportationService>();

        AccountingPortCustomAgencyServices = new HashSet<PortCustomAgencyService>();

        AccountingCustomAgencyServices = new HashSet<CustomAgencyService>();

        AccountingPlaneDeliveryServices = new HashSet<PlaneDeliveryService>();

        AccountingVehicleDeliveryServices = new HashSet<VehicleDeliveryService>();

        AccountingMergedServices = new HashSet<MergedService>();

        AccountingVehicleServices = new HashSet<VehicleService>();

        BillOfLadingServices = new HashSet<BillOfLadingService>();

        AccountingBillOfLadingServices = new HashSet<BillOfLadingService>();
    }

    public string Comment { get; set; }

    public TaskStatus TaskStatus { get; set; }

    public DateTime? TaskStatusUpdated { get; set; }

    public DateTime? PayToDate { get; set; }

    public long? UserId { get; set; }

    public long? UpdatedById { get; set; }

    public long? DeletedById { get; set; }

    public bool IsAvailableForPayment { get; set; }

    public TaskAssignedTo TaskAssignedTo { get; set; }

    public decimal CurrentTotal { get; set; }

    public decimal NetPrice { get; set; }

    public decimal EuroNetPrice { get; set; }

    public decimal GrossPrice { get; set; }

    public decimal EuroGrossPrice { get; set; }

    public User User { get; set; }

    public User UpdatedBy { get; set; }

    public User DeletedBy { get; set; }

    public bool IsAccounting { get; set; }

    public bool IsImportedFromOneC { get; set; }

    public ConsumablesOrder ConsumablesOrder { get; set; }

    public ICollection<SupplyOrderPaymentDeliveryProtocol> PaymentDeliveryProtocols { get; set; }

    public ICollection<SupplyOrderPolandPaymentDeliveryProtocol> SupplyOrderPolandPaymentDeliveryProtocols { get; set; }

    public ICollection<ContainerService> ContainerServices { get; set; }

    public ICollection<VehicleService> VehicleServices { get; set; }

    public ICollection<CustomService> BrokerServices { get; set; }

    public ICollection<PortWorkService> PortWorkServices { get; set; }

    public ICollection<TransportationService> TransportationServices { get; set; }

    public ICollection<PortCustomAgencyService> PortCustomAgencyServices { get; set; }

    public ICollection<CustomAgencyService> CustomAgencyServices { get; set; }

    public ICollection<PlaneDeliveryService> PlaneDeliveryServices { get; set; }

    public ICollection<VehicleDeliveryService> VehicleDeliveryServices { get; set; }

    public ICollection<SupplyPaymentTaskDocument> SupplyPaymentTaskDocuments { get; set; }

    public ICollection<OutcomePaymentOrderSupplyPaymentTask> OutcomePaymentOrderSupplyPaymentTasks { get; set; }

    public ICollection<MergedService> MergedServices { get; set; }

    public ICollection<SupplyOrderUkrainePaymentDeliveryProtocol> SupplyOrderUkrainePaymentDeliveryProtocols { get; set; }

    public ICollection<ContainerService> AccountingContainerServices { get; set; }

    public ICollection<VehicleService> AccountingVehicleServices { get; set; }

    public ICollection<CustomService> AccountingBrokerServices { get; set; }

    public ICollection<PortWorkService> AccountingPortWorkServices { get; set; }

    public ICollection<TransportationService> AccountingTransportationServices { get; set; }

    public ICollection<PortCustomAgencyService> AccountingPortCustomAgencyServices { get; set; }

    public ICollection<CustomAgencyService> AccountingCustomAgencyServices { get; set; }

    public ICollection<PlaneDeliveryService> AccountingPlaneDeliveryServices { get; set; }

    public ICollection<VehicleDeliveryService> AccountingVehicleDeliveryServices { get; set; }

    public ICollection<MergedService> AccountingMergedServices { get; set; }

    public ICollection<BillOfLadingService> BillOfLadingServices { get; set; }

    public ICollection<BillOfLadingService> AccountingBillOfLadingServices { get; set; }
}