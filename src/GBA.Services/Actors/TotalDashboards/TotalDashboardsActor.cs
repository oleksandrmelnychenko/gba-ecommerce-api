using System;
using System.Data;
using Akka.Actor;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.EntityHelpers.TotalDashboards;
using GBA.Domain.Messages.TotalDashboards;
using GBA.Domain.Repositories.PaymentOrders.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Services.Actors.TotalDashboards;

public sealed class TotalDashboardsActor : ReceiveActor {
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IPaymentOrderRepositoriesFactory _paymentOrderRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly ISupplyRepositoriesFactory _supplyRepositoriesFactory;

    public TotalDashboardsActor(
        IDbConnectionFactory connectionFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IPaymentOrderRepositoriesFactory paymentOrderRepositoriesFactory,
        ISupplyRepositoriesFactory supplyRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _saleRepositoriesFactory = saleRepositoriesFactory;
        _paymentOrderRepositoriesFactory = paymentOrderRepositoriesFactory;
        _supplyRepositoriesFactory = supplyRepositoriesFactory;

        Receive<GetTotalsDashboardMessage>(ProcessGetTotalsDashboard);

        Receive<GetFilteredPaymentCurrencyRegisterMovementMessage>(ProcessGetFilteredPaymentCurrencyRegisterMovement);

        Receive<GetFilteredGroupedPaymentsByPeriodMessage>(ProcessGetFilteredGroupedPaymentsByPeriod);

        Receive<GetOrderedInvoicesByIsShippedMessage>(ProcessGetOrderedInvoicesByIsShipped);

        Receive<GetStateCurrencyRegisterByPeriodMessage>(ProcessGetStatePaymentByPeriod);
    }

    private void ProcessGetTotalsDashboard(GetTotalsDashboardMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            TotalDashboard dashboard = new() {
                TotalSales = _saleRepositoriesFactory
                    .NewSaleRepository(connection)
                    .GetTotalAmountByDayAndCurrentMonth(),
                TotalIncomes = _paymentOrderRepositoriesFactory
                    .NewIncomePaymentOrderRepository(connection)
                    .GetTotalsAmountByDayAndCurrentMonth(),
                TotalOutcomes = _paymentOrderRepositoriesFactory
                    .NewOutcomePaymentOrderRepository(connection)
                    .GetTotalsAmountByDayAndCurrentMonth(),
                BalanceByCurrency = _paymentOrderRepositoriesFactory
                    .NewPaymentRegisterRepository(connection)
                    .GetTotalBalanceByCurrency(),
                TotalInvoices = _supplyRepositoriesFactory
                    .NewSupplyInvoiceRepository(connection)
                    .GetTotalQtyNotArrivedInvoices()
            };

            Sender.Tell(dashboard);
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetFilteredPaymentCurrencyRegisterMovement(GetFilteredPaymentCurrencyRegisterMovementMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _paymentOrderRepositoriesFactory
                    .NewPaymentRegisterRepository(connection)
                    .GetFilteredMovementsByPaymentRegisterNetId(
                        message.NetId,
                        message.TypeFilteredMovements,
                        message.From,
                        message.To,
                        message.Limit,
                        message.Offset)
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetFilteredGroupedPaymentsByPeriod(GetFilteredGroupedPaymentsByPeriodMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _paymentOrderRepositoriesFactory
                    .NewIncomePaymentOrderRepository(connection)
                    .GetFilteredGroupedPaymentsByPeriod(
                        message.From,
                        message.To,
                        message.Period,
                        message.NetId)
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetOrderedInvoicesByIsShipped(GetOrderedInvoicesByIsShippedMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _supplyRepositoriesFactory
                    .NewSupplyInvoiceRepository(connection)
                    .GetOrderedInvoicesByIsShipped(
                        message.Type)
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }

    private void ProcessGetStatePaymentByPeriod(GetStateCurrencyRegisterByPeriodMessage message) {
        try {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            Sender.Tell(
                _paymentOrderRepositoriesFactory
                    .NewPaymentRegisterRepository(connection)
                    .GetStatePaymentByPeriod(
                        message.From,
                        message.To)
            );
        } catch (Exception exc) {
            Sender.Tell(exc);
        }
    }
}