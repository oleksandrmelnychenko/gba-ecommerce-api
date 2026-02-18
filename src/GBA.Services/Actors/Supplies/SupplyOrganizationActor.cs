using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Supplies.Organizations;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Services.Actors.Supplies;

public sealed class SupplyOrganizationActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;

    public SupplyOrganizationActor(
        IDbConnectionFactory connectionFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;
        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;

        Receive<AddNewSupplyOrganizationMessage>(ProcessAddNewSupplyOrganizationMessage);

        Receive<UpdateSupplyOrganizationMessage>(ProcessUpdateSupplyOrganizationMessage);

        Receive<AddNewSupplyOrganizationAgreementMessage>(ProcessAddNewSupplyOrganizationAgreementMessage);

        Receive<UpdateSupplyOrganizationAgreementMessage>(ProcessUpdateSupplyOrganizationAgreementMessage);

        Receive<DeleteSupplyOrganizationByNetIdMessage>(ProcessDeleteSupplyOrganizationByNetIdMessage);
    }

    private void ProcessAddNewSupplyOrganizationMessage(AddNewSupplyOrganizationMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISupplyOrganizationRepository supplyOrganizationRepository = _supplyRepositoriesFactory.NewSupplyOrganizationRepository(connection);
        ISupplyOrganizationDocumentRepository supplyOrganizationDocumentRepository = _supplyRepositoriesFactory.NewSupplyOrganizationDocumentRepository(connection);
        ISupplyOrganizationAgreementRepository supplyOrganizationAgreementRepository = _supplyRepositoriesFactory.NewSupplyOrganizationAgreementRepository(connection);

        message.SupplyOrganization.Id = supplyOrganizationRepository.Add(message.SupplyOrganization);

        Sender.Tell(supplyOrganizationRepository.GetById(message.SupplyOrganization.Id));
    }

    private void ProcessUpdateSupplyOrganizationMessage(UpdateSupplyOrganizationMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISupplyOrganizationRepository supplyOrganizationRepository = _supplyRepositoriesFactory.NewSupplyOrganizationRepository(connection);

        supplyOrganizationRepository.Update(message.SupplyOrganization);

        Sender.Tell(supplyOrganizationRepository.GetById(message.SupplyOrganization.Id));
    }

    private void ProcessAddNewSupplyOrganizationAgreementMessage(AddNewSupplyOrganizationAgreementMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISupplyOrganizationDocumentRepository supplyOrganizationDocumentRepository =
            _supplyRepositoriesFactory.NewSupplyOrganizationDocumentRepository(connection);
        ISupplyOrganizationAgreementRepository supplyOrganizationAgreementRepository =
            _supplyRepositoriesFactory.NewSupplyOrganizationAgreementRepository(connection);

        message.Agreement.OrganizationId = message.Agreement.Organization.Id;
        message.Agreement.CurrencyId = message.Agreement.Currency.Id;

        long agreementId = supplyOrganizationAgreementRepository.Add(message.Agreement);

        supplyOrganizationDocumentRepository.Add(
            message.Agreement.SupplyOrganizationDocuments.Select(document => {
                document.SupplyOrganizationAgreementId = agreementId;

                return document;
            }));

        Sender.Tell(supplyOrganizationAgreementRepository.GetById(agreementId));
    }

    private void ProcessUpdateSupplyOrganizationAgreementMessage(
        UpdateSupplyOrganizationAgreementMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISupplyOrganizationAgreementRepository supplyOrganizationAgreementRepository =
            _supplyRepositoriesFactory.NewSupplyOrganizationAgreementRepository(connection);
        ISupplyOrganizationDocumentRepository supplyOrganizationDocumentRepository =
            _supplyRepositoriesFactory.NewSupplyOrganizationDocumentRepository(connection);

        message.Agreement.OrganizationId = message.Agreement.Organization.Id;
        message.Agreement.CurrencyId = message.Agreement.Currency.Id;

        supplyOrganizationAgreementRepository.Update(message.Agreement);

        supplyOrganizationDocumentRepository.RemoveAllByIds(
            message.Agreement.SupplyOrganizationDocuments
                .Where(x => x.Deleted)
                .Select(x => x.Id)
        );

        if (message.Agreement.Deleted) supplyOrganizationDocumentRepository.RemoveAllBySupplyOrganizationAgreementId(message.Agreement.Id);

        supplyOrganizationDocumentRepository.Add(
            message.Agreement.SupplyOrganizationDocuments.Where(x => x.IsNew() && !x.Deleted).Select(document => {
                document.SupplyOrganizationAgreementId = message.Agreement.Id;

                return document;
            })
        );

        Sender.Tell(supplyOrganizationAgreementRepository.GetById(message.Agreement.Id));
    }

    private void ProcessDeleteSupplyOrganizationByNetIdMessage(DeleteSupplyOrganizationByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _supplyRepositoriesFactory.NewSupplyOrganizationRepository(connection).Remove(message.NetId);
    }
}