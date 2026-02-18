using System;
using System.Collections.Generic;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Domain.EntityHelpers.Agreements;
using GBA.Domain.Messages.Agreements;
using GBA.Domain.Repositories.Agreements.Contracts;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.DocumentMonths.Contracts;

namespace GBA.Services.Actors.Agreements.AgreementsGetActors;

public sealed class BaseAgreementsGetActor : ReceiveActor {
    private readonly IAgreementRepositoriesFactory _agreementRepositoriesFactory;
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IDocumentMonthRepositoryFactory _documentMonthRepositoryFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public BaseAgreementsGetActor(
        IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IAgreementRepositoriesFactory agreementRepositoriesFactory,
        IXlsFactoryManager xlsFactoryManager,
        IDocumentMonthRepositoryFactory documentMonthRepositoryFactory) {
        _connectionFactory = connectionFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _agreementRepositoriesFactory = agreementRepositoriesFactory;
        _xlsFactoryManager = xlsFactoryManager;
        _documentMonthRepositoryFactory = documentMonthRepositoryFactory;

        Receive<GetAllAgreementsMessage>(ProcessGetAllAgreementsMessage);

        Receive<GetAllAgreementsByClientNetIdMessage>(ProcessGetAllAgreementsByClientNetIdMessage);

        Receive<GetAllAgreementsByRetailClientMessage>(ProcessGetAllAgreementsByRetailClientMessage);

        Receive<GetAllAgreementsByClientNetIdGroupedMessage>(ProcessGetAllAgreementsByClientNetIdGroupedMessage);

        Receive<GetAgreementByNetIdMessage>(ProcessGetAgreementByNetIdMessage);

        Receive<GetAllTaxAccountingSchemeMessage>(ProcessGetAllTaxAccountingSchemeMessage);

        Receive<GetAllAgreementTypeCivilCodeMessage>(ProcessGetAllAgreementTypeCivilCodeMessage);

        Receive<GetAgreementDocumentByAgreementNetIdMessage>(ProcessGetAgreementDocumentByAgreementNetIdMessage);
    }

    private void ProcessGetAllAgreementsByRetailClientMessage(GetAllAgreementsByRetailClientMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_clientRepositoriesFactory.NewClientAgreementRepository(connection).GetAllByRetailClientNetId(message.NetId));
    }

    private void ProcessGetAllAgreementsMessage(GetAllAgreementsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_agreementRepositoriesFactory.NewAgreementRepository(connection).GetAll());
    }

    private void ProcessGetAllAgreementsByClientNetIdMessage(GetAllAgreementsByClientNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        List<ClientAgreement> clientAgreements = _clientRepositoriesFactory.NewClientAgreementRepository(connection).GetAllByClientNetId(message.NetId);

        foreach (ClientAgreement clientAgreement in clientAgreements)
            clientAgreement.AccountBalance = _clientRepositoriesFactory.NewClientCashFlowRepository(connection)
                .GetAccountBalanceByClientAgreement(
                    clientAgreement.Id,
                    clientAgreement.Agreement.Currency != null && clientAgreement.Agreement.Currency.Code.ToUpper().Equals("EUR"));

        Sender.Tell(clientAgreements);
    }

    private void ProcessGetAllAgreementsByClientNetIdGroupedMessage(GetAllAgreementsByClientNetIdGroupedMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_clientRepositoriesFactory.NewClientAgreementRepository(connection).GetAllByClientNetIdGrouped(message.NetId));
    }

    private void ProcessGetAgreementByNetIdMessage(GetAgreementByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_agreementRepositoriesFactory.NewAgreementRepository(connection).GetByNetId(message.NetId));
    }

    private void ProcessGetAllTaxAccountingSchemeMessage(GetAllTaxAccountingSchemeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_agreementRepositoriesFactory.NewAgreementRepository(connection).GetAllTaxAccountingScheme());
    }

    private void ProcessGetAllAgreementTypeCivilCodeMessage(GetAllAgreementTypeCivilCodeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_agreementRepositoriesFactory.NewAgreementRepository(connection).GetAllAgreementTypeCivilCodeMessage());
    }

    private void ProcessGetAgreementDocumentByAgreementNetIdMessage(GetAgreementDocumentByAgreementNetIdMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ClientAgreement agreement =
                _clientRepositoriesFactory
                    .NewClientAgreementRepository(connection)
                    .GetWithClientInfoByAgreementNetId(message.NetId);

            string wordFile;

            if (message.DocumentType.Equals(AgreementDownloadDocumentType.WarrantyConditions))
                wordFile = _xlsFactoryManager
                    .NewAgreementXlsManager()
                    .ExportWarrantyConditionsToDoc(
                        message.Path,
                        agreement,
                        _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk"));
            else
                wordFile = _xlsFactoryManager
                    .NewAgreementXlsManager()
                    .ExportAgreementToDoc(
                        message.Path,
                        agreement,
                        _documentMonthRepositoryFactory.NewDocumentMonthRepository(connection).GetAllByCulture("uk"));

            Sender.Tell(new Tuple<string, string>(wordFile, string.Empty));
        } catch {
            Sender.Tell(new Tuple<string, string>(string.Empty, string.Empty));
        }
    }
}