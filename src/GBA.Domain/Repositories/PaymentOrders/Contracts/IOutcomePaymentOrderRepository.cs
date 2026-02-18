using System;
using System.Collections.Generic;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.EntityHelpers.TotalDashboards;

namespace GBA.Domain.Repositories.PaymentOrders.Contracts;

public interface IOutcomePaymentOrderRepository {
    long Add(OutcomePaymentOrder outcomePaymentOrder);

    void Update(OutcomePaymentOrder outcomePaymentOrder);

    OutcomePaymentOrder GetById(long id);

    OutcomePaymentOrder GetByIdForSupplies(long id);

    OutcomePaymentOrder GetByIdWithCalculatedAmount(long id);

    OutcomePaymentOrder GetByNetId(Guid netId);

    OutcomePaymentOrder GetLastRecord(PaymentRegisterType type);

    OutcomePaymentOrder GetLastAdvanceRecord();

    Tuple<IEnumerable<OutcomePaymentOrder>, decimal, decimal> GetAll(
        long limit,
        long offset,
        DateTime from,
        DateTime to,
        string value,
        Guid? currencyNetId,
        Guid? registerNetId,
        Guid? paymentMovementNetId,
        long[] organizationIds);

    Tuple<IEnumerable<OutcomePaymentOrder>, decimal, decimal> GetAllUnderReport(
        long limit,
        long offset,
        DateTime from,
        DateTime to,
        string value,
        Guid? currencyNetId,
        Guid? registerNetId,
        Guid? paymentMovementNetId);

    IEnumerable<OutcomePaymentOrder> GetAllByColleagueNetId(Guid colleagueNetId);

    IEnumerable<OutcomePaymentOrder> GetCurrentOutcomesByCompanyCarNetId(Guid netId);

    void SetIsUnderReportDoneById(long id, bool isUnderReportDone);

    TotalDashboardItem GetTotalsAmountByDayAndCurrentMonth();
}