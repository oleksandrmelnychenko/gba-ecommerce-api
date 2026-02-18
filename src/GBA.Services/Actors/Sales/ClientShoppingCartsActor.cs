using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Messages.Sales.ClientShoppingCarts;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Services.Actors.Sales;

public sealed class ClientShoppingCartsActor : ReceiveActor {
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    public ClientShoppingCartsActor(
        IDbConnectionFactory connectionFactory,
        IClientRepositoriesFactory clientRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _clientRepositoriesFactory = clientRepositoriesFactory;

        Receive<GetAllValidShoppingCartsMessage>(ProcessGetAllValidShoppingCartsMessage);
    }

    private void ProcessGetAllValidShoppingCartsMessage(GetAllValidShoppingCartsMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        List<ClientShoppingCart> clientShoppingCarts =
            _clientRepositoriesFactory
                .NewClientShoppingCartRepository(connection)
                .GetAllValidClientShoppingCarts();

        foreach (ClientShoppingCart shoppingCart in clientShoppingCarts) {
            foreach (OrderItem orderItem in shoppingCart.OrderItems) {
                orderItem.TotalAmount =
                    decimal.Round(
                        orderItem.Product.CurrentPrice * Convert.ToDecimal(orderItem.Qty),
                        2,
                        MidpointRounding.AwayFromZero
                    );

                orderItem.TotalAmountLocal =
                    orderItem.Product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty);

                orderItem.Product.CurrentLocalPrice =
                    decimal.Round(
                        orderItem.Product.CurrentLocalPrice,
                        2,
                        MidpointRounding.AwayFromZero
                    );

                orderItem.TotalAmount =
                    decimal.Round(
                        orderItem.TotalAmount,
                        2,
                        MidpointRounding.AwayFromZero
                    );

                orderItem.TotalAmountLocal =
                    decimal.Round(
                        orderItem.TotalAmountLocal,
                        2,
                        MidpointRounding.AwayFromZero
                    );
            }

            shoppingCart.TotalAmount =
                decimal.Round(
                    shoppingCart.OrderItems.Sum(o => o.TotalAmount),
                    2,
                    MidpointRounding.AwayFromZero
                );

            shoppingCart.TotalLocalAmount =
                decimal.Round(
                    shoppingCart.OrderItems.Sum(o => o.TotalAmountLocal),
                    2,
                    MidpointRounding.AwayFromZero
                );
        }

        Sender.Tell(clientShoppingCarts);
    }
}