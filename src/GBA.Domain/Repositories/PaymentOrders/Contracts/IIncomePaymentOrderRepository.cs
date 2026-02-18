using System;
using System.Collections.Generic;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.EntityHelpers.SalesModels.ChartOfSalesModels;
using GBA.Domain.EntityHelpers.TotalDashboards;
using GBA.Domain.EntityHelpers.TotalDashboards.Charts;

namespace GBA.Domain.Repositories.PaymentOrders.Contracts;

public interface IIncomePaymentOrderRepository {
    long Add(IncomePaymentOrder incomeCashOrder);

    void Update(IncomePaymentOrder incomeCashOrder);

    void UpdateExchangeRateById(long id, decimal exchangeRate);

    void UpdateOverpaidAmountById(long id, decimal overpaidAmount);

    void UpdateAgreementId(IncomePaymentOrder incomePaymentOrder);

    IncomePaymentOrder GetById(long id);

    IncomePaymentOrder GetByIdWithCalculatedAmount(long id);

    IncomePaymentOrder GetByNetId(Guid netId);

    IncomePaymentOrder GetByNetIdReversed(Guid netId);

    IncomePaymentOrder GetLastRecord(PaymentRegisterType type);

    IncomePaymentOrder GetLastIncomeForSalesByClientId(long clientId);

    IncomePaymentOrder GetLastBySaleId(long saleId);

    IEnumerable<IncomePaymentOrder> GetAll(long limit, long offset, DateTime from, DateTime to, string value, Guid? currencyNetId, Guid? registerNetId, long[] organizationIds);

    IEnumerable<IncomePaymentOrder> GetAll(long registerId, long limit, long offset, DateTime from, DateTime to, string value, Guid? currencyNetId);

    void Remove(Guid netId);

    TotalDashboardItem GetTotalsAmountByDayAndCurrentMonth();

    IEnumerable<GroupedPaymentsByPeriod> GetFilteredGroupedPaymentsByPeriod(
        DateTime from,
        DateTime to,
        TypePeriodGrouping period,
        Guid? netId);
}