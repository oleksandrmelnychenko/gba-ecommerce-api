using System;
using GBA.Domain.Entities.Supplies.HelperServices;

namespace GBA.Domain.Entities.Supplies.PackingLists;

public sealed class PackingListPackageOrderItemSupplyService : EntityBase {
    public decimal NetValue { get; set; }

    public decimal GeneralValue { get; set; }

    public decimal ManagementValue { get; set; }

    public string Name { get; set; }

    public DateTime ExchangeRateDate { get; set; }

    public long PackingListPackageOrderItemId { get; set; }

    public long CurrencyId { get; set; }

    public long? BillOfLadingServiceId { get; set; }

    public long? ContainerServiceId { get; set; }

    public long? CustomAgencyServiceId { get; set; }

    public long? CustomServiceId { get; set; }

    public long? MergedServiceId { get; set; }

    public long? PlaneDeliveryServiceId { get; set; }

    public long? PortCustomAgencyServiceId { get; set; }

    public long? PortWorkServiceId { get; set; }

    public long? TransportationServiceId { get; set; }

    public long? VehicleDeliveryServiceId { get; set; }

    public long? VehicleServiceId { get; set; }

    public Currency Currency { get; set; }

    public PackingListPackageOrderItem PackingListPackageOrderItem { get; set; }

    public BillOfLadingService BillOfLadingService { get; set; }

    public ContainerService ContainerService { get; set; }

    public CustomAgencyService CustomAgencyService { get; set; }

    public CustomService CustomService { get; set; }

    public MergedService MergedService { get; set; }

    public PlaneDeliveryService PlaneDeliveryService { get; set; }

    public PortCustomAgencyService PortCustomAgencyService { get; set; }

    public PortWorkService PortWorkService { get; set; }

    public TransportationService TransportationService { get; set; }

    public VehicleDeliveryService VehicleDeliveryService { get; set; }

    public VehicleService VehicleService { get; set; }

    public decimal NetValueEur { get; set; }

    public decimal NetValueUah { get; set; }

    public decimal GeneralValueEur { get; set; }

    public decimal GeneralValueUah { get; set; }

    public decimal ManagementValueEur { get; set; }

    public decimal ManagementValueUah { get; set; }

    public decimal TotalNetPriceForServiceEur { get; set; }

    public decimal TotalGeneralPriceForServiceEur { get; set; }

    public decimal TotalManagementPriceForServiceEur { get; set; }

    public decimal TotalNetPriceForServiceUah { get; set; }

    public decimal TotalGeneralPriceForServiceUah { get; set; }

    public decimal TotalManagementPriceForServiceUah { get; set; }
}