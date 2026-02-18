using System;
using System.Collections.Generic;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.EntityHelpers.TotalDashboards.PaymentRegisters;

namespace GBA.Domain.Repositories.PaymentOrders.Contracts;

public interface IPaymentRegisterRepository {
    long Add(PaymentRegister paymentRegister);

    void Update(PaymentRegister paymentRegister);

    void Remove(Guid netId);

    void SetActiveById(long id);

    void SetInactiveByOrganizationAndCurrencyIds(long organizationId, long currencyId);

    void SetSelectedByNetId(Guid netId);

    void DeselectByNetId(Guid netId);

    PaymentRegister GetIsSelected();

    PaymentRegister GetById(long id);

    PaymentRegister GetByNetId(Guid netId);

    PaymentRegister GetByNetIdWithoutIncludes(Guid netId);

    PaymentRegister GetActiveBankAccountByCurrencyAndOrganizationIds(long currencyId, long organizationId);

    List<PaymentRegister> GetAllFromSearch(string value);

    List<PaymentRegister> GetAllForRetail(Guid? organizationNetUid);

    List<PaymentRegister> GetAll(PaymentRegisterType? type, string value, Guid? organizationNetId);

    PaymentCurrencyRegisterModel GetFilteredMovementsByPaymentRegisterNetId(
        Guid netId,
        TypeFilteredMovements type,
        DateTime from,
        DateTime to,
        int limit,
        int offset);

    Dictionary<string, decimal> GetTotalBalanceByCurrency();

    TotalCurrencyRegisters GetStatePaymentByPeriod(DateTime from, DateTime to);

    void UpdateAllNotMainByOrganizationId(long id);

    void UpdateIsMainById(long id);

    PaymentRegister GetMainPaymentRegisterByOrganization(long organizationId);

    List<PaymentRegister> GetAllByBank(string bank);
}