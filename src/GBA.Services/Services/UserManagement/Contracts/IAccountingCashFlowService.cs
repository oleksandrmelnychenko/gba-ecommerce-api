using System;
using System.Threading.Tasks;
using GBA.Domain.EntityHelpers.Accounting;

namespace GBA.Services.Services.UserManagement.Contracts;

public interface IAccountingCashFlowService {
    Task<AccountingCashFlow> GetAccountingCashFlow(Guid netId, DateTime from, DateTime to);
}