using System;
using System.Collections.Generic;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Consumables;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.EntityHelpers.GbaDataExportModels;

public sealed class SupplyOrganizationAgreementDto {
    public Guid NetUid { get; set; }
    
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

    public DateTime ToDate { get; set; }

    public DateTime FromDate { get; set; }

    public long OrganizationId { get; set; }

    public string Number { get; set; }

    public Organization Organization { get; set; }

    public SupplyOrganization SupplyOrganization { get; set; }

    public CurrencyDto Currency { get; set; }

    public TaxAccountingScheme TaxAccountingScheme { get; set; }

    public AgreementTypeCivilCode AgreementTypeCivilCode { get; set; }

    public List<ContainerService> ContainerServices { get; set; }

    public List<VehicleService> VehicleServices { get; set; }

    public List<CustomAgencyService> CustomAgencyServices { get; set; }

    public List<CustomService> CustomServices { get; set; }

    public List<PlaneDeliveryService> PlaneDeliveryServices { get; set; }

    public List<PortCustomAgencyService> PortCustomAgencyServices { get; set; }

    public List<PortWorkService> PortWorkServices { get; set; }

    public List<TransportationService> TransportationServices { get; set; }

    public List<VehicleDeliveryService> VehicleDeliveryServices { get; set; }

    public List<ConsumablesOrderItem> ConsumablesOrderItems { get; set; }

    public List<OutcomePaymentOrder> OutcomePaymentOrders { get; set; }

    public List<MergedService> MergedServices { get; set; }

    public List<BillOfLadingService> BillOfLadingServices { get; set; }

    public List<SupplyOrganizationDocument> SupplyOrganizationDocuments { get; set; }

    public List<IncomePaymentOrder> IncomePaymentOrders { get; set; }

    public List<CompanyCarFueling> CompanyCarFuelings { get; set; }

    public List<DeliveryExpense> DeliveryExpenses { get; set; }
}