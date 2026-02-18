using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Products;
using GBA.Domain.Messages.Products.ProductReservations;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Services.Actors.Products;

public sealed class ProductReservationsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;

    public ProductReservationsActor(
        IDbConnectionFactory connectionFactory,
        IProductRepositoriesFactory productRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _productRepositoriesFactory = productRepositoriesFactory;

        Receive<AddProductReservationMessage>(ProcessAddProductReservationMessage);

        Receive<UpdateProductReservationMessage>(ProcessUpdateProductReservationMessage);
    }

    private void ProcessAddProductReservationMessage(AddProductReservationMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        if (!message.ProductReservation.OrderItemId.Equals(0) && !message.ProductReservation.ProductAvailabilityId.Equals(0))
            _productRepositoriesFactory.NewProductReservationRepository(connection).Add(message.ProductReservation);
    }

    private void ProcessUpdateProductReservationMessage(UpdateProductReservationMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);

        if (message.ProductReservation.OrderItemId.Equals(0) || message.ProductReservation.ProductAvailabilityId.Equals(0)) return;

        ProductReservation productReservation = productReservationRepository
            .GetByOrderItemAndProductAvailabilityIds(message.ProductReservation.OrderItemId, message.ProductReservation.ProductAvailabilityId);

        productReservation.Qty = message.ProductReservation.Qty;

        productReservationRepository.Update(productReservation);
    }
}