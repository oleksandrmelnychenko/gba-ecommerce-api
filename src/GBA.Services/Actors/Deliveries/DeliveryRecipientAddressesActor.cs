using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Deliveries.RecipientAddresses;
using GBA.Domain.Repositories.Delivery.Contracts;

namespace GBA.Services.Actors.Deliveries;

public sealed class DeliveryRecipientAddressesActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IDeliveryRepositoriesFactory _deliveryRepositoriesFactory;

    public DeliveryRecipientAddressesActor(
        IDbConnectionFactory connectionFactory,
        IDeliveryRepositoriesFactory deliveryRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _deliveryRepositoriesFactory = deliveryRepositoriesFactory;

        Receive<AddDeliveryRecipientAddressMessage>(ProcessAddDeliveryRecipientAddressMessage);

        Receive<GetAllDeliveryRecipientAddressesByRecipientNetIdMessage>(ProcessGetAllDeliveryRecipientAddressesByRecipientNetIdMessage);

        Receive<ChangeDeliveryRecipientAddressPriorityMessage>(ProcessChangeDeliveryRecipientAddressPriorityMessage);
    }

    private void ProcessAddDeliveryRecipientAddressMessage(AddDeliveryRecipientAddressMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IDeliveryRecipientAddressRepository deliveryRecipientAddressRepository = _deliveryRepositoriesFactory.NewDeliveryRecipientAddressRepository(connection);

        if (message.DeliveryRecipientAddress.DeliveryRecipient != null)
            message.DeliveryRecipientAddress.DeliveryRecipientId = message.DeliveryRecipientAddress.DeliveryRecipient.Id;

        long deliveryRecipientAddressId = deliveryRecipientAddressRepository.Add(message.DeliveryRecipientAddress);

        Sender.Tell(deliveryRecipientAddressRepository.GetById(deliveryRecipientAddressId));
    }

    private void ProcessGetAllDeliveryRecipientAddressesByRecipientNetIdMessage(GetAllDeliveryRecipientAddressesByRecipientNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_deliveryRepositoriesFactory
            .NewDeliveryRecipientAddressRepository(connection)
            .GetAllByRecipientNetId(message.RecipientNetId));
    }

    private void ProcessChangeDeliveryRecipientAddressPriorityMessage(ChangeDeliveryRecipientAddressPriorityMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IDeliveryRecipientAddressRepository deliveryRecipientAddressRepository = _deliveryRepositoriesFactory.NewDeliveryRecipientAddressRepository(connection);

        if (message.DecreaseTo != null) deliveryRecipientAddressRepository.DecreasePriority((long)message.DecreaseTo);

        deliveryRecipientAddressRepository.IncreasePriority(message.IncreaseTo);
    }
}