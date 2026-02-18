using System;
using System.Data;
using System.Threading.Tasks;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Repositories.Agreements.Contracts;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.Organizations.Contracts;
using GBA.Domain.Repositories.Pricings.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Services.Services.Clients.Contracts;

namespace GBA.Services.Services.Clients;

public sealed class ClientAgreementService : IClientAgreementService {
    private readonly IAgreementRepositoriesFactory _agreementRepositoriesFactory;
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IOrganizationRepositoriesFactory _organizationRepositoriesFactory;
    private readonly IPricingRepositoriesFactory _pricingRepositoriesFactory;
    private readonly IStorageRepositoryFactory _storageRepositoryFactory;

    public ClientAgreementService(
        IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IOrganizationRepositoriesFactory organizationRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IPricingRepositoriesFactory pricingRepositoriesFactory,
        IAgreementRepositoriesFactory agreementRepositoriesFactory,
        IStorageRepositoryFactory storageRepositoryFactory) {
        _connectionFactory = connectionFactory;

        _clientRepositoriesFactory = clientRepositoriesFactory;

        _organizationRepositoriesFactory = organizationRepositoriesFactory;

        _currencyRepositoriesFactory = currencyRepositoriesFactory;

        _pricingRepositoriesFactory = pricingRepositoriesFactory;

        _agreementRepositoriesFactory = agreementRepositoriesFactory;
        _storageRepositoryFactory = storageRepositoryFactory;
    }

    public Task AddDefaultAgreementForClient(Client client, bool isLocalPayment) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IAgreementRepository agreementRepository = _agreementRepositoriesFactory.NewAgreementRepository(connection);
            //Agreement defaultAgreement = agreementRepository.GetDefaultByCulture();

            Storage storage = _storageRepositoryFactory.NewStorageRepository(connection).GetWithHighestPriority();

            ClientAgreement retailClientAgreement =
                _clientRepositoriesFactory.NewClientAgreementRepository(connection)
                    .GetByClientNetIdWithOrWithoutVat(
                        _clientRepositoriesFactory.NewClientRepository(connection).GetRetailClient().NetUid,
                        storage.OrganizationId.Value,
                        storage.ForVatProducts);

            Agreement defaultAgreement = retailClientAgreement.Agreement;
            defaultAgreement.Id = 0;
            defaultAgreement.NetUid = Guid.Empty;
            defaultAgreement.Name = "������ Concord";
            defaultAgreement.IsSelected = true;
            defaultAgreement.Created = DateTime.Now;
            defaultAgreement.Updated = DateTime.Now;
            defaultAgreement.FromDate = DateTime.Now;
            defaultAgreement.ToDate = DateTime.Now.AddYears(1);

            Organization organization = defaultAgreement.Organization;

            Agreement lastRecord = agreementRepository.GetLastRecordByOrganizationId(organization.Id);

            defaultAgreement.Number = organization.Code;

            try {
                if (lastRecord != null && !string.IsNullOrEmpty(lastRecord.Number))
                    defaultAgreement.Number +=
                        string.Format(
                            "{0:D5}",
                            Convert.ToInt64(
                                lastRecord
                                    .Number
                                    .Substring(
                                        organization.Code.Length,
                                        lastRecord.Number.Length - organization.Code.Length)
                            ) + 1);
                else
                    defaultAgreement.Number += string.Format(
                        "{0:D5}",
                        1
                    );
            } catch (FormatException) {
                defaultAgreement.Number += string.Format(
                    "{0:D5}",
                    1
                );
            }

            // defaultAgreement = new Agreement {
            //     IsControlAmountDebt = true,
            //     IsPrePayment = true,
            //     IsPrePaymentFull = true,
            //     NumberDaysDebt = 0,
            //     IsActive = true,
            //     IsDefault = true,
            //     PrePaymentPercentages = 100,
            //     IsAccounting = true,
            //     Name = $"������ {CultureInfo.CurrentCulture.TwoLetterISOLanguageName}",
            //     IsControlNumberDaysDebt = true,
            //     IsManagementAccounting = true,
            //     WithVATAccounting = isLocalPayment,
            //     AmountDebt = 0,
            //     CurrencyId = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
            //         ? _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetUAHCurrencyIfExists()?.Id
            //         : _currencyRepositoriesFactory.NewCurrencyRepository(connection).GetEURCurrencyIfExists()?.Id,
            //     PricingId = _pricingRepositoriesFactory.NewPricingRepository(connection).GetPricingByCurrentCultureWithHighestExtraCharge()?.Id,
            //     IsSelected = true,
            //     OrganizationId = retailClientAgreement.Agreement.OrganizationId
            // };

            //agreementRepository.Add(defaultAgreement);

            _clientRepositoriesFactory.NewClientAgreementRepository(connection).Add(new ClientAgreement {
                AgreementId = agreementRepository.Add(defaultAgreement),
                ClientId = client.Id,
                ProductReservationTerm = 3
            });

        return Task.CompletedTask;
    }

    public Task<Client> UpdateSelectedClientAgreement(Guid clientNetId, Guid clientAgreementNetId) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IAgreementRepository agreementRepository = _agreementRepositoriesFactory.NewAgreementRepository(connection);
        IClientRepository clientRepository = _clientRepositoriesFactory.NewClientRepository(connection);

            Client client = clientRepository.GetByNetId(clientNetId, true);
            if (client != null) {
                foreach (ClientAgreement clientAgreement in client.ClientAgreements) {
                    clientAgreement.Agreement.IsSelected = clientAgreement.NetUid.Equals(clientAgreementNetId);

                    agreementRepository.Update(clientAgreement.Agreement);
                }
            } else {
                IWorkplaceRepository workplaceRepository = _clientRepositoriesFactory.NewWorkplaceRepository(connection);

                Workplace workplace = workplaceRepository.GetByNetIdWithClient(clientNetId);
                client = workplace.MainClient;
                client.CurrentWorkplace = workplace;

                foreach (WorkplaceClientAgreement workplaceClientAgreement in workplace.WorkplaceClientAgreements) {
                    workplaceClientAgreement.IsSelected = workplaceClientAgreement.ClientAgreement.NetUid.Equals(clientAgreementNetId);

                    workplaceRepository.UpdateWorkplaceClientAgreement(workplaceClientAgreement);
                }
            }

        return Task.FromResult(client);
    }
}
