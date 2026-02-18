using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Messages.Supplies;
using GBA.Domain.Messages.Supplies.Documents;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Services.Actors.Supplies;

public sealed class SupplyDeliveryDocumentsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;

    public SupplyDeliveryDocumentsActor(
        IDbConnectionFactory connectionFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;

        Receive<GetAllSupplyDeliveryDocumentsByTypeMessage>(ProcessGetAllSupplyDeliveryDocumentsByTypeMessage);

        Receive<GetSupplyDeliveryDocumentByNetIdMessage>(ProcessGetSupplyDeliveryDocumentByNetIdMessage);

        Receive<AddSupplyDeliveryDocumentMessage>(ProcessAddSupplyDeliveryDocumentMessage);

        Receive<UpdateSupplyOrderDeliveryDocumentMessage>(ProcessUpdateSupplyOrderDeliveryDocumentMessage);

        Receive<GetAllSupplyDeliveryDocumentsMessage>(ProcessGetAllSupplyDeliveryDocumentsMessage);
    }

    private void ProcessGetAllSupplyDeliveryDocumentsByTypeMessage(GetAllSupplyDeliveryDocumentsByTypeMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_supplyRepositoriesFactory
            .NewSupplyDeliveryDocumentRepository(connection)
            .GetAllByType(message.Type)
        );
    }

    private void ProcessGetSupplyDeliveryDocumentByNetIdMessage(GetSupplyDeliveryDocumentByNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_supplyRepositoriesFactory
            .NewSupplyDeliveryDocumentRepository(connection)
            .GetByNetId(message.NetId)
        );
    }

    private void ProcessAddSupplyDeliveryDocumentMessage(AddSupplyDeliveryDocumentMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISupplyDeliveryDocumentRepository supplyDeliveryDocumentRepository = _supplyRepositoriesFactory.NewSupplyDeliveryDocumentRepository(connection);

        long supplyDeliveryDocumentId = supplyDeliveryDocumentRepository.Add(message.SupplyDeliveryDocument);

        Sender.Tell(supplyDeliveryDocumentRepository
            .GetById(supplyDeliveryDocumentId)
        );
    }

    private void ProcessUpdateSupplyOrderDeliveryDocumentMessage(UpdateSupplyOrderDeliveryDocumentMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ISupplyOrderRepository supplyOrderRepository = _supplyRepositoriesFactory.NewSupplyOrderRepository(connection);

        _supplyRepositoriesFactory.NewSupplyOrderDeliveryDocumentRepository(connection).UpdateDocumentData(message.Document);

        Sender.Tell(supplyOrderRepository.GetByNetId(supplyOrderRepository.GetNetIdById(message.Document.SupplyOrderId)));
    }

    private void ProcessGetAllSupplyDeliveryDocumentsMessage(GetAllSupplyDeliveryDocumentsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_supplyRepositoriesFactory.NewSupplyDeliveryDocumentRepository(connection).GetAllNamesGrouped());
    }
}