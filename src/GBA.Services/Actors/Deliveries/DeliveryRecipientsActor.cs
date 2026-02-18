using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Messages.Deliveries.Recipients;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Delivery.Contracts;

namespace GBA.Services.Actors.Deliveries;

public sealed class DeliveryRecipientsActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IDeliveryRepositoriesFactory _deliveryRepositoriesFactory;

    public DeliveryRecipientsActor(
        IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory,
        IDeliveryRepositoriesFactory deliveryRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _deliveryRepositoriesFactory = deliveryRepositoriesFactory;

        Receive<GetAllRecipientsByClientNetIdMessage>(ProcessGetAllRecipientsByClientNetIdMessage);

        Receive<GetAllRecipientsDeletedByClientNetIdMessage>(ProcessGetAllRecipientsDeletedByClientNetIdMessage);

        Receive<AddDeliveryRecipientMessage>(ProcessAddDeliveryRecipientMessage);

        Receive<ChangeDeliveryRecipientPriorityMessage>(ProcessChangeDeliveryRecipientPriorityMessage);

        Receive<ReturnRemoveNetIdMessage>(ProcessReturnRemoveNetIdMessageMessage);

        Receive<RemoveNetIdMessage>(ProcessRemoveNetIdMessageMessage);
    }

    private void ProcessReturnRemoveNetIdMessageMessage(ReturnRemoveNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IDeliveryRecipientRepository deliveryRecipientRepository = _deliveryRepositoriesFactory
            .NewDeliveryRecipientRepository(connection);

        deliveryRecipientRepository.ReturnRemove(message.DeliveryRecipientNetId);
        Sender.Tell(deliveryRecipientRepository.GetByNetId(message.DeliveryRecipientNetId));
    }

    private void ProcessRemoveNetIdMessageMessage(RemoveNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IDeliveryRecipientRepository deliveryRecipientRepository = _deliveryRepositoriesFactory
            .NewDeliveryRecipientRepository(connection);

        deliveryRecipientRepository.Remove(message.DeliveryRecipientNetId);
        Sender.Tell(deliveryRecipientRepository.GetByNetId(message.DeliveryRecipientNetId));
    }

    private void ProcessGetAllRecipientsDeletedByClientNetIdMessage(GetAllRecipientsDeletedByClientNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IDeliveryRecipientRepository deliveryRecipientRepository = _deliveryRepositoriesFactory
            .NewDeliveryRecipientRepository(connection);

        List<DeliveryRecipient> deliveryRecipients = deliveryRecipientRepository.GetAllRecipientsDeletedByClientNetId(message.ClientNetId);

        if (!deliveryRecipients.Any()) {
            Client client = _clientRepositoriesFactory.NewClientRepository(connection).GetByNetId(message.ClientNetId);

            deliveryRecipientRepository.Add(new DeliveryRecipient {
                ClientId = client.Id,
                FullName = client.FullName
            });

            deliveryRecipients = deliveryRecipientRepository.GetAllRecipientsDeletedByClientNetId(message.ClientNetId);
        }

        Sender.Tell(deliveryRecipients);
    }

    private void ProcessGetAllRecipientsByClientNetIdMessage(GetAllRecipientsByClientNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IDeliveryRecipientRepository deliveryRecipientRepository = _deliveryRepositoriesFactory
            .NewDeliveryRecipientRepository(connection);

        List<DeliveryRecipient> deliveryRecipients = deliveryRecipientRepository.GetAllRecipientsByClientNetId(message.ClientNetId);

        if (!deliveryRecipients.Any()) {
            Client client = _clientRepositoriesFactory.NewClientRepository(connection).GetByNetId(message.ClientNetId);

            deliveryRecipientRepository.Add(new DeliveryRecipient {
                ClientId = client.Id,
                FullName = client.FullName
            });

            deliveryRecipients = deliveryRecipientRepository.GetAllRecipientsByClientNetId(message.ClientNetId);
        }

        Sender.Tell(deliveryRecipients);
    }

    private void ProcessAddDeliveryRecipientMessage(AddDeliveryRecipientMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IDeliveryRecipientRepository deliveryRecipientRepository = _deliveryRepositoriesFactory
            .NewDeliveryRecipientRepository(connection);

        if (message.DeliveryRecipient.ClientId.Equals(0) && message.DeliveryRecipient.Client != null)
            message.DeliveryRecipient.ClientId = message.DeliveryRecipient.Client.Id;

        long deliveryRecipientId = deliveryRecipientRepository.Add(message.DeliveryRecipient);

        Sender.Tell(_deliveryRepositoriesFactory
            .NewDeliveryRecipientRepository(connection)
            .GetById(deliveryRecipientId));
    }

    private void ProcessChangeDeliveryRecipientPriorityMessage(ChangeDeliveryRecipientPriorityMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IDeliveryRecipientRepository deliveryRecipientRepository = _deliveryRepositoriesFactory.NewDeliveryRecipientRepository(connection);

        if (message.DecreaseTo != null) deliveryRecipientRepository.DecreasePriority((long)message.DecreaseTo);

        deliveryRecipientRepository.IncreasePriority(message.IncreaseTo);
    }
}