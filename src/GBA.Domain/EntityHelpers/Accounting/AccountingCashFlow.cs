using System.Collections.Generic;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Supplies;

namespace GBA.Domain.EntityHelpers.Accounting;

public sealed class AccountingCashFlow {
    public AccountingCashFlow() {
        AccountingCashFlowHeadItems = new List<AccountingCashFlowHeadItem>();
    }

    public AccountingCashFlow(Client client) {
        Client = client;

        AccountingCashFlowHeadItems = new List<AccountingCashFlowHeadItem>();
    }

    public AccountingCashFlow(ClientAgreement clientAgreement) {
        ClientAgreement = clientAgreement;

        Client = clientAgreement.Client;

        AccountingCashFlowHeadItems = new List<AccountingCashFlowHeadItem>();
    }

    public AccountingCashFlow(SupplyOrganization supplyOrganization) {
        SupplyOrganization = supplyOrganization;

        AccountingCashFlowHeadItems = new List<AccountingCashFlowHeadItem>();
    }

    public AccountingCashFlow(SupplyOrganizationAgreement supplyOrganizationAgreement) {
        SupplyOrganizationAgreement = supplyOrganizationAgreement;

        SupplyOrganization = supplyOrganizationAgreement.SupplyOrganization;

        AccountingCashFlowHeadItems = new List<AccountingCashFlowHeadItem>();
    }

    public Client Client { get; set; }

    public ClientAgreement ClientAgreement { get; set; }

    public SupplyOrganization SupplyOrganization { get; set; }

    public SupplyOrganizationAgreement SupplyOrganizationAgreement { get; set; }

    public decimal BeforeRangeInAmount { get; set; }

    public decimal BeforeRangeInAmountEuro { get; set; }

    public decimal BeforeRangeOutAmount { get; set; }

    public decimal BeforeRangeOutAmountEuro { get; set; }

    public decimal BeforeRangeBalance { get; set; }

    public decimal BeforeRangeBalanceEuro { get; set; }

    public decimal AfterRangeInAmount { get; set; }

    public decimal AfterRangeInAmountEuro { get; set; }

    public decimal AfterRangeOutAmount { get; set; }

    public decimal AfterRangeOutAmountEuro { get; set; }

    public List<AccountingCashFlowHeadItem> AccountingCashFlowHeadItems { get; set; }
}