using System;
using System.Data;
using System.Globalization;
using Akka.Actor;
using GBA.Common.IdentityConfiguration.Roles;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.EntityHelpers;
using GBA.Domain.IdentityEntities;
using GBA.Domain.Messages.Clients.EcommerceClients;
using GBA.Domain.Repositories.Agreements.Contracts;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Identities.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;

namespace GBA.Services.Actors.Clients;

public sealed class EcommerceClientActor : ReceiveActor {
    private readonly IAgreementRepositoriesFactory _agreementRepositoriesFactory;
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IIdentityRepositoriesFactory _identityRepositoriesFactory;
    private readonly IStorageRepositoryFactory _storageRepositoryFactory;

    public EcommerceClientActor(
        IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IIdentityRepositoriesFactory identityRepositoriesFactory,
        IAgreementRepositoriesFactory agreementRepositoriesFactory,
        IStorageRepositoryFactory storageRepositoryFactory) {
        _connectionFactory = connectionFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _identityRepositoriesFactory = identityRepositoriesFactory;
        _agreementRepositoriesFactory = agreementRepositoriesFactory;
        _storageRepositoryFactory = storageRepositoryFactory;

        Receive<EcommerceSignUpMessage>(ProcessEcommerceSignUpMessage);

        Receive<AddDefaultAgreementMessage>(ProcessAddDefaultAgreementMessage);
    }

    private void ProcessAddDefaultAgreementMessage(AddDefaultAgreementMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IAgreementRepository agreementRepository = _agreementRepositoriesFactory.NewAgreementRepository(connection);

            Storage storage = _storageRepositoryFactory.NewStorageRepository(connection).GetWithHighestPriority();

            ClientAgreement retailClientAgreement =
                _clientRepositoriesFactory.NewClientAgreementRepository(connection)
                    .GetByClientNetIdWithOrWithoutVat(
                        _clientRepositoriesFactory.NewClientRepository(connection).GetRetailClient().NetUid,
                        storage.OrganizationId.Value,
                        message.IsLocalPayment);

            Agreement defaultAgreement = retailClientAgreement.Agreement;
            defaultAgreement.Id = 0;
            defaultAgreement.NetUid = Guid.Empty;
            defaultAgreement.Name = $"������ {CultureInfo.CurrentCulture.TwoLetterISOLanguageName}";
            defaultAgreement.IsSelected = true;

            _clientRepositoriesFactory.NewClientAgreementRepository(connection).Add(new ClientAgreement {
                AgreementId = agreementRepository.Add(defaultAgreement),
                ClientId = message.Client.Id,
                ProductReservationTerm = 3
            });

            Sender.Tell(true);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessEcommerceSignUpMessage(EcommerceSignUpMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IIdentityRepository identityRepository = _identityRepositoriesFactory.NewIdentityRepository();
        IClientRepository clientRepository = _clientRepositoriesFactory.NewClientRepository(connection);

        Client client = message.Client;

        client.IsFromECommerce = true;

        client.Id = clientRepository.Add(client);

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower().Equals("uk"))
            client.ClientInRole = new ClientInRole {
                ClientTypeId = 2,
                ClientTypeRoleId = 1,
                ClientId = client.Id
            };
        else
            client.ClientInRole = new ClientInRole {
                ClientTypeId = 2,
                ClientTypeRoleId = 2,
                ClientId = client.Id
            };

        _clientRepositoriesFactory.NewClientInRoleRepository(connection).Add(client.ClientInRole);

        client = clientRepository.GetById(client.Id);

        //UpdateAbbreviation
        if (!string.IsNullOrEmpty(client.LastName) || !string.IsNullOrEmpty(client.FirstName)) {
            if (!string.IsNullOrEmpty(client.LastName)) client.Abbreviation += client.LastName.ToCharArray()[0];
            if (!string.IsNullOrEmpty(client.FirstName)) client.Abbreviation += client.FirstName.ToCharArray()[0];

            clientRepository.UpdateAbbreviation(client);
        }

        UserIdentity user = new() {
            Email = client.EmailAddress,
            UserName = !string.IsNullOrEmpty(message.Login)
                ? message.Login
                : !string.IsNullOrEmpty(client.MobileNumber)
                    ? client.MobileNumber
                    : client.EmailAddress,
            PhoneNumber = client.MobileNumber,
            NetId = client.NetUid,
            Region = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
            UserType = IdentityUserType.Client
        };

        IdentityResponse response = identityRepository.CreateUser(user, message.Password, false).Result;

        Sender.Tell(new Tuple<IdentityResponse, Client>(response, client));

        if (response.Succeeded) {
            identityRepository.AddUserRoleAndClaims(user, IdentityRoles.ClientUa);
            Self.Ask<object>(new AddDefaultAgreementMessage(client, message.IsLocalPayment));
        } else {
            clientRepository.Remove(client.Id);
        }
    }
}