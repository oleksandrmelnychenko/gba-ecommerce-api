using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Messages.Sales.Orders;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Pricings.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;

namespace GBA.Services.Actors.Sales;

public sealed class OrdersActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoryFactory;
    private readonly IPricingRepositoriesFactory _pricingRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;

    public OrdersActor(
        IDbConnectionFactory connectionFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IPricingRepositoriesFactory pricingRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoryFactory) {
        _connectionFactory = connectionFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _pricingRepositoriesFactory = pricingRepositoriesFactory;
        _exchangeRateRepositoryFactory = exchangeRateRepositoryFactory;

        Receive<GetAllShopOrdersMessage>(ProcessGetAllShopOrdersMessage);

        Receive<GetAllShopOrdersByUserNetIdMessage>(ProcessGetAllShopOrdersByUserNetIdMessage);

        Receive<GetAllShopOrdersByClientNetIdMessage>(ProcessGetAllShopOrdersByClientNetIdMessage);

        Receive<GetAllShopOrdersTotalAmountByUserNetIdMessage>(ProcessGetAllShopOrdersTotalAmountByUserNetIdMessage);

        Receive<GetAllShopOrdersTotalAmountMessage>(ProcessGetAllShopOrdersTotalAmountMessage);
    }

    private void ProcessGetAllShopOrdersMessage(GetAllShopOrdersMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_saleRepositoriesFactory.NewOrderRepository(connection).GetAllShopOrders());
    }

    private void ProcessGetAllShopOrdersByUserNetIdMessage(GetAllShopOrdersByUserNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_saleRepositoriesFactory
            .NewOrderRepository(connection)
            .GetAllShopOrdersByUserNetId(message.UserNetId));
    }

    private void ProcessGetAllShopOrdersByClientNetIdMessage(GetAllShopOrdersByClientNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        ExchangeRate euroExchangeRate = _exchangeRateRepositoryFactory.NewExchangeRateRepository(connection).GetEuroExchangeRateByCurrentCulture();
        List<Order> orders = _saleRepositoriesFactory.NewOrderRepository(connection).GetAllShopOrdersByClientNetId(message.ClientNetId);

        foreach (Order order in orders) {
            decimal currentExtraCharge = _pricingRepositoriesFactory.NewPricingRepository(connection)
                .GetCalculatedExtraChargeForCurrentPricing(order.ClientAgreement.Agreement.PricingId ?? 0);

            if (order.ClientAgreement?.Agreement?.Currency != null && !order.ClientAgreement.Agreement.Currency.Code.Equals("EUR"))
                foreach (OrderItem orderItem in order.OrderItems) {
                    if (!currentExtraCharge.Equals(decimal.Zero)) {
                        order.TotalAmount += orderItem.Product.ProductPricings.First().Price * Convert.ToDecimal(orderItem.Qty) * euroExchangeRate.Amount;
                    } else {
                        decimal productPrice = orderItem.Product.ProductPricings.First().Price;

                        order.TotalAmount += (productPrice + productPrice * currentExtraCharge / 100) * Convert.ToDecimal(orderItem.Qty) * euroExchangeRate.Amount;
                    }

                    order.TotalCount += orderItem.Qty;
                }
            else if (order.ClientAgreement?.Agreement?.Currency != null)
                foreach (OrderItem orderItem in order.OrderItems) {
                    if (!currentExtraCharge.Equals(decimal.Zero)) {
                        order.TotalAmount += orderItem.Product.ProductPricings.First().Price * Convert.ToDecimal(orderItem.Qty);
                    } else {
                        decimal productPrice = orderItem.Product.ProductPricings.First().Price;

                        order.TotalAmount += (productPrice + productPrice * currentExtraCharge / 100) * Convert.ToDecimal(orderItem.Qty);
                    }

                    order.TotalCount += orderItem.Qty;
                }
            else
                foreach (OrderItem orderItem in order.OrderItems) {
                    order.TotalAmount += orderItem.Product.ProductPricings.First().Price * Convert.ToDecimal(orderItem.Qty);

                    order.TotalCount += orderItem.Qty;
                }
        }

        Sender.Tell(orders);
    }

    private void ProcessGetAllShopOrdersTotalAmountByUserNetIdMessage(GetAllShopOrdersTotalAmountByUserNetIdMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_saleRepositoriesFactory
            .NewOrderRepository(connection)
            .GetAllShopOrdersTotalAmountByUserNetId(message.UserNetId));
    }

    private void ProcessGetAllShopOrdersTotalAmountMessage(GetAllShopOrdersTotalAmountMessage message) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        Sender.Tell(_saleRepositoriesFactory
            .NewOrderRepository(connection)
            .GetAllShopOrdersTotalAmount());
    }
}