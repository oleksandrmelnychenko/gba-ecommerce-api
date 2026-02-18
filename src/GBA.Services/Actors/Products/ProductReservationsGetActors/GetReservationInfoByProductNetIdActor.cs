using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Products;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Messages.Products.ProductReservations;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.ReSales.Contracts;
using GBA.Domain.Repositories.Supplies.Ukraine.Contracts;

namespace GBA.Services.Actors.Products.ProductReservationsGetActors;

public sealed class GetReservationInfoByProductNetIdActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly IReSaleRepositoriesFactory _reSaleRepositoriesFactory;
    private readonly ISupplyUkraineRepositoriesFactory _supplyUkraineRepositoriesFactory;

    public GetReservationInfoByProductNetIdActor(
        IDbConnectionFactory connectionFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        ISupplyUkraineRepositoriesFactory supplyUkraineRepositoriesFactory,
        IReSaleRepositoriesFactory reSaleRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _productRepositoriesFactory = productRepositoriesFactory;
        _supplyUkraineRepositoriesFactory = supplyUkraineRepositoriesFactory;
        _reSaleRepositoriesFactory = reSaleRepositoriesFactory;

        Receive<GetReservationInfoByProductNetIdMessage>(ProcessGetReservationInfoByProductNetIdMessage);
    }

    private void ProcessGetReservationInfoByProductNetIdMessage(GetReservationInfoByProductNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Product product = _productRepositoriesFactory.NewGetSingleProductRepository(connection).GetByNetIdWithoutIncludes(message.NetId);

        Reservation reservation = new();

        if (product != null) {
            IProductReservationRepository productReservationRepository = _productRepositoriesFactory.NewProductReservationRepository(connection);

            reservation.ProductReservationsPL =
                productReservationRepository
                    .GetAllCurrentReservationsByProductNetIdAndCulture(
                        product.NetUid,
                        "pl"
                    );

            reservation.CartProductReservationsPL = reservation.ProductReservationsPL.Where(r => r.OrderItem.ClientShoppingCart != null).ToList();
            reservation.ProductReservationsPL = reservation.ProductReservationsPL.Where(r => r.OrderItem.Order != null).ToList();

            reservation.TotalReservedPL = reservation.ProductReservationsPL.Sum(r => r.Qty);
            reservation.TotalCartReservedPL = reservation.CartProductReservationsPL.Sum(r => r.Qty);

            reservation.ProductReservationsUK =
                productReservationRepository
                    .GetAllCurrentReservationsByProductNetIdAndCulture(
                        product.NetUid,
                        "uk"
                    );

            reservation.CartProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.ClientShoppingCart != null).ToList();
            reservation.ProductReservationsUK = reservation.ProductReservationsUK.Where(r => r.OrderItem.Order != null).ToList();

            reservation.TotalReservedUK = reservation.ProductReservationsUK.Sum(r => r.Qty);
            reservation.TotalCartReservedUK = reservation.CartProductReservationsUK.Sum(r => r.Qty);

            reservation.SupplyOrderUkraineCartItem =
                _supplyUkraineRepositoriesFactory
                    .NewSupplyOrderUkraineCartItemRepository(connection)
                    .GetByProductIdIfReserved(
                        product.Id
                    );

            reservation.DefectiveAvailabilities =
                _productRepositoriesFactory
                    .NewProductAvailabilityRepository(connection)
                    .GetAllOnDefectiveStoragesByProductId(
                        product.Id
                    );

            reservation.TotalProductReSaleQty = _reSaleRepositoriesFactory
                .NewReSaleAvailabilityRepository(connection)
                .GetTotalProductQtyFromReSaleAvailabilitiesByProductId(product.Id) ?? 0;
        }

        Sender.Tell(reservation);
    }
}