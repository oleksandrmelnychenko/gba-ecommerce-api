using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Delivery.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Services.Services.Clients.Contracts;

namespace GBA.Services.Services.Clients;

public sealed class ClientResourceAccessService(
    IDbConnectionFactory connectionFactory,
    IClientRepositoriesFactory clientRepositoriesFactory,
    IDeliveryRepositoriesFactory deliveryRepositoriesFactory,
    ISaleRepositoriesFactory saleRepositoriesFactory)
    : IClientResourceAccessService {
    public bool CanAccessClient(Guid actorNetId, Guid clientNetId) {
        if (actorNetId == Guid.Empty || clientNetId == Guid.Empty) return false;

        using IDbConnection connection = connectionFactory.NewSqlConnection();
        AccessScope scope = BuildAccessScope(connection, actorNetId);
        return scope.ClientNetIds.Contains(clientNetId);
    }

    public bool CanAccessClientOrAgreement(Guid actorNetId, Guid resourceNetId) {
        if (actorNetId == Guid.Empty || resourceNetId == Guid.Empty) return false;

        using IDbConnection connection = connectionFactory.NewSqlConnection();
        AccessScope scope = BuildAccessScope(connection, actorNetId);
        return scope.AgreementNetIds.Contains(resourceNetId)
               || !scope.IsWorkplace && scope.ClientNetIds.Contains(resourceNetId);
    }

    public bool CanAccessDeliveryRecipient(Guid actorNetId, Guid recipientNetId) {
        if (actorNetId == Guid.Empty || recipientNetId == Guid.Empty) return false;

        using IDbConnection connection = connectionFactory.NewSqlConnection();
        AccessScope scope = BuildAccessScope(connection, actorNetId);
        DeliveryRecipient recipient = deliveryRepositoriesFactory
            .NewDeliveryRecipientRepository(connection)
            .GetByNetId(recipientNetId);

        return recipient != null && scope.ClientIds.Contains(recipient.ClientId);
    }

    public bool CanAccessDeliveryRecipientAddress(Guid actorNetId, Guid addressNetId) {
        if (actorNetId == Guid.Empty || addressNetId == Guid.Empty) return false;

        using IDbConnection connection = connectionFactory.NewSqlConnection();
        AccessScope scope = BuildAccessScope(connection, actorNetId);
        DeliveryRecipientAddress address = deliveryRepositoriesFactory
            .NewDeliveryRecipientAddressRepository(connection)
            .GetByNetId(addressNetId);
        DeliveryRecipient recipient = address == null
            ? null
            : deliveryRepositoriesFactory
                .NewDeliveryRecipientRepository(connection)
                .GetById(address.DeliveryRecipientId);

        return recipient != null && scope.ClientIds.Contains(recipient.ClientId);
    }

    public bool CanAccessSale(Guid actorNetId, Guid saleNetId) {
        if (actorNetId == Guid.Empty || saleNetId == Guid.Empty) return false;

        using IDbConnection connection = connectionFactory.NewSqlConnection();
        AccessScope scope = BuildAccessScope(connection, actorNetId);
        Sale sale = saleRepositoriesFactory.NewSaleRepository(connection).GetByNetId(saleNetId);

        if (sale == null) return false;
        Guid agreementNetId = sale.ClientAgreement?.NetUid ?? Guid.Empty;
        Guid clientNetId = sale.ClientAgreement?.Client?.NetUid ?? Guid.Empty;

        if (scope.IsWorkplace) {
            return sale.Workplace?.NetUid == actorNetId
                   || scope.AgreementNetIds.Contains(agreementNetId);
        }

        return scope.ClientNetIds.Contains(clientNetId)
               || scope.AgreementNetIds.Contains(agreementNetId);
    }

    private AccessScope BuildAccessScope(IDbConnection connection, Guid actorNetId) {
        IClientRepository clientRepository = clientRepositoriesFactory.NewClientRepository(connection);
        IClientAgreementRepository agreementRepository =
            clientRepositoriesFactory.NewClientAgreementRepository(connection);

        HashSet<Guid> clientNetIds = new();
        HashSet<long> clientIds = new();
        HashSet<Guid> agreementNetIds = new();
        bool isWorkplace = false;

        Guid rootNetId = clientRepository.GetRootNetIdBySubClientNetId(actorNetId);
        if (rootNetId != Guid.Empty) {
            AddClient(clientRepository.GetByNetIdWithoutIncludes(rootNetId), clientNetIds, clientIds);
            AddClient(clientRepository.GetByNetIdWithoutIncludes(actorNetId), clientNetIds, clientIds);
        } else {
            Client actorClient = clientRepository.GetByNetIdWithoutIncludes(actorNetId);
            if (actorClient != null) {
                AddClient(actorClient, clientNetIds, clientIds);

                foreach (Client subClient in clientRepository.GetAllSubClients(actorClient.NetUid)) {
                    AddClient(subClient, clientNetIds, clientIds);
                }
            } else {
                isWorkplace = true;
                Workplace workplace = clientRepositoriesFactory
                    .NewWorkplaceRepository(connection)
                    .GetByNetIdWithClient(actorNetId);

                AddClient(workplace?.MainClient, clientNetIds, clientIds);
                foreach (WorkplaceClientAgreement workplaceAgreement
                         in workplace?.WorkplaceClientAgreements ?? Array.Empty<WorkplaceClientAgreement>()) {
                    AddAgreement(workplaceAgreement.ClientAgreement, agreementNetIds);
                }
            }
        }

        if (!isWorkplace) {
            foreach (Guid clientNetId in clientNetIds) {
                foreach (ClientAgreement agreement in agreementRepository.GetAllByClientNetId(clientNetId)) {
                    AddAgreement(agreement, agreementNetIds);
                }
            }
        }

        return new AccessScope(clientNetIds, clientIds, agreementNetIds, isWorkplace);
    }

    private static void AddClient(
        Client client,
        ISet<Guid> clientNetIds,
        ISet<long> clientIds) {
        if (client == null) return;
        if (client.NetUid != Guid.Empty) clientNetIds.Add(client.NetUid);
        if (client.Id > 0) clientIds.Add(client.Id);
    }

    private static void AddAgreement(
        ClientAgreement agreement,
        ISet<Guid> agreementNetIds) {
        if (agreement == null || agreement.NetUid == Guid.Empty) return;
        agreementNetIds.Add(agreement.NetUid);
    }

    private sealed record AccessScope(
        HashSet<Guid> ClientNetIds,
        HashSet<long> ClientIds,
        HashSet<Guid> AgreementNetIds,
        bool IsWorkplace);
}
