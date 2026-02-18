using System;
using System.Threading.Tasks;
using GBA.Domain.EntityHelpers.Agreements;
using GBA.Domain.EntityHelpers.DebtorModels;

namespace GBA.Services.Services.UserManagement.Contracts;

public interface IAgreementService {
    Task<ClientAgreementsWithTotalDebtModel> GetAllAgreementsByClientNetId(Guid netId);
    Task<DebtAfterDaysModel> GetDebtAfterDaysByClientAgreementNetId(Guid netId, int days);
}