using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Entities.Supplies;

public sealed class SupplyOrganizationAgreement : EntityBase {
    public SupplyOrganizationAgreement() {
        SupplyOrganizationDocuments = new HashSet<SupplyOrganizationDocument>();

        ContainerServices = new HashSet<ContainerService>();

        CustomAgencyServices = new HashSet<CustomAgencyService>();

        CustomServices = new HashSet<CustomService>();

        PlaneDeliveryServices = new HashSet<PlaneDeliveryService>();

        PortCustomAgencyServices = new HashSet<PortCustomAgencyService>();

        PortWorkServices = new HashSet<PortWorkService>();

        TransportationServices = new HashSet<TransportationService>();

        VehicleDeliveryServices = new HashSet<VehicleDeliveryService>();

        ConsumablesOrderItems = new HashSet<ConsumablesOrderItem>();

        OutcomePaymentOrders = new HashSet<OutcomePaymentOrder>();

        MergedServices = new HashSet<MergedService>();

        VehicleServices = new HashSet<VehicleService>();

        BillOfLadingServices = new HashSet<BillOfLadingService>();

        IncomePaymentOrders = new HashSet<IncomePaymentOrder>();

        DeliveryExpenses = new HashSet<DeliveryExpense>();
    }

    public string Name { get; set; }

    public decimal CurrentAmount { get; set; }

    public decimal AccountingCurrentAmount { get; set; }

    public decimal CurrentEuroAmount { get; set; }

    public long SupplyOrganizationId { get; set; }

    public long CurrencyId { get; set; }

    public long? TaxAccountingSchemeId { get; set; }

    public long? AgreementTypeCivilCodeId { get; set; }

    public byte[] SourceAmgId { get; set; }

    public byte[] SourceFenixId { get; set; }

    public long? SourceAmgCode { get; set; }

    public long? SourceFenixCode { get; set; }

    public DateTime ExistTo { get; set; }

    public DateTime ExistFrom { get; set; }

    public long OrganizationId { get; set; }

    public string Number { get; set; }

    public Organization Organization { get; set; }

    public SupplyOrganization SupplyOrganization { get; set; }

    public Currency Currency { get; set; }

    public TaxAccountingScheme TaxAccountingScheme { get; set; }

    public AgreementTypeCivilCode AgreementTypeCivilCode { get; set; }

    public ICollection<ContainerService> ContainerServices { get; set; }

    public ICollection<VehicleService> VehicleServices { get; set; }

    public ICollection<CustomAgencyService> CustomAgencyServices { get; set; }

    public ICollection<CustomService> CustomServices { get; set; }

    public ICollection<PlaneDeliveryService> PlaneDeliveryServices { get; set; }

    public ICollection<PortCustomAgencyService> PortCustomAgencyServices { get; set; }

    public ICollection<PortWorkService> PortWorkServices { get; set; }

    public ICollection<TransportationService> TransportationServices { get; set; }

    public ICollection<VehicleDeliveryService> VehicleDeliveryServices { get; set; }

    public ICollection<ConsumablesOrderItem> ConsumablesOrderItems { get; set; }

    public ICollection<OutcomePaymentOrder> OutcomePaymentOrders { get; set; }

    public ICollection<MergedService> MergedServices { get; set; }

    public ICollection<BillOfLadingService> BillOfLadingServices { get; set; }

    public ICollection<SupplyOrganizationDocument> SupplyOrganizationDocuments { get; set; }

    public ICollection<IncomePaymentOrder> IncomePaymentOrders { get; set; }

    public ICollection<CompanyCarFueling> CompanyCarFuelings { get; set; }

    public ICollection<DeliveryExpense> DeliveryExpenses { get; set; }

    public bool SourceIdsEqual(byte[] sourceId) {
        return SourceAmgId != null && sourceId != null && SourceAmgId.SequenceEqual(sourceId) ||
               SourceFenixId != null && sourceId != null && SourceFenixId.SequenceEqual(sourceId);
    }
}