using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.DocumentsManagement.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers.DebtorModels;
using GBA.Domain.Messages.Debtors;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Services.Actors.Debtors;

public sealed class DebtorsActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IXlsFactoryManager _xlsFactoryManager;

    public DebtorsActor(
        IXlsFactoryManager xlsFactoryManager,
        IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory) {
        _xlsFactoryManager = xlsFactoryManager;
        _connectionFactory = connectionFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;

        Receive<GetAllDebtorsFilteredMessage>(GetAllDebtorsFilteredAction);

        Receive<ExportClientInDebtDocumentMessage>(ProcessExportClientInDebtDocumentMessage);

        Receive<GetAllFilteredDebtorsByClientMessage>(GetAllFilteredDebtorsByClient);
    }

    private void GetAllDebtorsFilteredAction(GetAllDebtorsFilteredMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(
            _clientRepositoriesFactory
                .NewDebtorRepository(connection)
                .GetAllFiltered(
                    message.Value,
                    message.AllDebtors,
                    message.UserNetId,
                    message.Limit,
                    message.Offset
                )
        );
    }

    private void ProcessExportClientInDebtDocumentMessage(ExportClientInDebtDocumentMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            ClientDebtorsModel clientInDebtors = _clientRepositoriesFactory
                .NewClientInDebtRepository(connection)
                .GetFilteredDebtorsByClientForPrintingDocument(
                    message.UserNetId,
                    message.OrganizationNetId,
                    message.TypeAgreement,
                    message.TypeCurrency);

            (string excelFilePath, string pdfFilePath) =
                _xlsFactoryManager
                    .NewClientXlsManager()
                    .ExportClientInDebtToXlsx(
                        message.PathToFolder,
                        clientInDebtors
                    );

            Sender.Tell(new Tuple<string, string>(excelFilePath, pdfFilePath));
        } catch (Exception) {
            Sender.Tell(new Tuple<string, string>(string.Empty, string.Empty));
        }
    }

    private void GetAllFilteredDebtorsByClient(GetAllFilteredDebtorsByClientMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IClientInDebtRepository clientInDebtRepository = _clientRepositoriesFactory.NewClientInDebtRepository(connection);
            IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);
            ClientDebtorsModel clientDebtorsModel = _clientRepositoriesFactory
                .NewClientInDebtRepository(connection)
                .GetFilteredDebtorsByClientInfo(
                    message.UserNetId,
                    message.OrganizationNetId,
                    message.TypeAgreement,
                    message.TypeCurrency,
                    message.Limit,
                    message.Offset);

            DateTime dateAfterDays = DateTime.Now.AddDays(message.Days);

            foreach (ClientInDebtModel ClientInDebtors in clientDebtorsModel.ClientInDebtors) {
                IEnumerable<Debt> includedDebts = new List<Debt>();

                foreach (Guid item in ClientInDebtors.ClientAgreementNetId) {
                    ClientAgreement clientAgreement = clientAgreementRepository.GetByNetId(item);

                    includedDebts = ClientInDebtors.debts.Where(d => dateAfterDays.Subtract(d.Created.AddDays(clientAgreement.Agreement.NumberDaysDebt)).Days >= 0);
                }

                ClientInDebtors.TotalDebtInDays += Math.Round(includedDebts.Sum(d => d.Total), 4, MidpointRounding.AwayFromZero);
            }

            Sender.Tell(
                clientDebtorsModel
            );
        } catch (Exception) {
            Sender.Tell(new ClientDebtorsModel());
        }
    }
}