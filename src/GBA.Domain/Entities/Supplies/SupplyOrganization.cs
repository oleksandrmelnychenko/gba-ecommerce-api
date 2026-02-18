using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Entities.Supplies;

public sealed class SupplyOrganization : EntityBase {
    public SupplyOrganization() {
        ContainerServices = new HashSet<ContainerService>();

        CustomAgencyServices = new HashSet<CustomAgencyService>();

        CustomServices = new HashSet<CustomService>();

        ExciseDutyCustomServices = new HashSet<CustomService>();

        PlaneDeliveryServices = new HashSet<PlaneDeliveryService>();

        PortCustomAgencyServices = new HashSet<PortCustomAgencyService>();

        PortWorkServices = new HashSet<PortWorkService>();

        TransportationServices = new HashSet<TransportationService>();

        VehicleDeliveryServices = new HashSet<VehicleDeliveryService>();

        ConsumablesOrderItems = new HashSet<ConsumablesOrderItem>();

        OutcomePaymentOrders = new HashSet<OutcomePaymentOrder>();

        CompanyCarFuelings = new HashSet<CompanyCarFueling>();

        SupplyOrganizationAgreements = new HashSet<SupplyOrganizationAgreement>();

        MergedServices = new HashSet<MergedService>();

        VehicleServices = new HashSet<VehicleService>();

        BillOfLadingServices = new HashSet<BillOfLadingService>();

        IncomePaymentOrders = new HashSet<IncomePaymentOrder>();

        DeliveryExpenses = new HashSet<DeliveryExpense>();
    }

    public string Name { get; set; }

    public string Address { get; set; }

    public string PhoneNumber { get; set; }

    public string EmailAddress { get; set; }

    public string Requisites { get; set; }

    public string Swift { get; set; }

    public string SwiftBic { get; set; }

    public string IntermediaryBank { get; set; }

    public string BeneficiaryBank { get; set; }

    public string AccountNumber { get; set; }

    public string Beneficiary { get; set; }

    public string Bank { get; set; }

    public string BankAccount { get; set; }

    public string NIP { get; set; }

    public string BankAccountPLN { get; set; }

    public string BankAccountEUR { get; set; }

    public string ContactPersonName { get; set; }

    public string ContactPersonPhone { get; set; }

    public string ContactPersonEmail { get; set; }

    public string ContactPersonViber { get; set; }

    public string ContactPersonSkype { get; set; }

    public string ContactPersonComment { get; set; }

    public bool IsAgreementReceived { get; set; }

    public bool IsBillReceived { get; set; }

    public DateTime? AgreementReceiveDate { get; set; }

    public DateTime? BillReceiveDate { get; set; }

    public long? SourceAmgCode { get; set; }

    public long? SourceFenixCode { get; set; }

    public byte[] SourceAmgId { get; set; }

    public byte[] SourceFenixId { get; set; }

    public string OriginalRegionCode { get; set; }

    public bool IsNotResident { get; set; }

    public string TIN { get; set; }

    public string USREOU { get; set; }

    public string SROI { get; set; }

    public ICollection<ContainerService> ContainerServices { get; set; }

    public ICollection<VehicleService> VehicleServices { get; set; }

    public ICollection<CustomAgencyService> CustomAgencyServices { get; set; }

    public ICollection<CustomService> CustomServices { get; set; }

    public ICollection<CustomService> ExciseDutyCustomServices { get; set; }

    public ICollection<PlaneDeliveryService> PlaneDeliveryServices { get; set; }

    public ICollection<PortCustomAgencyService> PortCustomAgencyServices { get; set; }

    public ICollection<PortWorkService> PortWorkServices { get; set; }

    public ICollection<TransportationService> TransportationServices { get; set; }

    public ICollection<VehicleDeliveryService> VehicleDeliveryServices { get; set; }

    public ICollection<ConsumablesOrderItem> ConsumablesOrderItems { get; set; }

    public ICollection<OutcomePaymentOrder> OutcomePaymentOrders { get; set; }

    public ICollection<CompanyCarFueling> CompanyCarFuelings { get; set; }

    public ICollection<SupplyOrganizationAgreement> SupplyOrganizationAgreements { get; set; }

    public ICollection<MergedService> MergedServices { get; set; }

    public ICollection<BillOfLadingService> BillOfLadingServices { get; set; }

    public ICollection<IncomePaymentOrder> IncomePaymentOrders { get; set; }

    public ICollection<DeliveryExpense> DeliveryExpenses { get; set; }

    public decimal TotalAgreementsCurrentAmount { get; set; }

    public decimal TotalAgreementsCurrentEuroAmount { get; set; }

    public bool SourceIdsEqual(byte[] sourceId) {
        return SourceAmgId != null && sourceId != null && SourceAmgId.SequenceEqual(sourceId) ||
               SourceFenixId != null && sourceId != null && SourceFenixId.SequenceEqual(sourceId);
    }
}