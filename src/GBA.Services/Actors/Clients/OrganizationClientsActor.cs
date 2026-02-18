using System;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Common.ResourceNames;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Clients.OrganizationClients;
using GBA.Domain.Messages.Clients.OrganizationClients;
using GBA.Domain.Repositories.Clients.OrganizationClients.Contracts;

namespace GBA.Services.Actors.Clients;

public sealed class OrganizationClientsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IOrganizationClientRepositoriesFactory _organizationClientRepositoriesFactory;

    public OrganizationClientsActor(
        IDbConnectionFactory connectionFactory,
        IOrganizationClientRepositoriesFactory organizationClientRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _organizationClientRepositoriesFactory = organizationClientRepositoriesFactory;

        Receive<AddNewOrganizationClientMessage>(ProcessAddNewOrganizationClientMessage);

        Receive<UpdateOrganizationClientMessage>(ProcessUpdateOrganizationClientMessage);

        Receive<RemoveOrganizationClientByNetIdMessage>(ProcessRemoveOrganizationClientByNetIdMessage);
    }

    private void ProcessAddNewOrganizationClientMessage(AddNewOrganizationClientMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.Client == null) throw new Exception("OrganizationClient entity can not be null or empty");
            if (!message.Client.IsNew()) throw new Exception("Existing OrganizationClient is not valid payload for current request");
            if (!message.Client.OrganizationClientAgreements.Any()) throw new Exception(OrganizationClientResourceNames.NEED_ADD_ONE_AGREEMENT);
            if (!message.Client.OrganizationClientAgreements.All(a => a.Currency != null && !a.Currency.IsNew()))
                throw new Exception(OrganizationClientResourceNames.NEED_SPECIFY_CURRENCY_FOR_ALL_AGREEMENTS);

            IOrganizationClientRepository organizationClientRepository = _organizationClientRepositoriesFactory.NewOrganizationClientRepository(connection);
            IOrganizationClientAgreementRepository organizationClientAgreementRepository =
                _organizationClientRepositoriesFactory.OrganizationClientAgreementRepository(connection);

            message.Client.Id = organizationClientRepository.Add(message.Client);

            foreach (OrganizationClientAgreement agreement in message.Client.OrganizationClientAgreements) {
                agreement.CurrencyId = agreement.Currency.Id;
                agreement.OrganizationClientId = message.Client.Id;

                agreement.FromDate =
                    agreement.FromDate.Year.Equals(1)
                        ? DateTime.UtcNow.Date
                        : agreement.FromDate.Date;

                OrganizationClientAgreement lastRecord = organizationClientAgreementRepository.GetLastRecord();

                agreement.Number = lastRecord != null ? string.Format("{0:D10}", Convert.ToInt64(lastRecord.Number) + 1) : string.Format("{0:D10}", 1);

                organizationClientAgreementRepository.Add(agreement);
            }

            Sender.Tell(
                organizationClientRepository
                    .GetById(
                        message.Client.Id
                    )
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessUpdateOrganizationClientMessage(UpdateOrganizationClientMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (message.Client == null) throw new Exception("OrganizationClient entity can not be null or empty");
            if (message.Client.IsNew()) throw new Exception("New OrganizationClient is not valid payload for current request");
            if (!message.Client.OrganizationClientAgreements.Any()) throw new Exception(OrganizationClientResourceNames.NEED_ADD_ONE_AGREEMENT);
            if (!message.Client.OrganizationClientAgreements.All(a => a.Currency != null && !a.Currency.IsNew() && !a.Deleted))
                throw new Exception(OrganizationClientResourceNames.NEED_SPECIFY_CURRENCY_FOR_ALL_AGREEMENTS);

            IOrganizationClientRepository organizationClientRepository = _organizationClientRepositoriesFactory.NewOrganizationClientRepository(connection);
            IOrganizationClientAgreementRepository organizationClientAgreementRepository =
                _organizationClientRepositoriesFactory.OrganizationClientAgreementRepository(connection);

            organizationClientRepository.Update(message.Client);

            organizationClientAgreementRepository
                .RemoveAllByClientIdExceptProvided(
                    message.Client.Id,
                    message.Client.OrganizationClientAgreements.Where(a => !a.IsNew() && !a.Deleted).Select(a => a.Id)
                );

            organizationClientAgreementRepository
                .RemoveAllByIds(
                    message.Client.OrganizationClientAgreements.Where(a => !a.IsNew() && a.Deleted).Select(a => a.Id)
                );

            foreach (OrganizationClientAgreement agreement in message.Client.OrganizationClientAgreements.Where(a => a.IsNew())) {
                agreement.CurrencyId = agreement.Currency.Id;
                agreement.OrganizationClientId = message.Client.Id;

                agreement.FromDate =
                    agreement.FromDate.Year.Equals(1)
                        ? DateTime.UtcNow.Date
                        : agreement.FromDate.Date;

                OrganizationClientAgreement lastRecord = organizationClientAgreementRepository.GetLastRecord();

                agreement.Number = lastRecord != null ? string.Format("{0:D10}", Convert.ToInt64(lastRecord.Number) + 1) : string.Format("{0:D10}", 1);

                organizationClientAgreementRepository.Add(agreement);
            }

            organizationClientAgreementRepository
                .Update(
                    message
                        .Client
                        .OrganizationClientAgreements
                        .Where(a => !a.IsNew() && !a.Deleted)
                        .Select(agreement => {
                            agreement.FromDate =
                                agreement.FromDate.Year.Equals(1)
                                    ? DateTime.UtcNow.Date
                                    : agreement.FromDate.Date;

                            agreement.CurrencyId = agreement.Currency.Id;

                            return agreement;
                        })
                );

            Sender.Tell(
                organizationClientRepository
                    .GetById(
                        message.Client.Id
                    )
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessRemoveOrganizationClientByNetIdMessage(RemoveOrganizationClientByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        _organizationClientRepositoriesFactory
            .NewOrganizationClientRepository(connection)
            .Remove(message.NetId);
    }
}