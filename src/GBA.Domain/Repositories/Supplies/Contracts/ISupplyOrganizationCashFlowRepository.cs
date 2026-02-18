using System;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.EntityHelpers.Accounting;
using GBA.Domain.EntityHelpers.Supplies;

namespace GBA.Domain.Repositories.Supplies.Contracts;

public interface ISupplyOrganizationCashFlowRepository {
    AccountingCashFlow GetRangedBySupplyOrganization(SupplyOrganization supplyOrganization, DateTime from, DateTime to, TypePaymentTask typePaymentTask);

    AccountingCashFlow GetRangedBySupplyOrganizationAgreement(SupplyOrganizationAgreement preDefinedAgreement, DateTime from, DateTime to, TypePaymentTask typePaymentTask);
}