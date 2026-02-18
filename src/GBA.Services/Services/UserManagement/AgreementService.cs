using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers.Agreements;
using GBA.Domain.EntityHelpers.DebtorModels;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Services.Services.UserManagement.Contracts;

namespace GBA.Services.Services.UserManagement;

public sealed class AgreementService : IAgreementService {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;

    public AgreementService(IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
    }

    public Task<ClientAgreementsWithTotalDebtModel> GetAllAgreementsByClientNetId(Guid netId) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        dynamic data = _clientRepositoriesFactory.NewClientRepository(connection).GetDebtTotalsForClientStructureWithRootByClientNetId(netId);

        List<ClientAgreement> clientAgreements = _clientRepositoriesFactory.NewClientAgreementRepository(connection).GetAllByClientNetId(netId);

        foreach (ClientAgreement clientAgreement in clientAgreements) {
            ClientInDebt clientInDebt = clientAgreement.Agreement.ClientInDebts.OrderBy(c => c.Created).FirstOrDefault();

            clientAgreement.Agreement.ExpiredDays = clientInDebt != null
                ? DateTime.Now.Subtract(clientInDebt.Created.AddDays(clientAgreement.Agreement.NumberDaysDebt)).Days
                : 0;
        }

        ClientAgreementsWithTotalDebtModel toReturn = new() {
            ClientAgreements = clientAgreements,
            TotalEuro = data.TotalEuro ?? 0m,
            TotalLocal = data.TotalLocal ?? 0m
        };

        return Task.FromResult(toReturn);
    }

    public Task<DebtAfterDaysModel> GetDebtAfterDaysByClientAgreementNetId(Guid netId, int days) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IClientInDebtRepository clientInDebtRepository = _clientRepositoriesFactory.NewClientInDebtRepository(connection);
        IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);

        List<Debt> debts = clientInDebtRepository.GetDebtByClientAgreementNetId(netId);

        if (!debts.Any()) return Task.FromResult(new DebtAfterDaysModel(0, netId));

        ClientAgreement clientAgreement = clientAgreementRepository.GetByNetId(netId);

        DateTime dateAfterDays = DateTime.Now.AddDays(days);

        IEnumerable<Debt> includedDebts = debts.Where(d => dateAfterDays.Subtract(d.Created.AddDays(clientAgreement.Agreement.NumberDaysDebt)).Days >= 0);

        return Task.FromResult(new DebtAfterDaysModel(decimal.Round(includedDebts.Sum(d => d.Total), 2, MidpointRounding.AwayFromZero), netId));
    }
}
